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
            LocalTransformLookup = localTransformLookup,
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
        
        public void Execute(ref Fighter fighter, in LocalTransform localTransform, in DynamicBuffer<AvoidingEntityBufferElement> avoidanceBuffer, in Entity entity)
        {
            float3 averageAvoidDir = float3.zero;
            
            int size = avoidanceBuffer.Length;
            if (size == 0)
            {
                return;
            }

            for (int i = 0; i < avoidanceBuffer.Length; i++)
            {
                var avoidingEntity = avoidanceBuffer[i];

                // Skip if entity does not exist in LocalTransformLookup
                if (!LocalTransformLookup.HasComponent(avoidingEntity.AvoidingEntity))
                    continue;

                float3 direction = avoidingEntity.HitPosition - localTransform.Position;

                float distance = math.lengthsq(direction);

                if (!(distance > 0f)) continue;
                direction = math.normalize(direction);
                
                float lerpedDistance = math.lerp(0f, math.pow(10f, 2f), distance);
                float distanceFactor = math.clamp(lerpedDistance, 0, 1);
                    
                averageAvoidDir += direction * distanceFactor;
            }

            fighter.AvoidanceDirection -= averageAvoidDir;
        }
    }
}

public struct AvoidingEntityBufferElement : IBufferElementData
{
    public Entity AvoidingEntity;
    public float3 HitPosition;
}