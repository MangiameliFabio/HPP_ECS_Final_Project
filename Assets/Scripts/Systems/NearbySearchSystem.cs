using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[BurstCompile]
public partial struct NearbySearchSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<FighterSettings>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        ref var physicsWorld = ref physicsSingleton.PhysicsWorld;
        
        var fighterSettings = SystemAPI.GetSingleton<FighterSettings>();

        var job = new NearbySearchJob()
        {
            Settings = fighterSettings,
            CurrentPhysicsWorld = physicsWorld
        };
        
        var config = SystemAPI.GetSingleton<Config>();
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
                break;
        }
    }

    [BurstCompile]
    partial struct NearbySearchJob : IJobEntity
    {
        [ReadOnly] public FighterSettings Settings;
        [ReadOnly] public PhysicsWorld CurrentPhysicsWorld;

        void Execute(in LocalTransform localTransform,
                    in FighterComponent fighterComponent,
                    ref DynamicBuffer<NearbyFighter> swarmBuffer,
                    ref DynamicBuffer<AvoidingEntityBufferElement> avoidanceBuffer,
                    in Entity entity)
        {
            swarmBuffer.Clear();
            avoidanceBuffer.Clear();

            float3 center = localTransform.Position;
            float radiusVal = Settings.NeighbourDetectionRadius;

            var pInput = new PointDistanceInput
            {
                Position = center,
                MaxDistance = radiusVal,
                Filter = CollisionFilter.Default
            };

            var hits = new NativeList<DistanceHit>(Allocator.Temp);
            bool gotAny = CurrentPhysicsWorld.CollisionWorld.CalculateDistance(pInput, ref hits);
            if (!gotAny)
            {
                hits.Dispose();
                return;
            }

            for (int i = 0; i < hits.Length; ++i)
            {
                var hit = hits[i];
                int rbIndex = hit.RigidBodyIndex;
                if (rbIndex < 0 || rbIndex >= CurrentPhysicsWorld.NumBodies)
                    continue;

                var body = CurrentPhysicsWorld.Bodies[rbIndex];
                Entity hitEntity = body.Entity;

                if (hitEntity == Entity.Null || hitEntity == entity)
                    continue;

                // TODO: I am not sure why we are casting to uint here, when the initial tag is an int
                // Check if custom tag "Avoid" is set
                if ((body.CustomTags & (uint)PhysicsTags.Avoid) != 0)
                {
                    avoidanceBuffer.Add(new AvoidingEntityBufferElement
                    {
                        AvoidingEntity = hitEntity,
                        HitPosition = hit.Position,
                    });
                }

                // Check if custom tag "Fighter" is set
                if ((body.CustomTags & (uint)PhysicsTags.Fighter) != 0)
                {
                    swarmBuffer.Add(new NearbyFighter { entity = hitEntity });
                }
            }

            hits.Dispose();
        }
    }
}
