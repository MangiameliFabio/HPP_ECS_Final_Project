using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(DamageSystemOdd))]
[UpdateAfter(typeof(SimpleExplosionSystem))]
[UpdateBefore(typeof(DestructionSystem))]
public partial struct FighterRecyclingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<FighterSettings>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //var config = SystemAPI.GetSingleton<Config>();

        //var job = new RecyclingJob
        //{
        //    Config = config
        //};

        //switch (config.RunType)
        //{
        //    case RunningType.MainThread:
        //        job.Run();
        //        break;
        //    case RunningType.Scheduled:
        //        state.Dependency = job.Schedule(state.Dependency);
        //        break;
        //    case RunningType.Parallel:
        //        state.Dependency = job.ScheduleParallel(state.Dependency);
        //        break;
        //    default:
        //        break;
        //}
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}

[BurstCompile]
public partial struct RecyclingJob : IJobEntity
{
    public Config Config;
    void Execute(ref LocalTransform transform, ref Fighter fighter, ref HealthComponent healthComponent)
    {
        if (healthComponent.Health > 0)
            return;

        // reset position of the fighter
        transform.Position = fighter.StartPosition;

        healthComponent.Health = 1;
    }
}