using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class StarDestroyerLaserAuthoring : MonoBehaviour
{
    class Baker : Baker<StarDestroyerLaserAuthoring>
    {
        public override void Bake(StarDestroyerLaserAuthoring authoring)
        {
            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new LaserSD
            {
            });
            AddComponent(entity, new PhysicsCustomTags()
            {
                //Set physics custom tag to "Target"
                Value = (int)PhysicsTags.Avoid
            });

            var sphere = GetComponent<UnityEngine.SphereCollider>();
            var localTransform = GetComponent<Transform>();
            AddComponent(entity, new AvoidanceSphere
            {
                Radius = sphere.radius * localTransform.localScale.x
            });
        }
    }
}

public struct LaserSD : IComponentData
{
    public float3 Direction;
    public float Speed;
}
