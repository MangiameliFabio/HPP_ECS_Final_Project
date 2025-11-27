using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;

public partial struct FighterRayCastSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
    }
    
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        ref PhysicsWorld physicsWorld = ref physicsWorldSingleton.PhysicsWorld;
        var healthComponentLookup = SystemAPI.GetComponentLookup<HealthComponent>();

        var job = new NearbySearchJob()
        {
            CurrentPhysicsWorld = physicsWorld,
            HealthComponentLookup = healthComponentLookup
        };
        
        var handle = job.ScheduleParallel(state.Dependency);
        state.Dependency = handle;
    }

    [BurstCompile]
    partial struct NearbySearchJob : IJobEntity
    {
        [ReadOnly] public PhysicsWorld CurrentPhysicsWorld;
        [ReadOnly] public ComponentLookup<HealthComponent> HealthComponentLookup;
        public float ElapsedTime;

        void Execute(ref LocalTransform localTransform,
            ref Fighter fighter,
            in Entity entity)
        {
            if (fighter.LastShotTime + fighter.FireCooldown > ElapsedTime)
                return;

            float3 dir = math.normalize(localTransform.Forward());
            float3 start = localTransform.Position + dir * 10f;
            float maxDistance = 100f;
            float3 end = start + dir * maxDistance;

            var input = new RaycastInput
            {
                Start = start,
                End = end,
                Filter = CollisionFilter.Default
            };

            if (CurrentPhysicsWorld.CollisionWorld.CastRay(input, out Unity.Physics.RaycastHit hit))
            {
                Entity hitEntity = Entity.Null;
                if (hit.RigidBodyIndex >= 0 && hit.RigidBodyIndex < CurrentPhysicsWorld.NumBodies)
                {
                    hitEntity = CurrentPhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                }

                if (HealthComponentLookup.HasComponent(hitEntity))
                {
                    var hc = HealthComponentLookup[hitEntity];
                    hc.Health -= 1;
                    HealthComponentLookup[hitEntity] = hc;

                    fighter.LastShotTime = ElapsedTime;
                }
            }
        }
    }
}
