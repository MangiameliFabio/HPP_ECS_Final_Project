using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FighterSettingsAuthoring : MonoBehaviour
{
    public float minFighterSpeed = 10f;
    public float maxFighterSpeed = 20f;
    public float minFighterRotationSpeed = 0.5f;
    public float maxFighterRotationSpeed = 2f;
    public float fighterNeighbourDetectionRadius = 10f;
    public float alignmentFactor = 1f;
    public float crowdingFactor = 0.8f;
    public float neighbourCounterFactor = 1f;
    public float avoidanceFactor = 50f;
    public float targetTrendFactor = 1.5f;
    public float targetMinDistance = 50f;
    public double fireCooldown = 5f;
    class Baker : Baker<FighterSettingsAuthoring>
    {
        public override void Bake(FighterSettingsAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new FighterSettings
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
                FireCooldown = authoring.fireCooldown,
            });
        }
    }
}

public struct FighterSettings : IComponentData
{
    public float MinSpeed;
    public float MaxSpeed;
    public float MinRotationSpeed;
    public float MaxRotationSpeed;
    public float NeighbourDetectionRadius;
    public float AlignmentFactor;
    public float CrowdingFactor;
    public float NeighbourCounterForceFactor;
    public float AvoidanceFactor;
    public float TargetTrendFactor;
    public float TargetMinDistance;
    public double FireCooldown;
}