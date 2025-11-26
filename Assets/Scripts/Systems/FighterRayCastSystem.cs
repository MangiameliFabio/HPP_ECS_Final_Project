// using Unity.Burst;
// using Unity.Entities;
// using Unity.Mathematics;
// using Unity.Transforms;
// using Unity.Physics;
// using UnityEngine;
//
//
// public partial struct FighterRayCastSystem : ISystem
// {
//     public void OnCreate(ref SystemState state)
//     {
//         state.RequireForUpdate<PhysicsWorldSingleton>();
//     }
//
//     public void OnDestroy(ref SystemState state) { }
//
//     public void OnUpdate(ref SystemState state)
//     {
//         var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
//         PhysicsWorld physicsWorld = physicsWorldSingleton.PhysicsWorld;
//         var collisionWorld = physicsWorld.CollisionWorld;
//         var healthComponentLookup = SystemAPI.GetComponentLookup<HealthComponent>(true);
//
//         float delta = SystemAPI.Time.DeltaTime;
//
//         foreach (var (fighter, transform, entity) in SystemAPI.Query<RefRW<Fighter>, RefRO<LocalTransform>>().WithEntityAccess())
//         {
//             if (fighter.ValueRO.LastShotTime + fighter.ValueRO.FireCooldown > SystemAPI.Time.ElapsedTime)
//                 continue;
//             
//             float3 start = transform.ValueRO.Position;
//             float3 dir = math.normalize(transform.ValueRO.Forward());
//             float maxDistance = 100f;
//             float3 end = start + dir * maxDistance;
//
//             var input = new RaycastInput
//             {
//                 Start = start,
//                 End = end,
//                 Filter = CollisionFilter.Default
//             };
//
//             if (collisionWorld.CastRay(input, out Unity.Physics.RaycastHit hit))
//             {
//                 Entity hitEntity = Entity.Null;
//                 if (hit.RigidBodyIndex >= 0 && hit.RigidBodyIndex < physicsWorld.NumBodies)
//                 {
//                     hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
//                     if (healthComponentLookup.HasComponent(hitEntity))
//                     {
//                         HealthComponent hc = healthComponentLookup[hitEntity];
//                         hc.Health -= 1;
//
//                         float3 hitPoint = math.lerp(start, end, hit.Fraction);
//                         Debug.DrawLine(start, hitPoint, Color.red, 0.1f);
//                         
//                         fighter.ValueRW.LastShotTime = SystemAPI.Time.ElapsedTime;
//                     }
//                 }
//             }
//             else
//             {
//                 Debug.Log($"Raycast from {start} dir {dir}");
//                 Debug.DrawLine(start, end, Color.green, 0.1f);
//             }
//         }
//     }
// }

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateAfter(typeof(BuildPhysicsWorld))]
public partial struct FighterRayCastSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // falls du ein Query brauchst, kannst du es hier anlegen
        // state.RequireForUpdate<SomeTagComponent>(); // optional
    }

    public void OnDestroy(ref SystemState state) { }

    public void OnUpdate(ref SystemState state)
    {
        // PhysicsWorld lesen über das Singleton (aktueller Snapshot aus BuildPhysicsWorld)
        var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        PhysicsWorld physicsWorld = physicsWorldSingleton.PhysicsWorld; // value type snapshot
        var collisionWorld = physicsWorld.CollisionWorld;
        var healthComponentLookup = SystemAPI.GetComponentLookup<HealthComponent>();

        // Einfache iteration: für jede Entity einen Ray von Position in Forward-Richtung
        // Achtung: ForEach in ISystem braucht Entities.ForEach static-lambda / IJobEntity; hier für Klarheit eine Entities.ForEach-ähnliche Variante
        foreach (var (localTransform, fighter, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<Fighter>>().WithEntityAccess())
        {
            if (fighter.ValueRO.LastShotTime + fighter.ValueRO.FireCooldown > SystemAPI.Time.ElapsedTime)
                 continue;
            
            float3 dir = math.normalize(localTransform.ValueRO.Forward());
            float3 start = localTransform.ValueRO.Position + dir * 10f;
            float maxDistance = 100f;
            float3 end = start + dir * maxDistance;

            var input = new RaycastInput
            {
                Start = start,
                End = end,
                Filter = CollisionFilter.Default
            };

            if (collisionWorld.CastRay(input, out Unity.Physics.RaycastHit hit))
            {
                Entity hitEntity = Entity.Null;
                if (hit.RigidBodyIndex >= 0 && hit.RigidBodyIndex < physicsWorld.NumBodies)
                {
                    hitEntity = physicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                }
                if (healthComponentLookup.HasComponent(hitEntity))
                {
                    var hc = healthComponentLookup[hitEntity];
                    hc.Health -= 1;
                    healthComponentLookup[hitEntity] = hc;

                    fighter.ValueRW.LastShotTime = SystemAPI.Time.ElapsedTime;

                    float3 hitPoint = math.lerp(start, end, hit.Fraction);
                    //Debug.DrawLine(start, hitPoint, Color.red, 0.1f);
                }
            }
            else
            {
                //Debug.DrawLine(start, end, Color.green, 0.1f);
            }
        }
    }
}
