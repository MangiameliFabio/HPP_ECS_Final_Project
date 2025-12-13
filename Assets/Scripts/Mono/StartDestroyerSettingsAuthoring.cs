using Unity.Entities;
using UnityEngine;

public class StarDestroyerSettingsAuthoring : MonoBehaviour
{
    public float speed = 1f;
    public float movementRadius = 200f;
    public float health = 1000f;
    class Baker : Baker<StarDestroyerSettingsAuthoring>
    {
        public override void Bake(StarDestroyerSettingsAuthoring authoring)
        {
            // Get the entity being created
            var entity = GetEntity(TransformUsageFlags.None);
            
            AddComponent(entity, new StarDestroyerSettings
            {
                Speed = authoring.speed / 100f,
                MovementRadius = authoring.movementRadius,
                Health = authoring.health,
            });
        }
    }
}

public struct StarDestroyerSettings : IComponentData
{
    public float Speed;
    public float MovementRadius;
    public float Health;
}