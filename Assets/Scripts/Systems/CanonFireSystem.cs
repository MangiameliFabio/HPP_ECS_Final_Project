using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Physics.Systems;

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
        // Query all fighters
        var fighterQuery = SystemAPI.QueryBuilder()
            .WithAll<Fighter, LocalTransform, LocalToWorld>()
            .Build();

        var fighterLocalToWorld = fighterQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);
        var fighterLocalTransform = fighterQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var parentLocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);

        // Create job
        var job = new OrientateTurrentsJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            FighterWorldTransform = fighterLocalToWorld,
            FighterLocalTransform = fighterLocalTransform,
            ParentLocalToWorldLookup = parentLocalToWorldLookup
        };

        // Schedule
        var jobHandle = job.ScheduleParallel(state.Dependency);
        state.Dependency = jobHandle;

        // Proper disposal
        state.Dependency = fighterLocalToWorld.Dispose(state.Dependency);
        state.Dependency = fighterLocalTransform.Dispose(state.Dependency);

        jobHandle.Complete();

        // Shooting logic (main thread)
        var config = SystemAPI.GetSingleton<Config>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var laserTransform = state.EntityManager.GetComponentData<LocalTransform>(config.CruiserLaserPrefab);
        var vfxTransform = state.EntityManager.GetComponentData<LocalTransform>(config.CruiserLaserBlastVFX);

        foreach (var (canon, localToWorld, localTransform)
                 in SystemAPI.Query<RefRW<Canon>, RefRO<LocalToWorld>, RefRO<LocalTransform>>())
        {
            if (!canon.ValueRO.IsAimingAtTarget)
                continue;

            //Debug.DrawLine(localToWorld.ValueRO.Position, canon.ValueRO.Target, Color.red, 0.1f);

            if (canon.ValueRO.CurrentCoolDown >= canon.ValueRO.CoolDownTime)
            {
                var direction = math.normalize(localToWorld.ValueRO.Forward);

                var laserEntity = ecb.Instantiate(config.CruiserLaserPrefab);
                var vfxEntity = ecb.Instantiate(config.CruiserLaserBlastVFX);

                ecb.SetComponent(vfxEntity, new LocalTransform
                {
                    Position = localToWorld.ValueRO.Position,
                    Rotation = localToWorld.ValueRO.Rotation,
                    Scale = vfxTransform.Scale
                });
                ecb.SetComponent(vfxEntity, new LaserVFX());

                ecb.SetComponent(laserEntity, new LocalTransform
                {
                    Position = localToWorld.ValueRO.Position,
                    Rotation = localToWorld.ValueRO.Rotation,
                    Scale = 1f
                });

                ecb.SetComponent(laserEntity, new LaserSD
                {
                    Direction = direction,
                    Speed = 100f
                });

                canon.ValueRW.CurrentCoolDown = 0f;
                canon.ValueRW.Target = float3.zero;
            }
            else
            {
                canon.ValueRW.CurrentCoolDown += SystemAPI.Time.DeltaTime;
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
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

    [BurstCompile]
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

        // Get parent rotation
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
