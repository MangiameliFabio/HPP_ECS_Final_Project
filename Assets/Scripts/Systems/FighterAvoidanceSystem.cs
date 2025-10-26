using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

[BurstCompile]
public partial struct FighterAvoidanceSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);

        localTransformLookup.Update(ref state);

        var job = new FighterAvoidanceJob
        {
            LocalTransformLookup = localTransformLookup
        };

        var handle = job.ScheduleParallel(state.Dependency);
        state.Dependency = handle;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    partial struct FighterAvoidanceJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        
        public void Execute(ref Fighter fighter, in LocalTransform localTransform, in DynamicBuffer<AvoidingEntity> avoidanceBuffer, in Entity entity)
        {
            float3 averageAvoidDir = float3.zero;

            if (CheckIfOutOfBounds(localTransform.Position))
            {
                fighter.AvoidanceDirection -= localTransform.Position;
            }
            
            int size = avoidanceBuffer.Length;
            if (size == 0)
            {
                return;
            }

            for (int i = 0; i < avoidanceBuffer.Length; i++)
            {
                var element = avoidanceBuffer[i];
                
                var direction = localTransform.Position - LocalTransformLookup[element.entity].Position;
                var distance = math.lengthsq(direction);

                var fwDir = localTransform.Forward();
                direction = math.normalize(direction);
                var dot = math.dot(fwDir, direction);
                float angleFactor = math.lerp(0f, 1f, math.clamp(dot, 0f, 1f));

                float radiusSq = fighter.NeighbourDetectionRadius * fighter.NeighbourDetectionRadius;
                float distanceFactor = math.lerp(fighter.MinAvoidanceFactor, fighter.MaxAvoidanceFactor, math.unlerp(radiusSq, 0f, distance));

                if (distance > 0f)
                    averageAvoidDir += direction * distanceFactor * angleFactor * element.importanceFactor;
            }

            fighter.AvoidanceDirection = averageAvoidDir;
        }

        private bool CheckIfOutOfBounds(float3 point)
        {
            if (point.x > 30 || point.x < -30 || point.y > 30 || point.y < -30 || point.z > 30 || point.z < -30)
            {
                return true;
            }
            return false;
        }
    }
}

public struct AvoidingEntity : IBufferElementData
{
    public Entity entity;
    public float3 hitPosition;
    public float importanceFactor;
}