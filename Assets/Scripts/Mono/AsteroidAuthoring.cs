using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class AsteroidAuthering : MonoBehaviour
{
    class Baker : Baker<AsteroidAuthering>
    {
        public override void Bake(AsteroidAuthering authoring)
        {
            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new Asteroid());
            AddComponent(entity, new PhysicsCustomTags()
            {
                //Set physics custom tag to "Avoid" and "Asteroid"
                Value = (int)(PhysicsTags.Avoid | PhysicsTags.Asteroid)
            });
            var sphere = GetComponent<UnityEngine.SphereCollider>();
            var localTransform = GetComponent<Transform>();
            AddComponent(entity, new AvoidanceSphere()
            {
                Radius = sphere.radius * localTransform.localScale.x,
            });
        }
    }
}

public struct Asteroid : IComponentData
{
}

public struct AvoidanceSphere : IComponentData
{
    public float Radius;
}