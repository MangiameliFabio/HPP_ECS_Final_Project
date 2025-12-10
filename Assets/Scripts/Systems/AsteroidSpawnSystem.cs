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

            randomTransform.Scale = RandomFloat(0.15f, 0.5f);
            state.EntityManager.SetComponentData(asteroidEntity, randomTransform);

            var sphereGeometry = new SphereGeometry
            {
                Center = float3.zero,
                Radius = 45
            };
            var material = new Material
            {
                Friction = 0f,
                Restitution = 0.5f
            };
            var asteroidFilter = new CollisionFilter
            {
                BelongsTo =  16 << 0,
                CollidesWith =  16 << 0,
                GroupIndex = 0
            };
            var sphere = SphereCollider.Create(
                sphereGeometry,
                asteroidFilter,
                material
            );

            state.EntityManager.SetComponentData(asteroidEntity, new PhysicsCollider
            {
                Value = sphere
            });
            
            var asteroidComponentData = state.EntityManager.GetComponentData<Asteroid>(asteroidEntity);
            asteroidComponentData.SphereRadius = sphereGeometry.Radius;
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