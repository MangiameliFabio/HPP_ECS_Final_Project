using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class ExplosionVFXAuthoring : MonoBehaviour
{
    class Baker : Baker<ExplosionVFXAuthoring>
    {
        public override void Bake(ExplosionVFXAuthoring authoring)
        {
            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new ExplosionVFX());
            AddComponent(entity, new TimedDestructionComponent
            {
                lifeTime = 3f,
                elapsedTime = 0f
            });
        }
    }
}

public struct ExplosionVFX : IComponentData
{
}
