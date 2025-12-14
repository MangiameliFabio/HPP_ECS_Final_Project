using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

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
        var fighterQuery = SystemAPI.QueryBuilder()
            .WithAll<Fighter, LocalTransform, LocalToWorld>()
            .Build();

        var parentLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
        parentLocalToWorldLookup.Update(ref state);

        var fighterLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
        fighterLookup.Update(ref state);

        var fighterCount = fighterQuery.CalculateEntityCount();
        var fighterEntities = fighterQuery.ToEntityArray(Allocator.TempJob);

        var orientJob = new OrientateCanonsJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            globalTime = (float)SystemAPI.Time.ElapsedTime,
            FighterEntities = fighterEntities,
            FighterLookup = fighterLookup,
            FighterCount = fighterCount,
            ParentLocalToWorldLookup = parentLocalToWorldLookup
        };

        var config = SystemAPI.GetSingleton<Config>();
        JobHandle jobHandle;
        switch (config.RunType)
        {
            case RunningType.MainThread:
                orientJob.Run();
                break;
            case RunningType.Scheduled:
                jobHandle = orientJob.Schedule(state.Dependency);
                state.Dependency = jobHandle;
                jobHandle.Complete();
                break;
            case RunningType.Parallel:
                jobHandle = orientJob.ScheduleParallel(state.Dependency);
                state.Dependency = jobHandle;
                jobHandle.Complete();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var laserPrefabTransform = state.EntityManager.GetComponentData<LocalTransform>(config.StarDestroyerLaserPrefab);
        var vfxPrefabTransform = state.EntityManager.GetComponentData<LocalTransform>(config.StarDestroyerBlastVFX);

        foreach (var (canon, localToWorld, localTransform)
                 in SystemAPI.Query<RefRW<Canon>, RefRO<LocalToWorld>, RefRO<LocalTransform>>())
        {
            if (!canon.ValueRO.IsAimingAtTarget)
                continue;

            if (canon.ValueRO.CurrentCoolDown >= canon.ValueRO.CoolDownTime)
            {
                var direction = math.normalize(localToWorld.ValueRO.Forward);

                var laserEntity = ecb.Instantiate(config.StarDestroyerLaserPrefab);
                var vfxEntity = ecb.Instantiate(config.StarDestroyerBlastVFX);

                ecb.SetComponent(vfxEntity, new LocalTransform
                {
                    Position = localToWorld.ValueRO.Position,
                    Rotation = localToWorld.ValueRO.Rotation,
                    Scale = vfxPrefabTransform.Scale
                });
                ecb.AddComponent(vfxEntity, new LaserVFX());

                ecb.AddComponent(vfxEntity, new TimedDestructionComponent
                {
                    lifeTime = 1f,
                    elapsedTime = 0f
                });
                ecb.AddComponent(vfxEntity, new HealthComponent
                {
                    Health = 1f
                });

                // Laser entity transform & data
                ecb.SetComponent(laserEntity, new LocalTransform
                {
                    Position = localToWorld.ValueRO.Position,
                    Rotation = localToWorld.ValueRO.Rotation,
                    Scale = laserPrefabTransform.Scale
                });

                // If LaserSD is not on the prefab archetype, AddComponent ; otherwise SetComponent
                ecb.AddComponent(laserEntity, new LaserSD
                {
                    Direction = direction,
                    Speed = 100f
                });

                ecb.AddComponent(laserEntity, new TimedDestructionComponent
                {
                    lifeTime = 2.5f,
                    elapsedTime = 0f
                });
                ecb.AddComponent(laserEntity, new HealthComponent
                {
                    Health = 1f
                });
                ecb.AddBuffer<HitBufferElement>(laserEntity);

                // reset cooldown/target locally (we are on main thread; safe)
                canon.ValueRW.CurrentCoolDown = 0f;
                canon.ValueRW.Target = float3.zero;
            }
            else
            {
                canon.ValueRW.CurrentCoolDown += SystemAPI.Time.DeltaTime;
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}

[BurstCompile]
public partial struct OrientateCanonsJob : IJobEntity
{
    public float deltaTime;
    public float globalTime;

    [ReadOnly, DeallocateOnJobCompletion]
    public NativeArray<Entity> FighterEntities;

    [ReadOnly]
    public ComponentLookup<LocalToWorld> FighterLookup;

    public int FighterCount;

    [ReadOnly]
    public ComponentLookup<LocalToWorld> ParentLocalToWorldLookup;

    void Execute(ref LocalTransform transform, ref Canon canon, in LocalToWorld localToWorld, in Parent parent)
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
            {
                continue;
            }

            float3 fighterPos = FighterLookup[fighter].Position;

            if ((canon.IsTop && fighterPos.y < localToWorld.Position.y) ||
                (!canon.IsTop && fighterPos.y > localToWorld.Position.y))
            {
                continue;
            }

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
    }
    int PseudoRandom(int canonID, int callID, float globalTime, int range)
    {
        return (int)((math.frac(Mathf.Sin((canonID + callID) * 12.9898f + (canonID* globalTime) * 78.233f) * 43758.5453f))*range);
    }
}
