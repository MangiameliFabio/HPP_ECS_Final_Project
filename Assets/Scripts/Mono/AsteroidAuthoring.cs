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
                Value = (1 << 0) | (1 << 2),
            });
            AddComponent(entity, new AvoidanceSphere()
            {
                Radius = 15f
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