using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FighterAuthoring : MonoBehaviour
{
    public float fighterSpeed;
    public float fighterRotationSpeed;
    public float fighterNeighbourDetectionRadius;
    
    class Baker : Baker<FighterAuthoring>
    {
        public override void Bake(FighterAuthoring authoring)
        {
            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new Fighter
            {
                Speed = authoring.fighterSpeed,
                RotationSpeed = authoring.fighterRotationSpeed,
                NeighbourDetectionRadius = authoring.fighterNeighbourDetectionRadius
            });
            AddComponent(entity, new AvoidingTag());
            
            AddBuffer<NearbyEntity>(entity);
            AddBuffer<AvoidingEntity>(entity);
        }
    }
}

public struct Fighter : IComponentData
{
    public float Speed;
    public float RotationSpeed;
    public float NeighbourDetectionRadius;
    
    public float3 alignmentDirection;
    public float3 crowdCenter;
    public float3 avoidanceDirection;
}

public struct NearbyEntity : IBufferElementData
{
    public Entity entity;
}

public struct AvoidingEntity : IBufferElementData
{
    public Entity entity;
}