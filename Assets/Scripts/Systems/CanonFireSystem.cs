using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using Unity.IO.LowLevel.Unsafe;
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
        
        var fighterQuery = SystemAPI.QueryBuilder()
            .WithAll<Fighter, LocalTransform, LocalToWorld>()
            .Build();

        var fighterLocalToWorld = fighterQuery.ToComponentDataArray<LocalToWorld>(Allocator.TempJob);

        var job = new OrientateTurrentsJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            FighterWorldTransform = fighterLocalToWorld
        };

        var orientJobHandle = job.ScheduleParallel(state.Dependency);

        state.Dependency = orientJobHandle;
        state.Dependency = fighterLocalToWorld.Dispose(state.Dependency);
        orientJobHandle.Complete();

        // then shoot (initiate laser entities) on main thread
        var config = SystemAPI.GetSingleton<Config>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var laserTransform = state.EntityManager.GetComponentData<LocalTransform>(config.CruiserLaserPrefab);
        var vfxTransform = state.EntityManager.GetComponentData<LocalTransform>(config.CruiserLaserBlastVFX);

        foreach (var (canon, localToWorld, localTransform)
                 in SystemAPI.Query<RefRW<Canon>, RefRO<LocalToWorld>, RefRO<LocalTransform>>())
        {
            if (!canon.ValueRO.IsAimingAtTarget)
                continue;

            Debug.DrawLine(localToWorld.ValueRO.Position, canon.ValueRO.Target, Color.red, 0.1f);

            if (canon.ValueRO.CurrentCoolDown >= canon.ValueRO.CoolDownTime && canon.ValueRO.IsAimingAtTarget)
            {
                // for now just shoot at the first swarm center or forward when there is none just to debug
                var direction = math.normalize(localToWorld.ValueRO.Forward);

                var laserEntity = ecb.Instantiate(config.CruiserLaserPrefab);
                var vfxEntity = ecb.Instantiate(config.CruiserLaserBlastVFX);
                ecb.SetComponent(vfxEntity, new LocalTransform
                {
                    Position = localToWorld.ValueRO.Position,
                    Rotation = localToWorld.ValueRO.Rotation,
                    Scale = vfxTransform.Scale
                });
                ecb.SetComponent(vfxEntity, new LaserVFX { });

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
                // increment cooldown
                canon.ValueRW.CurrentCoolDown += SystemAPI.Time.DeltaTime;
            }
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}

// rotate canon in swarm direction (maybe) and shoot
[BurstCompile]
public partial struct OrientateTurrentsJob : IJobEntity
{
    public float deltaTime;
    [ReadOnly] public NativeArray<LocalToWorld> FighterWorldTransform;


    void Execute(ref LocalTransform transform, ref Canon canon, in LocalToWorld localToWorld)
    {
        float minDistSq = float.MaxValue;
        float3 bestTarget = float3.zero;
        bool foundTarget = false;

        foreach (var fighterTf in FighterWorldTransform)
        {
            float distSq = math.distancesq(localToWorld.Position, fighterTf.Position);
            if (distSq < minDistSq)
            {
                if (fighterTf.Position.y <= localToWorld.Position.y)
                {
                    minDistSq = distSq;
                    bestTarget = fighterTf.Position;
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

        float3 direction = math.normalize(canon.Target - localToWorld.Position);

        // lerp the rotation of the canon to the direction of the closest swarm center  
        quaternion targetRotation = quaternion.LookRotationSafe(direction, math.up());
        transform.Rotation = math.slerp(transform.Rotation, targetRotation, canon.RotationSpeed * deltaTime);
        
        // compare targetRotation and current rotation
        float angleDifference = math.degrees(math.acos(math.clamp(math.dot(transform.Rotation.value, targetRotation.value), -1f, 1f)));

        if (angleDifference < 35f)
        {
            canon.IsAimingAtTarget = true; // Consider aiming complete if the angle difference is less than 5 degrees
        }
        else
        {
            canon.IsAimingAtTarget = false;
        }
    }
}