using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using static UnityEngine.Rendering.STP;
using static UnityEngine.EventSystems.EventTrigger;

public partial struct SimpleExplosionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (health, localToWorld, transform) 
                 in SystemAPI.Query<RefRO<HealthComponent>, RefRO<LocalToWorld>, RefRW<LocalTransform>>()
                     .WithAny<Fighter, Asteroid>())
        {
            var currentHealth = health.ValueRO.Health;
            if (currentHealth > 0f)
            {
                continue;
            }
            
            var currentPosition = localToWorld.ValueRO.Position;
            
            TriggerExplosion(ecb, config, ref state, currentPosition);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void TriggerExplosion(EntityCommandBuffer ecb, Config config, ref SystemState state, float3 position)
    {
        Entity vfxEntity = ecb.Instantiate(config.CruiserExplosionVFX);

        ecb.SetComponent(vfxEntity, LocalTransform.FromPositionRotationScale(
            position,
            quaternion.identity,
            1f
        ));

        ecb.AddComponent(vfxEntity, new ExplosionVFX());
        ecb.AddComponent(vfxEntity, new TimedDestructionComponent
        {
            lifeTime = 2.5f,
            elapsedTime = 0f
        });
        ecb.AddComponent(vfxEntity, new HealthComponent
        {
            Health = 1f
        });
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}
