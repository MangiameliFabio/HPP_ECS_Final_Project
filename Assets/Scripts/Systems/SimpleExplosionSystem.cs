using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;
using static UnityEngine.Rendering.STP;
using static UnityEngine.EventSystems.EventTrigger;
using UnityEngine.UIElements;
using System.Globalization;

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
        var ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged)
            .AsParallelWriter();

        var job = new SpawnExplosionJob
        {
            ECB = ecb,
            config = SystemAPI.GetSingleton<Config>(),
            FighterLookup = SystemAPI.GetComponentLookup<Fighter>(true)
        };

        state.Dependency = job.ScheduleParallel(state.Dependency);
    }



    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}

[BurstCompile]
public partial struct SpawnExplosionJob : IJobEntity
{

    public EntityCommandBuffer.ParallelWriter ECB;
    public Config config;

    [ReadOnly] public ComponentLookup<Fighter> FighterLookup;

    void Execute(
        [ChunkIndexInQuery] int sortKey,
        Entity entity,
        in LocalTransform transform,
        in HealthComponent healthComponent)
    {
        var currentHealth = healthComponent.Health;
        if (currentHealth > 0f)
        {
            return;
        }

        bool isFighter = FighterLookup.HasComponent(entity);

        Entity vfxEntity = ECB.Instantiate(
            sortKey,
            isFighter ? config.FighterExplosionVFX
                      : config.CruiserExplosionVFX
        );

        ECB.SetComponent(
            sortKey,
            vfxEntity,
            LocalTransform.FromPositionRotationScale(
                transform.Position,
                quaternion.identity,
                1f
            )
        );

        if (isFighter)
            ECB.AddComponent<FighterExplosionVFX>(sortKey, vfxEntity);
        else
            ECB.AddComponent<ExplosionVFX>(sortKey, vfxEntity);

        ECB.AddComponent(
            sortKey,
            vfxEntity,
            new TimedDestructionComponent
            {
                lifeTime = 2.5f,
                elapsedTime = 0f
            }
        );

        ECB.AddComponent(
            sortKey,
            vfxEntity,
            new HealthComponent { Health = 1f }
        );
    }
}
