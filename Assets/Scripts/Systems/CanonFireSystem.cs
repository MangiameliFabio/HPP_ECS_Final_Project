using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

public partial struct CanonFireSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<Canon>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // --- Prepare fighter arrays and lookup for the job ---
        var fighterQuery = SystemAPI.QueryBuilder()
            .WithAll<Fighter, LocalTransform, LocalToWorld>()
            .Build();

        var fighterLocalToWorld = fighterQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);
        var fighterLocalTransform = fighterQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        // Prepare lookup and update it before scheduling
        var parentLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
        parentLocalToWorldLookup.Update(ref state);

        // Schedule orientation job
        var orientJob = new OrientateTurrentsJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            FighterWorldTransform = fighterLocalToWorld,
            FighterLocalTransform = fighterLocalTransform,
            ParentLocalToWorldLookup = parentLocalToWorldLookup
        };

        var jobHandle = orientJob.ScheduleParallel(state.Dependency);

        // Wait for job to finish before accessing/writing the same component data on main thread.
        jobHandle.Complete();

        // Now it's safe to dispose arrays
        fighterLocalToWorld.Dispose();
        fighterLocalTransform.Dispose();

        // --- Main-thread shooting logic ---
        var config = SystemAPI.GetSingleton<Config>();

        // Use end-sim ECB (do NOT call Playback; system will play it back)
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // Read transforms from prefab once (prefab entities exist in EntityManager)
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

                // Set/override LocalTransform for instantiated entities
                // (Assuming prefab already has LocalTransform, use SetComponent; otherwise use AddComponent)
                ecb.SetComponent(vfxEntity, new LocalTransform
                {
                    Position = localToWorld.ValueRO.Position,
                    Rotation = localToWorld.ValueRO.Rotation,
                    Scale = vfxPrefabTransform.Scale
                });
                // Add the VFX tag/component if prefab does not include it
                ecb.AddComponent(vfxEntity, new LaserVFX());

                // Add components that are likely not part of prefab archetype
                ecb.AddComponent(vfxEntity, new TimedDestructionComponent
                {
                    lifeTime = 4f,
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
                    lifeTime = 8f,
                    elapsedTime = 0f
                });
                ecb.AddComponent(laserEntity, new HealthComponent
                {
                    Health = 1f
                });

                // reset cooldown/target locally (we are on main thread; safe)
                canon.ValueRW.CurrentCoolDown = 0f;
                canon.ValueRW.Target = float3.zero;
            }
            else
            {
                canon.ValueRW.CurrentCoolDown += SystemAPI.Time.DeltaTime;
            }
        }

        // No Playback() here — use EndSimulationEntityCommandBufferSystem playback
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
}

[BurstCompile]
public partial struct OrientateTurrentsJob : IJobEntity
{
    public float deltaTime;

    [ReadOnly] public NativeArray<LocalToWorld> FighterWorldTransform;
    [ReadOnly] public NativeArray<LocalTransform> FighterLocalTransform;
    [ReadOnly] public ComponentLookup<LocalToWorld> ParentLocalToWorldLookup;

    void Execute(ref LocalTransform transform, ref Canon canon, in LocalToWorld localToWorld, in Parent parent)
    {
        float minDistSq = float.MaxValue;
        float3 bestTarget = float3.zero;
        bool foundTarget = false;

        for (int i = 0; i < FighterWorldTransform.Length; i++)
        {
            var fighterWorld = FighterWorldTransform[i];
            float distSq = math.distancesq(localToWorld.Position, fighterWorld.Position);
            if (distSq < minDistSq)
            {
                if ((canon.IsTop && fighterWorld.Position.y >= localToWorld.Position.y) ||
                    (!canon.IsTop && fighterWorld.Position.y <= localToWorld.Position.y))
                {
                    minDistSq = distSq;
                    bestTarget = fighterWorld.Position;
                    foundTarget = true;
                }
            }
        }

        if (!foundTarget)
        {
            canon.Target = float3.zero;
            canon.IsAimingAtTarget = false;
            return;
        }

        canon.Target = bestTarget;

        // Get parent rotation safely
        quaternion parentWorldRotation = quaternion.identity;
        if (ParentLocalToWorldLookup.HasComponent(parent.Value))
        {
            parentWorldRotation = ParentLocalToWorldLookup[parent.Value].Rotation;
        }

        // Compute desired world rotation
        float3 direction = canon.Target - localToWorld.Position;
        if (math.lengthsq(direction) < 0.0001f)
        {
            canon.IsAimingAtTarget = false;
            return;
        }
        direction = math.normalize(direction);
        quaternion desiredWorldRotation = quaternion.LookRotationSafe(direction, math.up());

        // Convert to local rotation
        quaternion desiredLocalRotation = math.mul(math.inverse(parentWorldRotation), desiredWorldRotation);

        // Lerp local rotation
        float t = math.clamp(canon.RotationSpeed * deltaTime, 0f, 1f);
        transform.Rotation = math.slerp(transform.Rotation, desiredLocalRotation, t);

        // Optional: aiming accuracy check
        float3 canonWorldForward = math.mul(parentWorldRotation, math.mul(transform.Rotation, new float3(0, 0, 1)));
        float angleDifference = math.degrees(math.acos(math.clamp(math.dot(canonWorldForward, direction), -1f, 1f)));
        canon.IsAimingAtTarget = angleDifference < 10f;
    }
}
