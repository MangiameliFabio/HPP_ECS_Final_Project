using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
public partial struct LaserCollisionSystem : ISystem
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

        var job = new LaserCollisionJob()
        {
            CurrentPhysicsWorld = physicsWorld
        };
        
        var handle = job.ScheduleParallel(state.Dependency);
        state.Dependency = handle;
    }

    [BurstCompile]
    partial struct LaserCollisionJob : IJobEntity
    {
        [ReadOnly] public PhysicsWorld CurrentPhysicsWorld;

        void Execute(ref LocalTransform localTransform,
                            in LaserSD laser,
                            ref DynamicBuffer<HitBufferElement> hitBuffer,
                            in Entity entity)
        {
            hitBuffer.Clear();

            float3 center = localTransform.Position;
            float radiusVal = 20f;

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
                
                if ((body.CustomTags & (uint)PhysicsTags.Fighter) != 0)
                {
                    hitBuffer.Add(new HitBufferElement()
                    {
                        TargetEntity = hitEntity,
                        Damage = 1,
                    });
                }
            }

            hits.Dispose();
        }
    }
}
