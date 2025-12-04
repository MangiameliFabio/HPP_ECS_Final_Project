using System.Linq;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public partial struct AsteroidSpawnSystem : ISystem
{
    Random random;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        
        random = new Random(1337);
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();
        
        state.Enabled = false;

        for (int i = 0; i < config.AsteroidCount; i++)
        {
            var asteroidEntity = state.EntityManager.Instantiate(config.AsteroidPrefab);

            var randomTransform =
                TransformUtils.CreateRandomTransform(config.MinSpawningBounds, config.MaxSpawningBounds, UnityEngine.Random.rotation);
            if (state.EntityManager.HasComponent<Asteroid>(asteroidEntity))
            {
                var asteroid = state.EntityManager.GetComponentData<Asteroid>(asteroidEntity);

                asteroid.AngularVelocity = RandomFloat3(-0.5f, 0.5f);
                asteroid.LinearVelocity = RandomFloat3(-1f, 1f);
                asteroid.Scale = RandomFloat(0.15f, 0.5f);
                    
                randomTransform.Scale = asteroid.Scale;
                
                state.EntityManager.SetComponentData(asteroidEntity, asteroid);  
            }
            
            if (state.EntityManager.HasComponent<LocalTransform>(asteroidEntity))
            {
                state.EntityManager.SetComponentData(asteroidEntity, randomTransform);    
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
    
    private float3 RandomFloat3(float min, float max)
    {
        return new float3(
            random.NextFloat(min, max),
            random.NextFloat(min, max),
            random.NextFloat(min, max)
        );
    }

    private float RandomFloat(float min, float max)
    {
        return random.NextFloat(min, max);
    }
}