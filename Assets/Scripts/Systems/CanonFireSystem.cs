using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

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
        //first rotate the canon towards the swarm center in a job

        var job = new OrientateTurrentsJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
        };

        var handle = job.ScheduleParallel(state.Dependency);
        state.Dependency = handle;
        handle.Complete();

        // then shoot (initiate laser entities) on main thread

        var config = SystemAPI.GetSingleton<Config>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var laserTransform = state.EntityManager.GetComponentData<LocalTransform>(config.StarDestroyerLaser);

        foreach (var (canon, localToWorld, localTransform, swarmCenters)
                 in SystemAPI.Query<RefRW<Canon>, RefRO<LocalToWorld>, RefRO<LocalTransform>, DynamicBuffer<SwarmCenterBuffer>>())
        {
            if (canon.ValueRO.CurrentCoolDown >= canon.ValueRO.CoolDownTime && canon.ValueRO.IsAimingAtTarget)
            {
                // for now just shoot at the first swarm center or forward when there is none just to debug
                var direction = math.normalize(localTransform.ValueRO.Forward());

                var laserEntity = ecb.Instantiate(config.StarDestroyerLaser);

                ecb.SetComponent(laserEntity, new LocalTransform
                {
                    Position = localToWorld.ValueRO.Position,
                    Rotation = localToWorld.ValueRO.Rotation,
                    Scale = 1f
                });

                ecb.SetComponent(laserEntity, new LaserSD
                {
                    Direction = direction,
                    Speed = 50f
                });

                canon.ValueRW.CurrentCoolDown = 0f;
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

    void Execute(ref LocalTransform transform, ref Canon canon, in DynamicBuffer<SwarmCenterBuffer> swarmCenters, ref LocalToWorld localToWorld)
    {
        float3 direction;

        if (swarmCenters.Length == 0)
        {
            return;
        }
        else
        {
            // iterate through all swarm centers and chose the one closest to the canon
            float minDistance = float.MaxValue;
            float3 closestSwarmCenter = float3.zero;

            foreach (var swarmCenter in swarmCenters)
            {
                float distance = math.distance(localToWorld.Position, swarmCenter.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestSwarmCenter = swarmCenter.Position;
                }
            }

            direction = math.normalize(closestSwarmCenter - localToWorld.Position);
        }

        // lerp the rotation of the canon to the direction of the closest swarm center  
        quaternion targetRotation = quaternion.LookRotationSafe(direction, math.up());
        transform.Rotation = math.slerp(transform.Rotation, targetRotation, canon.RotationSpeed * deltaTime);
        
        // compare targetRotation and current rotation
        float angleDifference = math.degrees(math.acos(math.clamp(math.dot(transform.Rotation.value, targetRotation.value), -1f, 1f)));

        if (angleDifference < 15f)
        {
            canon.IsAimingAtTarget = true; // Consider aiming complete if the angle difference is less than 5 degrees
        }
        else
        {
            canon.IsAimingAtTarget = false;
        }

    }
}