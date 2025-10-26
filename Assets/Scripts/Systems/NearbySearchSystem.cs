using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct NearbySearchSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        ref var physicsWorld = ref physicsSingleton.PhysicsWorld;

        var job = new NearbySearchJob()
        {
            CurrentPhysicsWorld = physicsWorld
        };
        
        var handle = job.ScheduleParallel(state.Dependency);
        state.Dependency = handle;
    }

    [BurstCompile]
    partial struct NearbySearchJob : IJobEntity
    {
        [ReadOnly] public PhysicsWorld CurrentPhysicsWorld;

        void Execute(ref LocalTransform localTransform,
                     ref Fighter fighter,
                     ref DynamicBuffer<NearbyFighter> swarmBuffer,
                     ref DynamicBuffer<AvoidingEntity> avoidanceBuffer,
                     in Entity entity)
        {
            swarmBuffer.Clear();
            avoidanceBuffer.Clear();

            float3 center = localTransform.Position;
            float radiusVal = fighter.NeighbourDetectionRadius;

            var pInput = new PointDistanceInput
            {
                Position = center,
                MaxDistance = radiusVal,
                Filter = CollisionFilter.Default
            };

            var hits = new NativeList<DistanceHit>(Allocator.Temp);
            bool gotAny = CurrentPhysicsWorld.CollisionWorld.CalculateDistance(pInput, ref hits);
            if (!gotAny)
            {
                hits.Dispose();
                return;
            }

            for (int i = 0; i < hits.Length; ++i)
            {
                var hit = hits[i];
                int rbIndex = hit.RigidBodyIndex;
                if (rbIndex < 0 || rbIndex >= CurrentPhysicsWorld.NumBodies)
                    continue;

                var body = CurrentPhysicsWorld.Bodies[rbIndex];
                Entity hitEntity = body.Entity;

                if (hitEntity == Entity.Null || hitEntity == entity)
                    continue;

                // Check if custom tag "Avoid" is set (bit 0)
                if ((body.CustomTags & 1u) != 0)
                {
                    avoidanceBuffer.Add(new AvoidingEntity { entity = hitEntity });
                }

                // Check if custom tag "Fighter" is set (bit 1)
                if ((body.CustomTags & (1u << 1)) != 0)
                {
                    swarmBuffer.Add(new NearbyFighter { entity = hitEntity });
                }
            }

            hits.Dispose();
        }
    }
}
