using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

[BurstCompile]
public partial struct FighterAvoidanceSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var localTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var avoidingBufferLookup = SystemAPI.GetBufferLookup<AvoidingEntity>(false);

        localTransformLookup.Update(ref state);
        avoidingBufferLookup.Update(ref state);

        var job = new FighterAvoidanceJob
        {
            localTransformLookup = localTransformLookup,
            avoidingBufferLookup = avoidingBufferLookup
        };

        job.ScheduleParallel();
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    partial struct FighterAvoidanceJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;
        [ReadOnly] public BufferLookup<AvoidingEntity> avoidingBufferLookup;

        public void Execute(ref Fighter fighter, in Entity entity)
        {
            if (!avoidingBufferLookup.HasBuffer(entity))
                return;

            var buffer = avoidingBufferLookup[entity];
            int size = buffer.Length;
            if (size == 0)
            {
                fighter.avoidanceDirection = float3.zero;
                return;
            }

            float3 averageAvoidDir = float3.zero;

            if (!localTransformLookup.HasComponent(entity))
            {
                fighter.avoidanceDirection = float3.zero;
                return;
            }

            var fighterTransform = localTransformLookup[entity];

            if (checkIfOutOfBounds(fighterTransform.Position))
            {
                averageAvoidDir -= fighterTransform.Position;
            }

            for (int i = 0; i < buffer.Length; i++)
            {
                var element = buffer[i];
                if (!localTransformLookup.HasComponent(element.entity))
                    continue;

                var bufferEntityTransform = localTransformLookup[element.entity];
                var direction = fighterTransform.Position - bufferEntityTransform.Position;
                var distance = math.lengthsq(direction);

                float radiusSq = fighter.NeighbourDetectionRadius * fighter.NeighbourDetectionRadius;
                float distanceFactor = math.clamp(math.unlerp(radiusSq, 0f, distance), 0f, 1f);

                if (distance > 0f)
                    averageAvoidDir += math.normalize(direction) * distanceFactor;
            }

            fighter.avoidanceDirection = averageAvoidDir;
        }

        bool checkIfOutOfBounds(float3 point)
        {
            if (point.x > 30 || point.x < -30 || point.y > 30 || point.y < -30 || point.z > 30 || point.z < -30)
            {
                return true;
            }

            return false;
        }
    }
}