using System.Linq;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Material = Unity.Physics.Material;
using Random = Unity.Mathematics.Random;
using SphereCollider = Unity.Physics.SphereCollider;
using Collider = Unity.Physics.Collider;

public partial struct AsteroidSpawnSystem : ISystem
{
    Random random;
    BlobAssetReference<Collider> asteroidCollider;
    float radius;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        
        random = new Random(1337);


        radius = 45f;
        var sphereGeometry = new SphereGeometry { Center = float3.zero, Radius = radius };
        var material = new Material { Friction = 0f, Restitution = 0.5f };
        var filter = new CollisionFilter { BelongsTo = 16 << 0, CollidesWith = 16 << 0 };
    
        asteroidCollider = SphereCollider.Create(sphereGeometry, filter, material);
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

            randomTransform.Scale = RandomFloat(0.15f, 0.5f);
            state.EntityManager.SetComponentData(asteroidEntity, randomTransform);

            state.EntityManager.SetComponentData(asteroidEntity, new PhysicsCollider
            {
                Value = asteroidCollider,
            });

            var asteroidComponentData = state.EntityManager.GetComponentData<Asteroid>(asteroidEntity);
            asteroidComponentData.SphereRadius = radius;
            state.EntityManager.SetComponentData(asteroidEntity, asteroidComponentData);
            
            if (state.EntityManager.HasComponent<PhysicsVelocity>(asteroidEntity))
            {
                float3 linear = RandomFloat3(-1f, 1f);
                float3 angular = RandomFloat3(-0.5f, 0.5f);

                state.EntityManager.SetComponentData(asteroidEntity, new PhysicsVelocity
                {
                    Linear = linear,
                    Angular = angular
                });

                if (state.EntityManager.HasComponent<PhysicsMass>(asteroidEntity))
                {
                    var mass = state.EntityManager.GetComponentData<PhysicsMass>(asteroidEntity);
                    mass.InverseMass = 1000f / (1f + randomTransform.Scale);
                    state.EntityManager.SetComponentData(asteroidEntity, mass);
                }
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (asteroidCollider.IsCreated)
        {
            asteroidCollider.Dispose();
        }
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

