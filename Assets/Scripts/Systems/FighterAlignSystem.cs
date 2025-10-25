using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

public partial struct FighterAlignSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var entityManager = state.EntityManager;
        
        foreach (var (fighter, entity) in
                 SystemAPI.Query<RefRW<Fighter>>()
                     .WithAll<Fighter>()
                     .WithEntityAccess())
        {
            if (!entityManager.HasBuffer<NearbyEntity>(entity))
                return;
            
            var buffer = entityManager.GetBuffer<NearbyEntity>(entity);
            float size = buffer.Length;
            
            float3 averageDir = float3.zero;
            float3 averagePosition = float3.zero;

            foreach (var element in buffer)
            {
                if (!entityManager.HasComponent<LocalTransform>(element.entity))  
                    continue;
                
                var localTransform = entityManager.GetComponentData<LocalTransform>(element.entity);
                averagePosition += localTransform.Position;
                averageDir += localTransform.Forward();
            }

            fighter.ValueRW.crowdCenter = averagePosition / size;
            fighter.ValueRW.alignmentDirection = averageDir;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}