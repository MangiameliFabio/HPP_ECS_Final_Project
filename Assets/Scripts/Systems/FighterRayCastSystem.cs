using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Jobs;

public partial struct FighterRayCastSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<FighterSettings>();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var fighterSettings = SystemAPI.GetSingleton<FighterSettings>();
            
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        ref PhysicsWorld physicsWorld = ref physicsWorldSingleton.PhysicsWorld;
        double elapsed = SystemAPI.Time.ElapsedTime;

        var job = new RayCastFighterJob
        {
            Settings = fighterSettings,
            CurrentPhysicsWorld = physicsWorld,
            ElapsedTime = elapsed,
        };
        
        var config = SystemAPI.GetSingleton<Config>();
        switch (config.RunType)
        {
            case RunningType.MainThread:
                state.Dependency.Complete();
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
    partial struct RayCastFighterJob : IJobEntity
    {
        [ReadOnly] public FighterSettings Settings;
        [ReadOnly] public PhysicsWorld CurrentPhysicsWorld;
        public double ElapsedTime;

        void Execute(ref LocalTransform localTransform, ref Fighter fighter, ref DynamicBuffer<HitBufferElement> hitBuffer, in Entity entity)
        {
            fighter.IsShooting = false;

            if (fighter.LastShotTime + Settings.FireCooldown > ElapsedTime)
                return;

            float3 dir = math.normalize(localTransform.Forward());
            float3 start = localTransform.Position + dir * 10f;
            float3 end = start + dir * 100f;

            var input = new RaycastInput { Start = start, End = end, Filter = CollisionFilter.Default };

            if (CurrentPhysicsWorld.CollisionWorld.CastRay(input, out Unity.Physics.RaycastHit hit))
            {
                if (hit.RigidBodyIndex < 0 || hit.RigidBodyIndex >= CurrentPhysicsWorld.NumBodies)
                    return;

                var body = CurrentPhysicsWorld.Bodies[hit.RigidBodyIndex];
                Entity hitEntity = body.Entity;

                if (hitEntity != Entity.Null && (body.CustomTags & (uint)PhysicsTags.StarDestroyer) != 0)
                {
                    hitBuffer.Add(new HitBufferElement()
                    {
                        TargetEntity = hitEntity,
                        Damage = 1
                    });

                    fighter.IsShooting = true;
                    fighter.BeamStart = start;
                    fighter.BeamEnd = hit.Position;

                    fighter.LastShotTime = ElapsedTime;
                }
            }
        }
    }
}
