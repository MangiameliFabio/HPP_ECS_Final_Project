using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Globalization;
using UnityEngine.VFX;
public partial struct CanonFireSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<Canon>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var fighterQuery = SystemAPI.QueryBuilder().WithAll<Fighter, LocalTransform, LocalToWorld>().Build();
        int currentFighterCount = fighterQuery.CalculateEntityCount();
        var config = SystemAPI.GetSingleton<Config>();
        var parentLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
        parentLocalToWorldLookup.Update(ref state);
        var fighterLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
        fighterLookup.Update(ref state);
        var fighterCount = fighterQuery.CalculateEntityCount();
        var fighterEntities = fighterQuery.ToEntityArray(Allocator.TempJob);
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var laserTransform = SystemAPI.GetComponent<LocalTransform>(config.StarDestroyerLaserPrefab);
        var vfxTransform = SystemAPI.GetComponent<LocalTransform>(config.StarDestroyerBlastVFX);
        var orientJob = new OrientateCanonsJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            globalTime = (float)SystemAPI.Time.ElapsedTime,
            FighterEntities = fighterEntities,
            FighterLookup = fighterLookup,
            FighterCount = fighterCount,
            ParentLocalToWorldLookup = parentLocalToWorldLookup,
            ECB = ecb,
            LaserPrefab = config.StarDestroyerLaserPrefab,
            BlastVFXPrefab = config.StarDestroyerBlastVFX,
        };

        JobHandle jobHandle;
        switch (config.RunType)
        {
            case RunningType.MainThread:
                orientJob.Run();
                break;
            case RunningType.Scheduled:
                jobHandle = orientJob.Schedule(state.Dependency);
                state.Dependency = jobHandle;
                // dont call complete here? so that it can run async? 
                //jobHandle.Complete();
                break;
            case RunningType.Parallel:
                jobHandle = orientJob.ScheduleParallel(state.Dependency);
                state.Dependency = jobHandle;
                // dont call complete here? so that it can run async? 
                //jobHandle.Complete();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
}

[BurstCompile]
public partial struct OrientateCanonsJob : IJobEntity
{
    public float deltaTime;
    public float globalTime;
    [ReadOnly, DeallocateOnJobCompletion] public NativeArray<Entity> FighterEntities;
    [ReadOnly] public ComponentLookup<LocalToWorld> FighterLookup;
    public int FighterCount;
    [ReadOnly] public ComponentLookup<LocalToWorld> ParentLocalToWorldLookup;
    public EntityCommandBuffer.ParallelWriter ECB;
    public Entity LaserPrefab;
    public Entity BlastVFXPrefab;

    void Execute([ChunkIndexInQuery] int sortKey, ref LocalTransform transform, ref Canon canon, in LocalToWorld localToWorld, in Parent parent)
    {
        if (FighterCount == 0)
        {
            canon.Target = float3.zero;
            canon.IsAimingAtTarget = false;
            return;
        }

        int canonID = (int)(transform.Position.x + transform.Position.y + transform.Position.z);
        bool foundTarget = false;

        // try three times to find a fighter, else just dont fire 
        // also introduces randomness, so the stardestroyers do not shoot all at once
        for (int i = 0; i < 10; i++)
        {
            int index = PseudoRandom(canonID, i, globalTime, FighterCount);
            var fighter = FighterEntities[index];

            if (!FighterLookup.HasComponent(fighter))
                continue;

            float3 fighterPos = FighterLookup[fighter].Position;

            if ((canon.IsTop && fighterPos.y < localToWorld.Position.y) || (!canon.IsTop && fighterPos.y > localToWorld.Position.y))
                continue;

            canon.Target = fighterPos;
            foundTarget = true;
            break;
        }

        if (!foundTarget)
        {
            canon.Target = float3.zero;
            canon.IsAimingAtTarget = false;
            return;
        }

        quaternion parentWorldRotation = quaternion.identity;

        if (ParentLocalToWorldLookup.HasComponent(parent.Value))
        {
            parentWorldRotation = ParentLocalToWorldLookup[parent.Value].Rotation;
        }

        float3 direction = canon.Target - localToWorld.Position;
        if (math.lengthsq(direction) < 0.0001f)
        {
            canon.IsAimingAtTarget = false;
            return;
        }

        direction = math.normalize(direction);
        quaternion desiredWorldRotation = quaternion.LookRotationSafe(direction, math.up());
        quaternion desiredLocalRotation = math.mul(math.inverse(parentWorldRotation), desiredWorldRotation);

        float t = math.clamp(canon.RotationSpeed * deltaTime, 0f, 1f);
        transform.Rotation = math.slerp(transform.Rotation, desiredLocalRotation, t);
        float3 canonWorldForward = math.mul(parentWorldRotation, math.mul(transform.Rotation, new float3(0, 0, 1)));
        float angleDifference = math.degrees(math.acos(math.clamp(math.dot(canonWorldForward, direction), -1f, 1f)));
        canon.IsAimingAtTarget = angleDifference < 10f;

        // spawn laser thing
        if (!canon.IsAimingAtTarget)
        {
            canon.CurrentCoolDown += deltaTime; return;
        }
        if (canon.CurrentCoolDown < canon.CoolDownTime) { canon.CurrentCoolDown += deltaTime; return; }

        canon.CurrentCoolDown = 0f;
        canon.Target = float3.zero;

        direction = math.normalize(localToWorld.Forward);

        Entity laser = ECB.Instantiate(sortKey, LaserPrefab);
        Entity vfx = ECB.Instantiate(sortKey, BlastVFXPrefab);

        // VFX entity
        ECB.SetComponent(sortKey, vfx, new LocalTransform
        {
            Position = localToWorld.Position,
            Rotation = localToWorld.Rotation,
            Scale = 1
        });
        ECB.AddComponent<LaserVFX>(sortKey, vfx);
        ECB.AddComponent(sortKey, vfx, new TimedDestructionComponent
        {
            lifeTime = 1f,
            elapsedTime = 0f
        });
        ECB.AddComponent(sortKey, vfx, new HealthComponent
        {
            Health = 1f
        });

        // Laser entity
        ECB.SetComponent(sortKey, laser, new LocalTransform
        {
            Position = localToWorld.Position,
            Rotation = localToWorld.Rotation,
            Scale = 1
        });
        ECB.AddComponent(sortKey, laser, new LaserSD
        {
            Direction = direction,
            Speed = 100f
        });
        ECB.AddComponent(sortKey, laser, new TimedDestructionComponent
        {
            lifeTime = 2.5f,
            elapsedTime = 0f
        });
        ECB.AddComponent(sortKey, laser, new HealthComponent
        {
            Health = 1f
        });
        ECB.AddBuffer<HitBufferElement>(sortKey, laser);

    }
    int PseudoRandom(int canonID, int callID, float globalTime, int range)
    {
        return (int)(
            math.frac(
                Mathf.Sin(
                    (canonID + callID) * 12.9898f +
                    (canonID * globalTime) * 78.233f
                ) * 43758.5453f
            ) * range
        );
    }

    [BurstCompile]
    public partial struct CheapOrientationJob : IJobEntity
    {
        public float deltaTime;

        void Execute(
            ref LocalTransform transform,
            ref Canon canon,
            ref LocalToWorld localToWorld
        )
        {
            int canonID = (int)(
                transform.Position.x +
                transform.Position.y +
                transform.Position.z
            );

            // set canon target to a random point in space
            canon.Target = new float3(
                PseudoRandom(canonID, 1, deltaTime, 1000) - 500,
                PseudoRandom(canonID, 2, deltaTime, 1000) - 500,
                PseudoRandom(canonID, 3, deltaTime, 1000) - 500
            );

            float3 direction = canon.Target - localToWorld.Position;
            if (math.lengthsq(direction) < 0.0001f)
            {
                canon.IsAimingAtTarget = false;
                return;
            }

            direction = math.normalize(direction);

            quaternion desiredWorldRotation =
                quaternion.LookRotationSafe(direction, math.up());

            float t = math.clamp(
                canon.RotationSpeed * deltaTime,
                0f,
                1f
            );

            transform.Rotation = math.slerp(
                transform.Rotation,
                desiredWorldRotation,
                t
            );

            float3 canonWorldForward = math.mul(
                transform.Rotation,
                new float3(0, 0, 1)
            );

            float angleDifference = math.degrees(
                math.acos(
                    math.clamp(
                        math.dot(canonWorldForward, direction),
                        -1f,
                        1f
                    )
                )
            );

            canon.IsAimingAtTarget = angleDifference < 10f;
        }

        int PseudoRandom(int canonID, int callID, float globalTime, int range)
        {
            return (int)(
                math.frac(
                    Mathf.Sin(
                        (canonID + callID) * 12.9898f +
                        (canonID * globalTime) * 78.233f
                    ) * 43758.5453f
                ) * range
            );
        }
    }
}