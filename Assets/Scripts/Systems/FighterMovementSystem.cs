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
        // Ensure direction is normalized and nonzero
        float3 dir = math.normalizesafe(fighter.alignmentDirection, float3.zero);
        
        if (math.lengthsq(dir) < 1e-5f)
            dir = transform.Forward();
        
        quaternion targetRot = quaternion.LookRotationSafe(dir, math.up());
        quaternion newRot = math.slerp(transform.Rotation, targetRot, 1f * deltaTime);

        transform.Rotation = math.normalize(newRot);
        transform.Position += transform.Forward() * 1f * deltaTime;
    }
}