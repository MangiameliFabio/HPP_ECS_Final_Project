using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[System.Serializable]
public struct SpawningBounds
{
    public float3 MaxBounds;
    public float3 MinBounds;
}

public class ConfigAuthoring : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject fighterPrefab;
    public GameObject starDestroyerLaserPrefab;
    public GameObject starDestroyerPrefab;

    [Header("VFX Prefabs")]
    public GameObject cruiserBlastPrefab;

    [Header("Settings")]
    public int fighterCount;
    public int starDestroyerCount;

    [Header("Spawning Bounds")]
    public SpawningBounds spawningBounds;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new Config
            {
                FighterPrefab = GetEntity(authoring.fighterPrefab, TransformUsageFlags.Dynamic),
                StarDestroyerPrefab = GetEntity(authoring.starDestroyerPrefab, TransformUsageFlags.Dynamic),
                StarDestroyerLaserPrefab = GetEntity(authoring.starDestroyerLaserPrefab, TransformUsageFlags.Dynamic),
                StarDestroyerBlastVFX = GetEntity(authoring.cruiserBlastPrefab, TransformUsageFlags.Dynamic),
                FighterCount = authoring.fighterCount,
                StarDestroyerCount = authoring.starDestroyerCount,
                MaxSpawningBounds = authoring.spawningBounds.MaxBounds,
                MinSpawningBounds = authoring.spawningBounds.MinBounds,
            });
        }
    }
}

public struct Config : IComponentData
{
    public Entity FighterPrefab;
    public Entity StarDestroyerLaserPrefab;
    public Entity StarDestroyerBlastVFX;
    public Entity StarDestroyerPrefab;
    public int FighterCount;
    public int StarDestroyerCount;
    public float3 MaxSpawningBounds;
    public float3 MinSpawningBounds;
}

[System.Flags]
public enum PhysicsTags
{
    Avoid = 1 << 0,
    Fighter = 1 << 1,
    Asteroid = 1 << 2,
    StarDestroyer = 1 << 3,
}

public struct HealthComponent : IComponentData
{
    public float Health;
}

public struct TimedDestructionComponent : IComponentData
{
    public float lifeTime;
    public float elapsedTime;
}