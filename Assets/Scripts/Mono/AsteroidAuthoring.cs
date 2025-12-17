using Unity.Entities;
using Unity.Physics;
using UnityEngine;

public class AsteroidAuthering : MonoBehaviour
{
    class Baker : Baker<AsteroidAuthering>
    {
        public override void Bake(AsteroidAuthering authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            
            AddComponent(entity, new AsteroidComponent());
            AddComponent(entity, new PhysicsCustomTags()
            {
                Value = (int)(PhysicsTags.Avoid | PhysicsTags.Asteroid)
            });
            AddComponent(entity, new HealthComponent()
            {
                Health = 1000
            });
            AddBuffer<HitBufferElement>(entity);
        }
    }
}

public struct AsteroidComponent : IComponentData
{
    public float SphereRadius;
}