using System.Linq;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public partial struct ShipSpawnSystem : ISystem
{
    private EntityQuery _starDestroyerQuery;
    private EntityQuery _fighterQuery;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();

        _starDestroyerQuery = state.GetEntityQuery(ComponentType.ReadOnly<StarDestroyer>());
        _fighterQuery = state.GetEntityQuery(ComponentType.ReadOnly<Fighter>());
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();

        int currentDestroyerCount = _starDestroyerQuery.CalculateEntityCount();
        var deltaShipCount = config.StarDestroyerCount - currentDestroyerCount;
        
        for (int i = 0; i < deltaShipCount; i++)
        {
            var destroyerEntity = state.EntityManager.Instantiate(config.StarDestroyerPrefab);
        
            var randomTransform =
                TransformUtils.CreateRandomTransform(config.MinSpawningBounds * 5, config.MaxSpawningBounds * 5, default);

            float3 newPoint0 = randomTransform.Position;
            newPoint0.y = 0;

            float3 newPoint1 = TransformUtils.CreateRandomTransform(config.MinSpawningBounds, config.MaxSpawningBounds, UnityEngine.Random.rotation).Position;
            newPoint1.y = 0;

            float3 newPoint2 = TransformUtils.CreateRandomTransform(config.MinSpawningBounds, config.MaxSpawningBounds, UnityEngine.Random.rotation).Position;
            newPoint2.y = 0;

            var startDirection = math.normalize(newPoint1 - newPoint0);

            // position the cruiser off-screen so that it can hyperjump in
            // direction of second point - first point
            randomTransform.Position += -startDirection * 5000f;

            randomTransform.Rotation = quaternion.LookRotationSafe(startDirection, math.up());

            if (state.EntityManager.HasComponent<LocalTransform>(destroyerEntity))
            {
                state.EntityManager.SetComponentData(destroyerEntity, randomTransform);
            }

            if (state.EntityManager.HasComponent<StarDestroyer>(destroyerEntity))
            {
                var starDestroyer = state.EntityManager.GetComponentData<StarDestroyer>(destroyerEntity);

                starDestroyer.ID = UnityEngine.Random.Range(1, int.MaxValue);

                starDestroyer.Point1 = newPoint0;
                starDestroyer.Point2 = newPoint1;
                starDestroyer.Point3 = newPoint2;

                starDestroyer.HasJumpedInScene = false;

                state.EntityManager.SetComponentData(destroyerEntity, starDestroyer);
            }
        }

        int currentFighterCount = _fighterQuery.CalculateEntityCount();
        deltaShipCount = config.FighterCount - currentFighterCount;

        for (int i = 0; i < deltaShipCount; i++)
        {
            var fighterEntity = state.EntityManager.Instantiate(config.FighterPrefab);

            var randomTransform =
                TransformUtils.CreateRandomTransform(config.MinSpawningBounds, config.MaxSpawningBounds, UnityEngine.Random.rotation);
            
            if (state.EntityManager.HasComponent<LocalTransform>(fighterEntity))
            {
                state.EntityManager.SetComponentData(fighterEntity, randomTransform);    
            }
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}

public static class TransformUtils
{
    public static LocalTransform CreateRandomTransform(float3 min, float3 max, quaternion rotation = default, float scale = 1f)
    {
        float3 pos = new float3(
            UnityEngine.Random.Range(min.x, max.x),
            UnityEngine.Random.Range(min.y, max.y),
            UnityEngine.Random.Range(min.z, max.z)
        );
        
        if (rotation.Equals(default(quaternion)))
            rotation = quaternion.identity;

        return LocalTransform.FromPositionRotationScale(pos, rotation, scale);
    }
}