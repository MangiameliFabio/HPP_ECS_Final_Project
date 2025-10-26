using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial struct FighterSwarmSystem : ISystem
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

        var job = new FighterSwarmJob()
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
    partial struct FighterSwarmJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        void Execute(in DynamicBuffer<NearbyFighter> buffer, in LocalTransform localTransform, ref Fighter fighter, in Entity entity)
        {
            float size = buffer.Length;
            
            float3 averageDir = localTransform.Forward();
            float3 averagePosition = localTransform.Position;
            float3 averageCounterForceDir = float3.zero;
                
            var entityLocalTransform = LocalTransformLookup[entity];

            foreach (var element in buffer)
            {
                if (!LocalTransformLookup.HasComponent(element.entity))  
                    continue;
                var neigbourLocalTransform = LocalTransformLookup[element.entity];
                averagePosition += neigbourLocalTransform.Position;
                averageDir += neigbourLocalTransform.Forward();
                
                var direction = entityLocalTransform.Position - neigbourLocalTransform.Position;
                var distance = math.lengthsq(direction);
                
                float radiusSq = fighter.NeighbourDetectionRadius * fighter.NeighbourDetectionRadius;
                float distanceFactor = math.lerp(fighter.MinNeighbourCounterFactor, fighter.MaxNeighbourCounterFactor, math.unlerp(radiusSq, 0f, distance));
                
                if (distance > 0f)
                    averageCounterForceDir += math.normalize(direction) * distanceFactor;
            }

            fighter.CrowdCenter = averagePosition / size;
            fighter.AlignmentDirection = averageDir + averageCounterForceDir;
        }
    }
}