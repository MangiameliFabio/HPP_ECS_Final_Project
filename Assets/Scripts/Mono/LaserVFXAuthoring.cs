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
            AddComponent(entity, new LaserVFX());
            AddComponent(entity, new TimedDestructionComponent
            {
                lifeTime = 1f,
                elapsedTime = 0f
            });
        }
    }
}

public struct LaserVFX : IComponentData
{
}
