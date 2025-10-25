using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FighterAuthoring : MonoBehaviour
{
    
    class Baker : Baker<FighterAuthoring>
    {
        public override void Bake(FighterAuthoring authoring)
        {
            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new Fighter
            {
                searchRadius = 10f,
                alignmentDirection = float3.zero
            });
            
            AddBuffer<NearbyEntity>(entity);
        }
    }
}

public struct Fighter : IComponentData
{
    public float searchRadius;
    public float3 alignmentDirection;
}

// Buffer to store found entities
public struct NearbyEntity : IBufferElementData
{
    public Entity entity;
}