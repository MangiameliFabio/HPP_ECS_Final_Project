using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

public class StarDestroyerAuthering : MonoBehaviour
{
    public float speed = 0.001f;
    public float movementProgress = 0;
    public float3 point1 = new float3(0, 0, 0);
    public float3 point2 = new float3(5, 0, 0);
    public float3 point3 = new float3(0, 0, 10);
    public float movementRadius = 300;
    public float ID;

    class Baker : Baker<StarDestroyerAuthering>
    {
        public override void Bake(StarDestroyerAuthering authoring)
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
                MovementRadius = authoring.movementRadius
            });
            AddComponent(entity, new PhysicsCustomTags()
            {
                //Set physics custom tag to "Avoid" and "Target"
                Value = (int)(PhysicsTags.Avoid)
            });
            var sphere = GetComponent<UnityEngine.SphereCollider>();
            var localTransform = GetComponent<Transform>();
            AddComponent(entity, new AvoidanceSphere()
            {
                Radius = sphere.radius * localTransform.localScale.x,
            });
            AddComponent(entity, new TargetEntity());
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
}
