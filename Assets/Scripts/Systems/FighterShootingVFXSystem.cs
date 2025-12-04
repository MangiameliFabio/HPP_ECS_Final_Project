using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(FighterRayCastSystem))]

public partial struct FighterShootingVFXSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (fighter, transform) in
                 SystemAPI.Query<RefRO<Fighter>, RefRO<LocalToWorld>>())
        {
            if (!fighter.ValueRO.IsShooting)
                continue;

            var beamVFX = ecb.Instantiate(config.BeamVFX);

            // Set/override LocalTransform for instantiated entities
            // (Assuming prefab already has LocalTransform, use SetComponent; otherwise use AddComponent)
            ecb.SetComponent(beamVFX, new LocalTransform
            {
                Position = transform.ValueRO.Position,
                Rotation = transform.ValueRO.Rotation,
                Scale = 1f
            });
            // Add the VFX tag/component if prefab does not include it
            ecb.AddComponent(beamVFX, new BeamComponent());

            // Add components that are likely not part of prefab archetype
            ecb.AddComponent(beamVFX, new TimedDestructionComponent
            {
                lifeTime = 0.5f,
                elapsedTime = 0f
            });
            ecb.AddComponent(beamVFX, new HealthComponent
            {
                Health = 1f
            });
        }
    }

    public void OnDestroy(ref SystemState state)
    {
    }
}
