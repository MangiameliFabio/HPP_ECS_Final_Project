using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class HyperspeedJumpVFXAuthoring : MonoBehaviour
{
    class Baker : Baker<HyperspeedJumpVFXAuthoring>
    {
        public override void Bake(HyperspeedJumpVFXAuthoring authoring)
        {
            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new HyperspeedJumpVFX());
            AddComponent(entity, new TimedDestructionComponent
            {
                lifeTime = 0.3f,
                elapsedTime = 0f
            });
        }
    }
}

public struct HyperspeedJumpVFX : IComponentData
{
}
