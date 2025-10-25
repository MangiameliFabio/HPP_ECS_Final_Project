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
        
        job.ScheduleParallel();
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
        float3 alignmentDirection = math.normalizesafe(fighter.alignmentDirection, float3.zero);
        
        if (math.lengthsq(alignmentDirection) < 1e-5f)
            alignmentDirection = transform.Forward();
        
        float3 toCenterDir = transform.Forward();
        if (!fighter.crowdCenter.Equals(float3.zero))
        {
            toCenterDir = math.normalizesafe(fighter.crowdCenter - transform.Position, float3.zero);
        }
        
        float3 avoidanceDirection = math.normalizesafe(fighter.avoidanceDirection, float3.zero);
        
        if (math.lengthsq(avoidanceDirection) < 1e-5f)
            avoidanceDirection = transform.Forward();
        
        float3 newDirection = alignmentDirection * 1f + toCenterDir * 0.5f + avoidanceDirection * 0.5f;
        
        quaternion targetRot = quaternion.LookRotationSafe(newDirection, math.up());
        quaternion newRot = math.slerp(transform.Rotation, targetRot, 1f * deltaTime);

        transform.Rotation = math.normalize(newRot);
        transform.Position += transform.Forward() * 3f * deltaTime;
    }
}