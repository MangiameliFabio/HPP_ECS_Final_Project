using Unity.Burst;
using Unity.Collections;
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


    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (health, entity) in SystemAPI.Query<RefRO<HealthComponent>>().WithEntityAccess())
        {
            if (health.ValueRO.Health <= 0)
            {
                var buffer = state.EntityManager.GetBuffer<LinkedEntityGroup>(entity);
                
                var linked = state.EntityManager.GetBuffer<LinkedEntityGroup>(entity);
                var entities = linked.Reinterpret<Entity>().AsNativeArray();
                
                ecb.DestroyEntity(entities);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        // JobHandle combinedDeps = state.Dependency;
        //
        // var job = new DestroyJob
        // {
        //     CommandBuffer = ecb.AsParallelWriter()
        // };
        //
        // state.Dependency = job.Schedule(combinedDeps);
    }
    
    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }
    
    [BurstCompile]
    partial struct DestroyJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        void Execute(Entity entity, in HealthComponent health)
        {
            // if (health.Health <= 0)
            // {
            //     var buffer = systemState.EntityManager.GetBuffer<LinkedEntityGroup>(entity);
            //
            //     CommandBuffer.DestroyEntity(buffer.AsNativeArray());
            //     CommandBuffer.Playback(systemState.EntityManager);
            //     CommandBuffer.Dispose();
            // }
        }
    }
}

