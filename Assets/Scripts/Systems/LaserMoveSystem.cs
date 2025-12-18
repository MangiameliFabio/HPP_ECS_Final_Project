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
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new MoveLaser
        {
            deltaTime = SystemAPI.Time.DeltaTime,
        };

        var config = SystemAPI.GetSingleton<Config>();
        switch (config.RunType)
        {
            case RunningType.MainThread:
                state.Dependency.Complete();
                job.Run();
                break;
            case RunningType.Scheduled:
                state.Dependency = job.Schedule(state.Dependency);
                break;
            case RunningType.Parallel:
                state.Dependency = job.ScheduleParallel(state.Dependency);
                break;
            default:
                break;
        }
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