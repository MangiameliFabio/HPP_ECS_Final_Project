using UnityEngine;
using Unity.Entities;
using UnityEngine.VFX;
using Unity.Mathematics;

public class BeamVFXAuthoring : MonoBehaviour
{
    class Baker : Baker<BeamVFXAuthoring>
    {
        public override void Bake(BeamVFXAuthoring authoring)
        {

            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new BeamComponent());
            AddComponent(entity, new TimedDestructionComponent
            {
                lifeTime = 1f,
                elapsedTime = 0f
            });
        }
    }
}

public struct BeamComponent : IComponentData
{
    public float3 Start;
    public float3 End;
}