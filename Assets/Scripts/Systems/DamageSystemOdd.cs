using Unity.Collections;
using Unity.Entities;

[UpdateAfter(typeof(FighterMovementSystem))]
[UpdateAfter(typeof(FighterAvoidanceSystem))]
[UpdateAfter(typeof(FighterSwarmSystem))]
[UpdateAfter(typeof(LaserMoveSystem))]
[UpdateAfter(typeof(StarDestroyerMovementSystem))]
[UpdateAfter(typeof(StarDestroyerExplosionSystem))]
[UpdateAfter(typeof(LaserCollisionSystem))]

public partial struct DamageSystemOdd : ISystem
{

    public void OnCreate(ref SystemState state)
    {
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
    }

    public void OnDestroy(ref SystemState state)
    {
    }
}
