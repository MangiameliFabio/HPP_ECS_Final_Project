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
        state.RequireForUpdate<Fighter>();
        state.RequireForUpdate<TargetEntity>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var targetQuery = SystemAPI.QueryBuilder().WithAll<TargetEntity>().Build();

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
            LocalTransformLookup = localTransformLookup,
            Targets = targets
        };

        JobHandle handle = job.ScheduleParallel(state.Dependency);
        targets.Dispose(handle);
        state.Dependency = handle;
    }
    
    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public partial struct FindTargetJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        [ReadOnly] public NativeArray<Entity> Targets;

        public void Execute(RefRW<Fighter> fighterRef, RefRO<LocalTransform> localTransformRO)
        {
            var fighter = fighterRef.ValueRW;
            float3 myPos = localTransformRO.ValueRO.Position;

            if (fighter.targetEntity != Entity.Null)
            {
                if (LocalTransformLookup.HasComponent(fighter.targetEntity))
                {
                    if (fighter.currentState == FighterState.Attack)
                        fighter.CurrentTargetPosition = LocalTransformLookup[fighter.targetEntity].Position;
                }
                else
                {
                    fighter.targetEntity = Entity.Null;
                }
            }

            if (fighter.targetEntity == Entity.Null)
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
                    fighter.targetEntity = best;
                    fighter.CurrentTargetPosition = bestPos;
                    fighter.currentState = FighterState.Attack;
                }
            }

            float distSqToCurrentTarget = math.lengthsq(myPos - fighter.CurrentTargetPosition);
            float minDistSq = fighter.TargetMinDistance * fighter.TargetMinDistance;

            if (distSqToCurrentTarget < minDistSq)
            {
                if (fighter.currentState == FighterState.Retreat)
                    fighter.targetEntity = Entity.Null;
                else if (fighter.currentState == FighterState.Attack)
                {
                    fighter.CurrentTargetPosition = myPos + localTransformRO.ValueRO.Forward() * 200f;
                    fighter.currentState = FighterState.Retreat;
                }
            }

            fighterRef.ValueRW = fighter;
        }
    }
}

public struct TargetEntity : IComponentData { }