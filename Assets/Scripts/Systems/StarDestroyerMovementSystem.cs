using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

public partial struct StarDestroyerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        state.RequireForUpdate<StarDestroyerSettings>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var startDestroyerSettings = SystemAPI.GetSingleton<StarDestroyerSettings>();
        
        var job = new MoveTowardsTargetPoint
        {
            DestroyerSettings = startDestroyerSettings,
            deltaTime = SystemAPI.Time.DeltaTime,
            globalTime = (float)SystemAPI.Time.ElapsedTime // does the cast cause it to always be the same? like 9999999...1234 and 9999999...1235 become 9999999 (x2) or 1234 and 1235?
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
    public void OnDestroy(ref SystemState state)
    {

    }
}


[BurstCompile]
public partial struct MoveTowardsTargetPoint : IJobEntity
{
    [ReadOnly] public StarDestroyerSettings DestroyerSettings;
    public float deltaTime;
    public float globalTime;

    [BurstCompile]
    void Execute(ref LocalTransform transform, ref StarDestroyer starDestroyer)
    {
        if (starDestroyer.HasJumpedInScene)
        {
            PatrolPoints(ref transform, ref starDestroyer, ref DestroyerSettings);
        }
        else
        {
            // Lerp to first point  
            float3 direction = starDestroyer.Point1 - transform.Position;
            float distance = math.length(direction);

            if (distance > 0.01f) // Threshold to stop lerping  
            {
                float3 step = math.normalize(direction) * 5000 * DestroyerSettings.Speed;
                transform.Position += math.length(step) < distance ? step : direction;

                //// Scale the transform based on distance  
                //float scale = math.lerp(0, 1, 1 - math.saturate(distance / 5000)); // Assuming 100 is the max distance  
                //transform.Scale = scale;
            }
            else
            {
                starDestroyer.HasJumpedInScene = true;
                //transform.Scale = 1; // Ensure final scale is 1  
            }
        }
    }

    void PatrolPoints(ref LocalTransform transform, ref StarDestroyer starDestroyer, ref StarDestroyerSettings settings)
    {
        float process = starDestroyer.MovementProcess + settings.Speed * deltaTime;
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
            float scaleFactor = PseudoRandom(id, 2, globalTime) * settings.MovementRadius;
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

        float transformY = transform.Position.y;
        bezierPosition.y = transformY;

        if (math.lengthsq(tangent) > 0)
        {
            quaternion targetRotation = quaternion.LookRotationSafe(math.normalize(tangent), math.up());
            transform.Rotation = math.slerp(transform.Rotation, targetRotation, 0.1f);

            if (math.dot(transform.Rotation.value, targetRotation.value) < 0)
            {
                targetRotation.value = -targetRotation.value;
            }
        }

        transform.Position = bezierPosition; // I'd prefer not setting the position but moving in the direction but for now this seems easier
    }

    float PseudoRandom(int starDestrpyerID, int callID, float globalTime)
    {
        return math.frac(Mathf.Sin((starDestrpyerID + callID) * 12.9898f + (starDestrpyerID * globalTime) * 78.233f) * 43758.5453f);
    }
}

