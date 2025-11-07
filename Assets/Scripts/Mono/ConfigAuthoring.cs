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

    [Header("VFX Prefabs")]
    public GameObject cruiserBlastPrefab;

    [Header("Settings")]
    public int fighterCount;

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
                CruiserLaserPrefab = GetEntity(authoring.starDestroyerLaserPrefab, TransformUsageFlags.Dynamic),
                CruiserLaserBlastVFX = GetEntity(authoring.cruiserBlastPrefab, TransformUsageFlags.Dynamic),
                FighterCount = authoring.fighterCount,
                MaxSpawningBounds = authoring.spawningBounds.MaxBounds,
                MinSpawningBounds = authoring.spawningBounds.MinBounds,
            });
        }
    }
}

public struct Config : IComponentData
{
    public Entity FighterPrefab;
    public Entity CruiserLaserPrefab;
    public Entity CruiserLaserBlastVFX;
    public int FighterCount;
    public float3 MaxSpawningBounds;
    public float3 MinSpawningBounds;
}

[System.Flags]
public enum PhysicsTags
{
    Avoid = 1 << 0,
    Fighter = 1 << 1,
    Asteroid = 1 << 2,
    Target = 1 << 3,
}