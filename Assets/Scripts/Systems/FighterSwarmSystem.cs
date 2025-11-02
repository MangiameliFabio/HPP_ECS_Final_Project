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
        
        // collect swarm centers so we can assign them to the canon buffers in the next job
        NativeQueue<float3> swarmCenters = new NativeQueue<float3>(Allocator.TempJob);

        var swarmJob = new FighterSwarmJob()
        {
            LocalTransformLookup = localTransformLookup,
            queueWriter = swarmCenters.AsParallelWriter()
        };
        var swarmHandle = swarmJob.ScheduleParallel(state.Dependency);

        swarmHandle.Complete();
        var updateCanonBufferJob = new UpdateCanonSwarmCentersJob()
        {
            SwarmCenters = swarmCenters.ToArray(Allocator.TempJob)
        };
        var canonUpdateHandle = updateCanonBufferJob.ScheduleParallel(swarmHandle);

        swarmCenters.Dispose(canonUpdateHandle);
        state.Dependency = canonUpdateHandle;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }

    [BurstCompile]
    partial struct FighterSwarmJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        public NativeQueue<float3>.ParallelWriter queueWriter;
        void Execute(in DynamicBuffer<NearbyFighter> buffer, in LocalTransform localTransform, ref Fighter fighter, in Entity entity)
        {
            float size = buffer.Length;
            
            if (size == 0) return;
            
            float3 averageDir = localTransform.Forward();
            float3 averagePosition = float3.zero;
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
                float distanceFactor = math.lerp(0, 1, math.clamp(math.unlerp(radiusSq, 0f, distance), 0, 1));
                
                if (distance > 0f)
                    averageCounterForceDir += math.normalize(direction) * distanceFactor;
            }

            fighter.CrowdCenter = averagePosition / math.max(size, 1f);

            queueWriter.Enqueue(fighter.CrowdCenter);

            fighter.AlignmentDirection = averageDir;
            fighter.NeighbourCounterForceDirection = averageCounterForceDir;
        }
    }

    [BurstCompile]
    partial struct UpdateCanonSwarmCentersJob : IJobEntity
    {
        [ReadOnly] public NativeArray<float3> SwarmCenters;
        void Execute(ref DynamicBuffer<SwarmCenterBuffer> buffer, ref Canon canon)
        {
            buffer.Clear();

            for (int i = 0; i < SwarmCenters.Length; i++)
            {
                buffer.Add(new SwarmCenterBuffer { Position = SwarmCenters[i] });
            }
        }
    }
}