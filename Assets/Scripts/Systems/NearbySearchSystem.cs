// NearbyPhysicsSearchSystem.cs
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
        var physicsSingleton = SystemAPI.GetSingleton<Unity.Physics.PhysicsWorldSingleton>();
        ref var physicsWorld = ref physicsSingleton.PhysicsWorld;
        
        NativeList<Unity.Physics.DistanceHit> hits = new NativeList<Unity.Physics.DistanceHit>(Allocator.Temp);
        
        var entityManager = state.EntityManager;
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (t, fighter, searcherEntity) in
                 SystemAPI.Query<RefRO<LocalTransform>, RefRO<Fighter>>()
                          .WithAll<Fighter>()
                          .WithEntityAccess())
        {
            var swarmBuffer = entityManager.GetBuffer<NearbyEntity>(searcherEntity);
            swarmBuffer.Clear();
            
            var avoidanceBuffer = entityManager.GetBuffer<AvoidingEntity>(searcherEntity);
            avoidanceBuffer.Clear();

            float3 center = t.ValueRO.Position;
            float radiusVal = fighter.ValueRO.NeighbourDetectionRadius;
            
            var pInput = new Unity.Physics.PointDistanceInput
            {
                Position = center,
                MaxDistance = radiusVal,
                Filter = CollisionFilter.Default
            };

            hits.Clear();


            bool gotAny = physicsWorld.CollisionWorld.CalculateDistance(pInput, ref hits);

            if (!gotAny)
            {
                continue;
            }
            
            for (int i = 0; i < hits.Length; ++i)
            {
                var hit = hits[i];

                int rbIndex = hit.RigidBodyIndex;
                if (rbIndex < 0 || rbIndex >= physicsWorld.NumBodies)
                    continue;
                
                var body = physicsWorld.Bodies[rbIndex];
                Entity hitEntity = body.Entity;
                
                if (hitEntity == Entity.Null || hitEntity == searcherEntity)
                    continue;

                if (entityManager.HasComponent<AvoidingEntity>(hitEntity))
                {
                    avoidanceBuffer.Add(new AvoidingEntity { entity = hitEntity });
                }
                
                if (entityManager.HasComponent<Fighter>(hitEntity))
                {
                    swarmBuffer.Add(new NearbyEntity { entity = hitEntity });
                }
            }
        }

        hits.Dispose();
    }
}

public struct AvoidingTag : IComponentData
{
}

