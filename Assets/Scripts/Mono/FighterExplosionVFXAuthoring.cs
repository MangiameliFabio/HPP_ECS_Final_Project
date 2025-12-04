using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class FighterExplosionVFXAuthoring : MonoBehaviour
{
    class Baker : Baker<FighterExplosionVFXAuthoring>
    {
        public override void Bake(FighterExplosionVFXAuthoring authoring)
        {
            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new FighterExplosionVFX());
            AddComponent(entity, new TimedDestructionComponent
            {
                lifeTime = 2f,
                elapsedTime = 0f
            });
        }
    }
}

public struct FighterExplosionVFX : IComponentData
{
}
