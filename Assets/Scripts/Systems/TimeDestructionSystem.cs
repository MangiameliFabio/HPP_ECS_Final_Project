using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;

[UpdateAfter(typeof(FighterMovementSystem))]
[UpdateAfter(typeof(FighterAvoidanceSystem))]
[UpdateAfter(typeof(FighterSwarmSystem))]
[UpdateAfter(typeof(LaserMoveSystem))]
[UpdateAfter(typeof(StarDestroyerMovementSystem))]
[UpdateAfter(typeof(StarDestroyerExplosionSystem))]
[UpdateAfter(typeof(SimpleExplosionSystem))]
[UpdateAfter(typeof(LaserCollisionSystem))]

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
        JobHandle combinedDeps = state.Dependency;
        
        var job = new UpdateElapsedTimeJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
        };

        state.Dependency = job.ScheduleParallel(combinedDeps);
    }
    
    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
    
    [BurstCompile]
    partial struct UpdateElapsedTimeJob : IJobEntity
    {
        public float deltaTime;

        void Execute(ref HealthComponent health, ref TimedDestructionComponent timedComponent)
        {
            timedComponent.elapsedTime += deltaTime;

            if (timedComponent.elapsedTime >= timedComponent.lifeTime)
            {
                health.Health = 0;
            }
        }
    }
}

