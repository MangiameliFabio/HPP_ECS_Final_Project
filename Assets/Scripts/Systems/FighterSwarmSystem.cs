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
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<FighterSettings>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        localTransformLookup.Update(ref state);
        
        var fighterSettings = SystemAPI.GetSingleton<FighterSettings>();
        
        var job = new FighterSwarmJob()
        {
            Settings = fighterSettings,
            LocalTransformLookup = localTransformLookup,
        };
        
        var config = SystemAPI.GetSingleton<Config>();
        switch (config.RunType)
        {
            case RunningType.MainThread:
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

    [BurstCompile]
    partial struct FighterSwarmJob : IJobEntity
    {
        [ReadOnly] public FighterSettings Settings;
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        void Execute(in DynamicBuffer<NearbyFighter> buffer, in LocalTransform localTransform, ref Fighter fighter, in Entity entity)
        {
            float size = buffer.Length;
            
            if (size == 0) return;
            
            float3 averageDir = localTransform.Forward();
            float3 averagePosition = float3.zero;
            float3 averageCounterForceDir = float3.zero;

            if (!LocalTransformLookup.HasComponent(entity))
                return;

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
                
                float radiusSq = Settings.NeighbourDetectionRadius * Settings.NeighbourDetectionRadius;
                float distanceFactor = math.lerp(0, 1, math.clamp(math.unlerp(radiusSq, 0f, distance), 0, 1));
                
                if (distance > 0f)
                    averageCounterForceDir += math.normalize(direction) * distanceFactor;
            }

            fighter.CrowdCenter = averagePosition / math.max(size, 1f);

            fighter.AlignmentDirection = averageDir;
            fighter.NeighbourCounterForceDirection = averageCounterForceDir;
        }
    }
}