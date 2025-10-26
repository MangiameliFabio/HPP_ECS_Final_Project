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
        var avoidanceSphereLookup = SystemAPI.GetComponentLookup<AvoidanceSphere>(true);

        localTransformLookup.Update(ref state);
        avoidanceSphereLookup.Update(ref state);

        var job = new FighterAvoidanceJob
        {
            LocalTransformLookup = localTransformLookup,
            AvoidanceSphereLookup = avoidanceSphereLookup
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
        [ReadOnly] public ComponentLookup<AvoidanceSphere> AvoidanceSphereLookup;
        
        public void Execute(ref Fighter fighter, in LocalTransform localTransform, in DynamicBuffer<AvoidingEntity> avoidanceBuffer, in Entity entity)
        {
            float3 averageAvoidDir = float3.zero;

            // if (CheckIfOutOfBounds(localTransform.Position))
            // {
            //     fighter.AvoidanceDirection -= localTransform.Position;
            // }
            
            int size = avoidanceBuffer.Length;
            if (size == 0)
            {
                return;
            }

            for (int i = 0; i < avoidanceBuffer.Length; i++)
            {
                var element = avoidanceBuffer[i];
                
                float3 direction = LocalTransformLookup[element.entity].Position - localTransform.Position;
                float distance = math.lengthsq(direction);
                
                if (distance > 0f)
                {
                    direction = math.normalize(direction);

                    if (!AvoidanceSphereLookup.HasComponent(element.entity))
                        continue;

                    float obstacleSphereRadius = AvoidanceSphereLookup[element.entity].Radius;
                    float maxRange = math.pow(fighter.NeighbourDetectionRadius + obstacleSphereRadius, 2f);
                    float distanceFactor = math.clamp(1 - distance / maxRange, 0, 1);
                    
                    averageAvoidDir += direction * distanceFactor;
                }
            }

            fighter.AvoidanceDirection -= averageAvoidDir;
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
}