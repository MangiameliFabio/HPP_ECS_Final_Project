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
    public GameObject asteroidPrefab;

    [Header("VFX Prefabs")]
    public GameObject cruiserBlastPrefab;
    public GameObject cruiserExplosionPrefab;
    public GameObject fighterExplosionPrefab;
    public GameObject beamVFXPrefab;

    [Header("Settings")]
    public int fighterCount;
    public int starDestroyerCount;
    public int asteroidCount;

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
                AsteroidPrefab = GetEntity(authoring.asteroidPrefab, TransformUsageFlags.Dynamic),
                StarDestroyerLaserPrefab = GetEntity(authoring.starDestroyerLaserPrefab, TransformUsageFlags.Dynamic),
                StarDestroyerBlastVFX = GetEntity(authoring.cruiserBlastPrefab, TransformUsageFlags.Dynamic),
                CruiserExplosionVFX = GetEntity(authoring.cruiserExplosionPrefab, TransformUsageFlags.Dynamic),
                FighterExplosionVFX = GetEntity(authoring.fighterExplosionPrefab, TransformUsageFlags.Dynamic),
                BeamVFX = GetEntity(authoring.beamVFXPrefab, TransformUsageFlags.Dynamic),
                FighterCount = authoring.fighterCount,
                StarDestroyerCount = authoring.starDestroyerCount,
                AsteroidCount = authoring.asteroidCount,
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
    public Entity AsteroidPrefab;
    public Entity CruiserExplosionVFX;
    public Entity FighterExplosionVFX;
    public Entity BeamVFX;
    public int FighterCount;
    public int StarDestroyerCount;
    public int AsteroidCount;
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