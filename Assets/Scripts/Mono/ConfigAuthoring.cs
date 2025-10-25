using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject fighterPrefab;
    public int fighterCount;
    public float3 maxSpawningBounds;
    public float3 minSpawningBounds;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new Config
            {
                FighterPrefab = GetEntity(authoring.fighterPrefab, TransformUsageFlags.Dynamic),
                FighterCount = authoring.fighterCount,
                MaxSpawningBounds =  authoring.maxSpawningBounds,
                MinSpawningBounds = authoring.minSpawningBounds,
            });
        }
    }
}
public struct Config : IComponentData
{
    public Entity FighterPrefab;
    public int FighterCount;
    public float3 MaxSpawningBounds;
    public float3 MinSpawningBounds;
}