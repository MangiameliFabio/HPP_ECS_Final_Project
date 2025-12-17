using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public partial struct FighterFindTargetSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<FighterComponent>();
        state.RequireForUpdate<TargetEntity>();
        state.RequireForUpdate<FighterSettings>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var fighterSettings = SystemAPI.GetSingleton<FighterSettings>();
        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var targetQuery = SystemAPI.QueryBuilder().WithAll<TargetEntity>().Build();
        var config = SystemAPI.GetSingleton<Config>();
        
        if (targetQuery.IsEmptyIgnoreFilter)
            return;

        NativeArray<Entity> targets = targetQuery.ToEntityArray(Allocator.TempJob);
        if (!targets.IsCreated || targets.Length == 0)
        {
            if (targets.IsCreated) targets.Dispose();
            return;
        }

        var job = new FindTargetJob
        {
            Settings = fighterSettings,
            LocalTransformLookup = localTransformLookup,
            Targets = targets
        };
        
        switch (config.RunType)
        {
            case RunningType.MainThread:
                job.Run();
                targets.Dispose();
                break;

            case RunningType.Scheduled:
                var h1 = job.Schedule(state.Dependency);
                targets.Dispose(h1);
                state.Dependency = h1;
                break;

            case RunningType.Parallel:
                var h2 = job.ScheduleParallel(state.Dependency);
                targets.Dispose(h2);
                state.Dependency = h2;
                break;
        }
    }
    
    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public partial struct FindTargetJob : IJobEntity
    {
        [ReadOnly] public FighterSettings Settings;
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        [ReadOnly] public NativeArray<Entity> Targets;

        public void Execute(RefRW<FighterComponent> fighterRef, RefRO<LocalTransform> localTransformRO)
        {
            var fighter = fighterRef.ValueRW;
            float3 myPos = localTransformRO.ValueRO.Position;

            if (fighter.TargetEntity != Entity.Null)
            {
                if (LocalTransformLookup.HasComponent(fighter.TargetEntity))
                {
                    if (fighter.CurrentState == FighterState.Attack)
                        fighter.CurrentTargetPosition = LocalTransformLookup[fighter.TargetEntity].Position;
                }
                else
                {
                    fighter.TargetEntity = Entity.Null;
                }
            }

            if (fighter.TargetEntity == Entity.Null)
            {
                Entity best = Entity.Null;
                float bestDistSq = float.MaxValue;
                float3 bestPos = float3.zero;

                for (int i = 0; i < Targets.Length; i++)
                {
                    var t = Targets[i];
                    if (!LocalTransformLookup.HasComponent(t))
                        continue;

                    float3 tPos = LocalTransformLookup[t].Position;
                    float distSq = math.lengthsq(tPos - myPos);

                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        best = t;
                        bestPos = tPos;
                    }
                }

                if (best != Entity.Null)
                {
                    fighter.TargetEntity = best;
                    fighter.CurrentTargetPosition = bestPos;
                    fighter.CurrentState = FighterState.Attack;
                }
            }

            float distSqToCurrentTarget = math.lengthsq(myPos - fighter.CurrentTargetPosition);
            float minDistSq = Settings.TargetMinDistance * Settings.TargetMinDistance;

            if (distSqToCurrentTarget < minDistSq)
            {
                if (fighter.CurrentState == FighterState.Retreat)
                    fighter.TargetEntity = Entity.Null;
                else if (fighter.CurrentState == FighterState.Attack)
                {
                    fighter.CurrentTargetPosition = myPos + localTransformRO.ValueRO.Forward() * 200f;
                    fighter.CurrentState = FighterState.Retreat;
                }
            }

            fighterRef.ValueRW = fighter;
        }
    }
}

public struct TargetEntity : IComponentData { }