using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using SphereCollider = UnityEngine.SphereCollider;

public class AsteroidAuthering : MonoBehaviour
{
    class Baker : Baker<AsteroidAuthering>
    {
        public override void Bake(AsteroidAuthering authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new Asteroid());
            AddComponent(entity, new PhysicsCustomTags()
            {
                Value = (int)(PhysicsTags.Avoid | PhysicsTags.Asteroid)
            });
            AddComponent(entity, new HealthComponent()
            {
                Health = 1
            });
            AddBuffer<HitBufferElement>(entity);
        }
    }
}

public struct Asteroid : IComponentData
{
    public float SphereRadius;
}