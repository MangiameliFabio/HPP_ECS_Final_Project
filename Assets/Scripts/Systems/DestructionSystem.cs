using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

[UpdateAfter(typeof(FighterMovementSystem))]
[UpdateAfter(typeof(FighterAvoidanceSystem))]
[UpdateAfter(typeof(FighterSwarmSystem))]
[UpdateAfter(typeof(LaserMoveSystem))]
[UpdateAfter(typeof(StarDestroyerMovementSystem))]
public partial struct DestructionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        
        JobHandle combinedDeps = state.Dependency;
        
        var job = new DestroyJob
        {
            CommandBuffer = ecb.AsParallelWriter()
        };

        state.Dependency = job.ScheduleParallel(combinedDeps);
    }
    
    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
    
    [BurstCompile]
    partial struct DestroyJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        void Execute(Entity entity, in HealthComponent health)
        {
            if (health.Health <= 0)
            {
                CommandBuffer.DestroyEntity(entity.Index, entity);
            }
        }
    }
}

