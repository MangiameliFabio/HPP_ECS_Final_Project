using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial struct FighterMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<FighterSettings>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var fighterSettings = SystemAPI.GetSingleton<FighterSettings>();
        
        var job = new MoveForwardJob
        {
            Settings = fighterSettings,
            deltaTime = SystemAPI.Time.DeltaTime
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
public partial struct MoveForwardJob : IJobEntity
{
    [ReadOnly] public FighterSettings Settings;
    public float deltaTime;

    void Execute(ref LocalTransform transform, ref FighterComponent fighterComponent, ref HealthComponent healthComponent)
    {
        
        if (healthComponent.Health <= 0)
            return;

        float3 alignmentDirection = math.normalizesafe(fighterComponent.AlignmentDirection, float3.zero);
        float3 avoidanceDirection = math.normalizesafe(fighterComponent.AvoidanceDirection, float3.zero);
        float3 neighbourForceDirection = math.normalizesafe(fighterComponent.NeighbourCounterForceDirection, float3.zero);
        
        float3 toCenterDir = float3.zero;
        if (!fighterComponent.CrowdCenter.Equals(float3.zero))
            toCenterDir = math.normalizesafe(fighterComponent.CrowdCenter - transform.Position);
        
        fighterComponent.TargetDirection =  math.normalizesafe(fighterComponent.CurrentTargetPosition - transform.Position, float3.zero);

        float3 newDirection =
            alignmentDirection * Settings.AlignmentFactor +
            avoidanceDirection * Settings.AvoidanceFactor +
            neighbourForceDirection * Settings.NeighbourCounterForceFactor +
            fighterComponent.TargetDirection * Settings.TargetTrendFactor +
            toCenterDir * Settings.CrowdingFactor;
        
        quaternion targetRot = transform.Rotation;
        if (!newDirection.Equals(float3.zero))
            targetRot = quaternion.LookRotationSafe(newDirection, math.up());
        
        float angle = math.acos(math.clamp(math.dot(transform.Rotation.value, targetRot.value), -1f, 1f)) * 2f;
        
        float normalizedAngle = math.saturate(angle / math.PI);
        
        float dynamicRotationSpeed = math.lerp(Settings.MinRotationSpeed, Settings.MaxRotationSpeed, normalizedAngle); 
        quaternion newRot = math.slerp(transform.Rotation, targetRot, dynamicRotationSpeed * deltaTime);
        transform.Rotation = math.normalize(newRot);

        float dynamicSpeed = math.lerp(Settings.MinSpeed, Settings.MaxSpeed, math.clamp(200 / math.length(newDirection), 0 , 1));
        transform.Position += transform.Forward() * dynamicSpeed * deltaTime;
        
        fighterComponent.AlignmentDirection = float3.zero;
        fighterComponent.AvoidanceDirection = float3.zero;
        fighterComponent.TargetDirection = float3.zero;
        fighterComponent.CrowdCenter = float3.zero;
    }
}