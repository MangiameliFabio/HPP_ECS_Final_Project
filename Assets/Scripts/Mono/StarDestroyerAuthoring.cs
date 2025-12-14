using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public class StarDestroyerAuthoring : MonoBehaviour
{
    public float movementProgress = 0;
    public float3 point1 = new float3(0, 0, 0);
    public float3 point2 = new float3(5, 0, 0);
    public float3 point3 = new float3(0, 0, 10);

    class Baker : Baker<StarDestroyerAuthoring>
    {
        public override void Bake(StarDestroyerAuthoring authoring)
        {
            var entity = GetEntity(authoring, TransformUsageFlags.Dynamic);
            AddComponent(entity, new StarDestroyer
            {
                MovementProcess = authoring.movementProgress,
                Point1 = authoring.point1,
                Point2 = authoring.point2,
                Point3 = authoring.point3,
                HasJumpedInScene = false,
            });
            AddComponent(entity, new PhysicsCustomTags()
            {
                Value = (int)PhysicsTags.Avoid | (int)PhysicsTags.StarDestroyer
            });
            AddComponent(entity, new TargetEntity());
            AddComponent(entity, new HealthComponent());
        }
    }
}
public struct StarDestroyer : IComponentData
{
    public float MovementProcess;
    public float3 Point1; // array would be better, but ECS has limited support for arrays
    public float3 Point2; // buffer is overkill because it is always only 3 points
    public float3 Point3;
    public int ID;
    public bool HasJumpedInScene;
}
