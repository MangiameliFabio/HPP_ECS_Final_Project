using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using static UnityEngine.Rendering.STP;
using static UnityEngine.EventSystems.EventTrigger;

[UpdateAfter(typeof(LaserCollisionSystem))]
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

        var query = SystemAPI.QueryBuilder()
            .WithAll<HealthComponent, LocalToWorld>()
            .WithAny<Fighter, Asteroid>()
            .Build();

        foreach (var entity in query.ToEntityArray(Allocator.Temp))
        {
            var health = SystemAPI.GetComponentRO<HealthComponent>(entity);
            var localToWorld = SystemAPI.GetComponentRO<LocalToWorld>(entity);

            var currentHealth = health.ValueRO.Health;
            if (currentHealth > 0f)
            {
                continue;
            }

            bool isFighter = false;
            if (SystemAPI.HasComponent<Fighter>(entity))
                isFighter = true;

            var currentPosition = localToWorld.ValueRO.Position;
            
            TriggerExplosion(ecb, config, ref state, currentPosition, isFighter);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void TriggerExplosion(EntityCommandBuffer ecb, Config config, ref SystemState state, float3 position, bool isFighter)
    {
        Entity vfxEntity; 
        
        if (isFighter)
            vfxEntity = ecb.Instantiate(config.FighterExplosionVFX);
        else
            vfxEntity = ecb.Instantiate(config.CruiserExplosionVFX);

        ecb.SetComponent(vfxEntity, LocalTransform.FromPositionRotationScale(
            position,
            quaternion.identity,
            1f
        ));

        if (isFighter)
        {
            ecb.AddComponent(vfxEntity, new FighterExplosionVFX());
        }
        else
        {
            ecb.AddComponent(vfxEntity, new ExplosionVFX());
        }
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
