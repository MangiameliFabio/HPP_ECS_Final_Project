using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class CanonAuthoring : MonoBehaviour
{
    public float CoolDownTime;
    public float CurrentCoolDown;

    class Baker : Baker<CanonAuthoring>
    {
        public override void Bake(CanonAuthoring authoring)
        {
            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new Canon
            { 
                CoolDownTime = authoring.CoolDownTime,
                CurrentCoolDown = authoring.CurrentCoolDown,
                RotationSpeed = 3f
            });
        }
    }
}

// todo: add shooting radius
public struct Canon : IComponentData
{
    public float CoolDownTime;
    public float CurrentCoolDown;
    public float RotationSpeed;
    public bool IsAimingAtTarget;
    public float3 Target;
}
