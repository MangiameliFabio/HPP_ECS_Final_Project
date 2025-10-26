using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class FighterAuthoring : MonoBehaviour
{
    public float minFighterSpeed = 10f;
    public float maxFighterSpeed = 20f;
    public float minFighterRotationSpeed = 5f;
    public float maxFighterRotationSpeed = 20f;
    public float fighterNeighbourDetectionRadius = 10f;
    public float alignmentFactor = 1f;
    public float crowdingFactor = 0.5f;
    public float minNeighbourCounterFactor = 0f;
    public float maxNeighbourCounterFactor = 5f;
    public float minAvoidanceFactor = 1f;
    public float maxAvoidanceFactor = 10f;
    
    class Baker : Baker<FighterAuthoring>
    {
        public override void Bake(FighterAuthoring authoring)
        {
            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new Fighter
            {
                MinSpeed = authoring.minFighterSpeed,
                MaxSpeed = authoring.maxFighterSpeed,
                MinRotationSpeed = authoring.minFighterRotationSpeed,
                MaxRotationSpeed = authoring.maxFighterRotationSpeed,
                NeighbourDetectionRadius = authoring.fighterNeighbourDetectionRadius,
                AlignmentFactor = authoring.alignmentFactor,
                CrowdingFactor = authoring.crowdingFactor,
                MinNeighbourCounterFactor = authoring.minNeighbourCounterFactor,
                MaxNeighbourCounterFactor = authoring.maxNeighbourCounterFactor,
                MinAvoidanceFactor = authoring.minAvoidanceFactor,
                MaxAvoidanceFactor = authoring.maxAvoidanceFactor,
            });
            AddComponent(entity, new PhysicsCustomTags()
            {
                //Set physics custom tag to "Fighter"
                Value = (1 << 1),
            });
            
            AddBuffer<NearbyFighter>(entity);
            AddBuffer<AvoidingEntity>(entity);
        }
    }
}

public struct Fighter : IComponentData
{
    public float MinSpeed;
    public float MaxSpeed;
    public float MinRotationSpeed;
    public float MaxRotationSpeed;
    public float NeighbourDetectionRadius;
    public float AlignmentFactor;
    public float CrowdingFactor;
    public float MinNeighbourCounterFactor;
    public float MaxNeighbourCounterFactor;
    public float MinAvoidanceFactor;
    public float MaxAvoidanceFactor;
    
    public float3 AlignmentDirection;
    public float3 CrowdCenter;
    public float3 AvoidanceDirection;
}

public struct NearbyFighter : IBufferElementData
{
    public Entity entity;
}