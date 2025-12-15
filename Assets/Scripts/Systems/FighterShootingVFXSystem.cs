using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateAfter(typeof(FighterRayCastSystem))]
public partial struct FighterShootingVFXSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var config = SystemAPI.GetSingleton<Config>();

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        var job = new FighterShootingVFXJob
        {
            BeamVFXPrefab = config.BeamVFX,
            ECB = ecb
        };

        switch (config.RunType)
        {
            case RunningType.MainThread:
                job.Run();
                break;
            case RunningType.Scheduled:
                state.Dependency = job.Schedule(state.Dependency);
                break;
            case RunningType.Parallel:
                state.Dependency = job.ScheduleParallel(state.Dependency);
                break;
            default:
                throw new System.ArgumentOutOfRangeException();
        }
    }

    [BurstCompile]
    partial struct FighterShootingVFXJob : IJobEntity
    {
        public Entity BeamVFXPrefab;
        public EntityCommandBuffer.ParallelWriter ECB;

        void Execute([ChunkIndexInQuery] int sortKey,
                     in Fighter fighter,
                     in LocalToWorld transform)
        {
            if (!fighter.IsShooting)
                return;

            Entity beamVFX = ECB.Instantiate(sortKey, BeamVFXPrefab);

            ECB.SetComponent(sortKey, beamVFX, new LocalTransform
            {
                Position = transform.Position,
                Rotation = transform.Rotation,
                Scale = 1f
            });

            ECB.AddComponent<BeamComponent>(sortKey, beamVFX);
            ECB.AddComponent(sortKey, beamVFX, new TimedDestructionComponent
            {
                lifeTime = 0.5f,
                elapsedTime = 0f
            });
            ECB.AddComponent(sortKey, beamVFX, new HealthComponent
            {
                Health = 1f
            });
        }
    }

    public void OnDestroy(ref SystemState state) { }
}
