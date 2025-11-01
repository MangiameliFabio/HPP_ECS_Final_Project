using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions.Must;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;

public partial struct StarDestroyerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new MoveTowardsTargetPoint
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            globalTime = (float)SystemAPI.Time.ElapsedTime // does the cast cause it to always be the same? like 9999999...1234 and 9999999...1235 become 9999999 (x2) or 1234 and 1235?
        };

        var handle = job.ScheduleParallel(state.Dependency);
        state.Dependency = handle;
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}

[BurstCompile]
public partial struct MoveTowardsTargetPoint : IJobEntity
{
    public float deltaTime;
    public float globalTime;

    void Execute(ref LocalTransform transform, ref StarDestroyer starDestroyer)
    {
        float process = starDestroyer.MovementProcess + starDestroyer.Speed * deltaTime;
        starDestroyer.MovementProcess = process;
        int id = starDestroyer.ID;

        // get new point, if current set of points is finished
        if (process >= 1)
        {
            starDestroyer.MovementProcess = 0;
            process = 0;

            // update points
            float3 endpoint = starDestroyer.Point3;
            float3 secondPoint = starDestroyer.Point2;

            // new randoml direction between -1 and 1 for the x,z plane
            float3 newPoint = new float3(
                2 * (0.5f - PseudoRandom(id, 0, globalTime)),
                0.0f,
                2 * (0.5f - PseudoRandom(id, 1, globalTime))
            );
            newPoint = math.normalize(newPoint);
            float scaleFactor = PseudoRandom(id, 2, globalTime) * starDestroyer.MovementRadius;
            float3 randomNewPoint = newPoint * scaleFactor;
            randomNewPoint.y = 0;

            starDestroyer.Point1 = endpoint;
            starDestroyer.Point2 = endpoint + (endpoint - secondPoint);
            starDestroyer.Point3 = randomNewPoint;
        }

        // Bezier Mathmagic
        // (Position on the Bezier curve)
        float invProcess = 1 - process;
        float3 bezierPosition = (invProcess * invProcess * starDestroyer.Point1) +
                                (2 * invProcess * process * starDestroyer.Point2) +
                                (process * process * starDestroyer.Point3);

        // (Tangent of the Bezier curve at current position)
        float3 tangent = math.normalize(
            2 * invProcess * (starDestroyer.Point2 - starDestroyer.Point1) +
            2 * process * (starDestroyer.Point3 - starDestroyer.Point2)
        );

        // keep y position
        float transformY = transform.Position.y;
        bezierPosition.y = transformY;

        // Adjust rotation to face movement direction
        if (math.lengthsq(tangent) > 0)
        {
            quaternion targetRotation = quaternion.LookRotationSafe(math.normalize(tangent), math.up());
            transform.Rotation = math.slerp(transform.Rotation, targetRotation, 0.1f);
        }

        transform.Position = bezierPosition; // I'd prefer not setting the position but moving in the direction but for now this seems easier
    }

    float PseudoRandom(int starDestrpyerID, int callID, float globalTime)
    {
        return math.frac(Mathf.Sin((starDestrpyerID + callID) * 12.9898f + (starDestrpyerID * globalTime)* 78.233f) * 43758.5453f);
    }
}