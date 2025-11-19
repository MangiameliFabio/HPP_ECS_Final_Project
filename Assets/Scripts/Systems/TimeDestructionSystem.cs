using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

[UpdateAfter(typeof(FighterMovementSystem))]
[UpdateAfter(typeof(FighterAvoidanceSystem))]
[UpdateAfter(typeof(FighterSwarmSystem))]
[UpdateAfter(typeof(LaserMoveSystem))]
[UpdateAfter(typeof(StarDestroyerMovementSystem))]

// zusammen mit DestructionSystem verwenden um entitys zu zerstören die eine bestimmte zeit gelebt haben?
public partial struct TimeDestructionSystem : ISystem
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
        
        var job = new UpdateElapsedTimeJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            CommandBuffer = ecb.AsParallelWriter()
        };

        state.Dependency = job.ScheduleParallel(combinedDeps);
    }
    
    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
    
    [BurstCompile]
    partial struct UpdateElapsedTimeJob : IJobEntity
    {
        public float deltaTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        void Execute(Entity entity, ref HealthComponent health, ref TimedDestructionComponent timedComponent)
        {
            
            if (timedComponent.elapsedTime > timedComponent.lifeTime)
            {
                CommandBuffer.DestroyEntity(entity.Index, entity);
            }
            else
            {
                timedComponent.elapsedTime += deltaTime;
            }
        }
    }
}

