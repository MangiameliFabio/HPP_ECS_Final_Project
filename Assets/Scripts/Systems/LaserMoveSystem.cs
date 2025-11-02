using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

public partial struct LaserMoveSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new MoveLaser
        {
            deltaTime = SystemAPI.Time.DeltaTime,
        };

        var handle = job.ScheduleParallel(state.Dependency);
        state.Dependency = handle;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}

[BurstCompile]
public partial struct MoveLaser : IJobEntity
{
    public float deltaTime;

    void Execute(ref LocalTransform transform, ref LaserSD laser)
    {
        // rotate laser towards direction property
        float3 direction = laser.Direction;
        quaternion targetRot = quaternion.LookRotationSafe(direction, math.up());
        transform.Rotation = math.slerp(transform.Rotation, targetRot, 1.0f);

        transform.Position += transform.Forward() * laser.Speed * deltaTime;
    }
}