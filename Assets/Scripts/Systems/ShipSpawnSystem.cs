using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct ShipSpawnSystem : ISystem
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
        
        state.Enabled = false;

        for (int i = 0; i < config.FighterCount; i++)
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