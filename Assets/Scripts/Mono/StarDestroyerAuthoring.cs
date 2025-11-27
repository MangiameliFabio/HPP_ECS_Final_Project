using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class StarDestroyerAuthoring : MonoBehaviour
{
    public float speed = 0.001f;
    public float movementProgress = 0;
    public float3 point1 = new float3(0, 0, 0);
    public float3 point2 = new float3(5, 0, 0);
    public float3 point3 = new float3(0, 0, 10);
    public float movementRadius = 300;
    public float ID;
    public float health = 100;

    class Baker : Baker<StarDestroyerAuthoring>
    {
        public override void Bake(StarDestroyerAuthoring authoring)
        {
            // GetEntity returns the Entity baked from the GameObject
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new StarDestroyer
            {
                Speed = authoring.speed,
                MovementProcess = authoring.movementProgress,
                Point1 = authoring.point1,
                Point2 = authoring.point2,
                Point3 = authoring.point3,
                MovementRadius = authoring.movementRadius,
                Health = (int)authoring.health,
            });
            AddComponent(entity, new PhysicsCustomTags()
            {
                //Set physics custom tag to "Avoid" and "StarDestroyer"
                Value = (int)PhysicsTags.Avoid | (int)PhysicsTags.StarDestroyer
            });
            AddComponent(entity, new TargetEntity());
            AddComponent(entity, new HealthComponent()
            {
                Health = authoring.health,
            });
        }
    }
}
public struct StarDestroyer : IComponentData
{
    public float Speed;
    public float MovementProcess;
    public float3 Point1; // array would be better, but ECS has limited support for arrays
    public float3 Point2; // buffer is overkill because it is always only 3 points
    public float3 Point3;
    public float MovementRadius;
    public int ID;
    public int Health;
}
