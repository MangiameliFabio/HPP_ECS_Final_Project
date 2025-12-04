using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.Serialization;

public class FighterAuthoring : MonoBehaviour
{
    public float minFighterSpeed = 10f;
    public float maxFighterSpeed = 20f;
    public float minFighterRotationSpeed = 5f;
    public float maxFighterRotationSpeed = 20f;
    public float fighterNeighbourDetectionRadius = 10f;
    public float alignmentFactor = 1f;
    public float crowdingFactor = 0.5f;
    public float neighbourCounterFactor = 1f;
    public float avoidanceFactor = 100f;
    public float targetTrendFactor = 1f;
    public float targetMinDistance = 50f;
    public double fireCooldown = 5;
    public float fighterHealth = 1;
    
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
                NeighbourCounterForceFactor = authoring.neighbourCounterFactor,
                AvoidanceFactor = authoring.avoidanceFactor,
                TargetTrendFactor = authoring.targetTrendFactor,
                TargetMinDistance = authoring.targetMinDistance,
                CurrentTargetPosition = float3.zero,
                FireCooldown = authoring.fireCooldown
            });
            AddComponent(entity, new PhysicsCustomTags()
            {
                //Set physics custom tag to "Fighter"
                Value = (int)PhysicsTags.Fighter
            });
            AddComponent(entity, new HealthComponent()
            {
                Health = authoring.fighterHealth
            });
            AddBuffer<NearbyFighter>(entity);
            AddBuffer<AvoidingEntityBufferElement>(entity);
            AddBuffer<HitBufferElement>(entity);
        }
    }
}

public enum FighterState : byte
{
    Attack = 0,
    Retreat = 1
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
    public float NeighbourCounterForceFactor;
    public float TargetTrendFactor;
    public float AvoidanceFactor;
    public float TargetMinDistance;
    
    public float3 AlignmentDirection;
    public float3 CrowdCenter;
    public float3 AvoidanceDirection;
    public float3 NeighbourCounterForceDirection;
    public float3 TargetDirection;
    public float3 CurrentTargetPosition;
    public FighterState CurrentState;
    public Entity TargetEntity;
    public double FireCooldown;
    public double LastShotTime;

    public bool IsShooting;
    public float3 BeamStart;
    public float3 BeamEnd;
}

public struct NearbyFighter : IBufferElementData
{
    public Entity entity;
}

public struct HitBufferElement : IBufferElementData
{
    public Entity TargetEntity;
    public int Damage;
}