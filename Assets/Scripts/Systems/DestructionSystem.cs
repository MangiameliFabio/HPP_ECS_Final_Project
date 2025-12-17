using Unity.Collections;
using Unity.Entities;

[UpdateAfter(typeof(TimeDestructionSystem))]
[UpdateAfter(typeof(SimpleExplosionSystem))]
[UpdateAfter(typeof(DamageSystemOdd))]

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
        
        // right now this will never really run, as the hitbuffer was already cleared in the DamageSystem, which updates before this one
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
                // only look at linked entities if it is a star destroyer
                if (state.EntityManager.HasComponent<StarDestroyer>(entity))
                {
                    var linked = state.EntityManager.GetBuffer<LinkedEntityGroup>(entity);
                    var entities = linked.Reinterpret<Entity>().AsNativeArray();
                    for (int i = 0; i < entities.Length; i++)
                    {
                        var linkedEntity = entities[i];
                        if (state.EntityManager.HasComponent<FighterComponent>(linkedEntity))
                        {
                            KillCount.FightersKilled += 1;
                        }
                    }

                    KillCount.StarDestroyerKilled += 1;

                    ecb.DestroyEntity(entities);
                    entities.Dispose();
                }
                else
                {
                    if (state.EntityManager.HasComponent<FighterComponent>(entity))
                    {
                        KillCount.FightersKilled += 1;
                    }
                    ecb.DestroyEntity(entity);
                }
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    public void OnDestroy(ref SystemState state)
    {
    }
}
