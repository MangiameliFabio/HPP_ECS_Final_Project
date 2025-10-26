using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial struct FighterMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new MoveForwardJob
        {
            deltaTime = SystemAPI.Time.DeltaTime
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
public partial struct MoveForwardJob : IJobEntity
{
    public float deltaTime;

    void Execute(ref LocalTransform transform, ref Fighter fighter)
    {
        float3 alignmentDirection = math.normalizesafe(fighter.AlignmentDirection, float3.zero);
        float3 avoidanceDirection = math.normalizesafe(fighter.AvoidanceDirection, float3.zero);
        float3 neighbourForceDirection = math.normalizesafe(fighter.NeighbourCounterForceDirection, float3.zero);
        
        float3 toCenterDir = float3.zero;
        if (!fighter.CrowdCenter.Equals(float3.zero))
            toCenterDir = math.normalizesafe(fighter.CrowdCenter - transform.Position);
        
        fighter.TargetDirection =  math.normalizesafe(new float3(100f,100f,100f) - transform.Position);

        float3 newDirection =
            alignmentDirection * fighter.AlignmentFactor +
            avoidanceDirection * fighter.AvoidanceFactor +
            neighbourForceDirection * fighter.NeighbourCounterForceFactor +
            fighter.TargetDirection * fighter.TargetTrendFactor +
            toCenterDir * fighter.CrowdingFactor;
        
        quaternion targetRot = transform.Rotation;
        if (!newDirection.Equals(float3.zero))
            targetRot = quaternion.LookRotationSafe(newDirection, math.up());
        
        float angle = math.acos(math.clamp(math.dot(transform.Rotation.value, targetRot.value), -1f, 1f)) * 2f;
        
        float normalizedAngle = math.saturate(angle / math.PI);
        
        float dynamicRotationSpeed = math.lerp(fighter.MinRotationSpeed, fighter.MaxRotationSpeed, normalizedAngle); 
        quaternion newRot = math.slerp(transform.Rotation, targetRot, dynamicRotationSpeed * deltaTime);
        transform.Rotation = math.normalize(newRot);

        float dynamicSpeed = math.lerp(fighter.MinSpeed, fighter.MaxSpeed, normalizedAngle);
        transform.Position += transform.Forward() * dynamicSpeed * deltaTime;
        
        fighter.AlignmentDirection = float3.zero;
        fighter.AvoidanceDirection = float3.zero;
        fighter.TargetDirection = float3.zero;
        fighter.CrowdCenter = float3.zero;
    }
}