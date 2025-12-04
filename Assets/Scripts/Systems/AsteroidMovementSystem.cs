using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial struct AsteroidMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new MoveAsteroidJob()
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
public partial struct MoveAsteroidJob : IJobEntity
{
    public float deltaTime;

    void Execute(ref LocalTransform transform, ref Asteroid asteroid)
    {
        float3 angularVelocity = asteroid.AngularVelocity;
        float angle = math.length(angularVelocity) * deltaTime;
        
        if (angle > 0f)
        {
            float3 axis = math.normalize(angularVelocity);
            quaternion deltaRotation = quaternion.AxisAngle(axis, angle);
            
            transform.Rotation = math.normalize(math.mul(deltaRotation, transform.Rotation));
        }
        
        transform.Position += asteroid.LinearVelocity * deltaTime;
    }
}