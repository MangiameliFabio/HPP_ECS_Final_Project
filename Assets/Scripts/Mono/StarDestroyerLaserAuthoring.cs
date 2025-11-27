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
            //AddComponent(entity, new HealthComponent
            //{
            //    Health = 1
            //});
            AddComponent(entity, new TimedDestructionComponent
            {
                lifeTime = 3f,
                elapsedTime = 0f
            });
        }
    }
}

public struct LaserSD : IComponentData
{
    public float3 Direction;
    public float Speed;
}
