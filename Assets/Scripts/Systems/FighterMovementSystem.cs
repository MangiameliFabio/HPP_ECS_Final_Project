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

    void Execute(ref LocalTransform transform, in Fighter fighter)
    {
        float3 alignmentDirection = math.normalizesafe(fighter.AlignmentDirection, float3.zero);
        
        if (math.lengthsq(alignmentDirection) < 1e-5f)
            alignmentDirection = transform.Forward();
        
        float3 toCenterDir = transform.Forward();
        if (!fighter.CrowdCenter.Equals(float3.zero))
        {
            toCenterDir = math.normalizesafe(fighter.CrowdCenter - transform.Position, float3.zero);
        }
        
        float3 avoidanceDirection = float3.zero;
        if (!fighter.AvoidanceDirection.Equals(float3.zero))
        {
            avoidanceDirection = math.normalizesafe(fighter.AvoidanceDirection, float3.zero);
        }

        float3 newDirection = alignmentDirection * fighter.AlignmentFactor + toCenterDir * fighter.CrowdingFactor + avoidanceDirection;
        
        quaternion targetRot = quaternion.LookRotationSafe(newDirection, math.up());
        
        float angle = math.acos(math.clamp(math.dot(transform.Rotation.value, targetRot.value), -1f, 1f)) * 2f;
        
        float normalizedAngle = math.saturate(angle / math.PI);
        
        float dynamicRotationSpeed = math.lerp(fighter.MinRotationSpeed, fighter.MaxRotationSpeed, normalizedAngle); 
        quaternion newRot = math.slerp(transform.Rotation, targetRot, dynamicRotationSpeed * deltaTime);
        transform.Rotation = math.normalize(newRot);

        float dynamicSpeed = math.lerp(fighter.MinSpeed, fighter.MaxSpeed, normalizedAngle);
        transform.Position += transform.Forward() * dynamicSpeed * deltaTime;
    }
}