using UnityEngine;

namespace Zero.AnomalyPrototype.Utilities
{
    /// <summary>Distance-based footsteps; vertical movement and teleports never emit waves.</summary>
    public sealed class WaterRippleStepSampler
    {
        private Vector3 previousPosition;
        private float accumulatedDistance;
        private bool hasPosition;

        public bool Sample(Vector3 position, bool inWater, float stepDistance)
        {
            if (!hasPosition || !inWater)
            {
                Reset(position);
                return false;
            }
            Vector3 displacement = position - previousPosition;
            previousPosition = position;
            displacement.y = 0f;
            float distance = displacement.magnitude;
            if (distance > 3f)
            {
                accumulatedDistance = 0f;
                return false;
            }
            accumulatedDistance += distance;
            float spacing = Mathf.Max(0.1f, stepDistance);
            if (accumulatedDistance < spacing) return false;
            accumulatedDistance %= spacing;
            return true;
        }

        public void Reset(Vector3 position)
        {
            previousPosition = position;
            accumulatedDistance = 0f;
            hasPosition = true;
        }
    }
}
