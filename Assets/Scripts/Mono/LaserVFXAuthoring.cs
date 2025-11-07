using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class LaserVFXAuthoring : MonoBehaviour
{
    class Baker : Baker<LaserVFXAuthoring>
    {
        public override void Bake(LaserVFXAuthoring authoring)
        {
            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new LaserVFX
            {
            });
            AddBuffer<SwarmCenterBuffer>(entity);

        }
    }
}

public struct LaserVFX : IComponentData
{
}
