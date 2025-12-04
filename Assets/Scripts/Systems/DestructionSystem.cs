using Unity.Collections;
using Unity.Entities;

[UpdateAfter(typeof(FighterMovementSystem))]
[UpdateAfter(typeof(FighterAvoidanceSystem))]
[UpdateAfter(typeof(FighterSwarmSystem))]
[UpdateAfter(typeof(LaserMoveSystem))]
[UpdateAfter(typeof(StarDestroyerMovementSystem))]
[UpdateAfter(typeof(StarDestroyerExplosionSystem))]
[UpdateAfter(typeof(AsteroidMovementSystem))]
[UpdateAfter(typeof(SimpleExplosionSystem))]
[UpdateAfter(typeof(LaserCollisionSystem))]
[UpdateBefore(typeof(TimeDestructionSystem))]

public partial struct DestructionSystem : ISystem
{

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }


    public void OnUpdate(ref SystemState state)
    {
        var healthLookup = SystemAPI.GetComponentLookup<HealthComponent>();
        healthLookup.Update(ref state);
        
        foreach (var hitBuffer in 
                 SystemAPI.Query<DynamicBuffer<HitBufferElement>>())
        {
            foreach (var hitBufferElement in hitBuffer)
            {
                if (!healthLookup.HasComponent(hitBufferElement.TargetEntity))
                    continue;
                
                var health = healthLookup[hitBufferElement.TargetEntity];
                health.Health -= hitBufferElement.Damage;
                healthLookup[hitBufferElement.TargetEntity] = health;
            }
            hitBuffer.Clear();
        }
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (health, entity) in SystemAPI.Query<RefRO<HealthComponent>>().WithEntityAccess())
        {
            if (health.ValueRO.Health <= 0)
            {
                var linked = state.EntityManager.GetBuffer<LinkedEntityGroup>(entity);
                var entities = linked.Reinterpret<Entity>().AsNativeArray();
                
                ecb.DestroyEntity(entities);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    public void OnDestroy(ref SystemState state)
    {
    }
}
