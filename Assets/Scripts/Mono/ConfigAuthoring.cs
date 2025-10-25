using Unity.Entities;
using UnityEngine;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject fighterPrefab;
    public int fighterCount;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.None);
            AddComponent(entity, new Config
            {
                FighterPrefab = GetEntity(authoring.fighterPrefab, TransformUsageFlags.Dynamic),
                FighterCount = authoring.fighterCount,
            });
        }
    }
}
public struct Config : IComponentData
{
    public Entity FighterPrefab;
    public int FighterCount;
}