using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using static UnityEngine.Rendering.STP;
using static UnityEngine.EventSystems.EventTrigger;

public partial struct StarDestroyerExplosionSystem : ISystem
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

        var positionOffsets = new NativeArray<float3>(3, Allocator.Temp)
        {
            [0] = new float3(35f, 0f, 0f),
            [1] = new float3(-20f, 0f, 15f),
            [2] = new float3(-15f, 10f, -10f)
        };

        foreach (var (health, localToWorld, starDestroyer, transform) in SystemAPI.Query<RefRO<HealthComponent>, RefRO<LocalTransform>, RefRO<StarDestroyer>, RefRW<LocalTransform>>())
        {
            var currentHealth = health.ValueRO.Health;
            var totalHealth = starDestroyer.ValueRO.Health;
            
            var currentPosition = localToWorld.ValueRO.Position;


            if (currentHealth <= 0f)
            {
                continue;
            }

            if (currentHealth <= totalHealth * 0.3)
            {
                // add a bit of rotation to the ship for visual effect
                transform.ValueRW.Rotation = math.mul(transform.ValueRW.Rotation, quaternion.EulerXYZ(new float3(0f, 0f, math.sin((float)SystemAPI.Time.ElapsedTime * 5f) * 0.01f)));
            }

            // launch three if <= 10% health is reached
            if (currentHealth <= totalHealth * 0.1f && currentHealth >= totalHealth * 0.05)
            {
                TriggerExplosion(ecb, config, ref state, currentPosition + positionOffsets[0]);
                TriggerExplosion(ecb, config, ref state, currentPosition + positionOffsets[1]);
                TriggerExplosion(ecb, config, ref state, currentPosition + positionOffsets[2]);
                continue;
            }

            // launch one explosion when 30% health is reached
            if (currentHealth <= totalHealth * 0.2f && currentHealth >= totalHealth * 0.15)
            {
                TriggerExplosion(ecb, config, ref state, currentPosition);
            }
        }

        positionOffsets.Dispose();

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void TriggerExplosion(EntityCommandBuffer ecb, Config config, ref SystemState state, float3 position)
    {
        Entity vfxEntity = ecb.Instantiate(config.CruiserExplosionVFX);

        ecb.SetComponent(vfxEntity, LocalTransform.FromPositionRotationScale(
            position,
            quaternion.identity,
            1f // <-- your prefab’s scale can be overridden if needed
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
