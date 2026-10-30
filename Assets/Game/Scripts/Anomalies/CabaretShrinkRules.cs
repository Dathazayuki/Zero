using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public static class CabaretShrinkRules
    {
        public static float Evaluate(float distance, float fullSizeDistance, float minimumScale)
        {
            float t = Math.Max(0f, Math.Min(1f, distance / Math.Max(.01f, fullSizeDistance)));
            float minimum = Math.Max(.1f, Math.Min(1f, minimumScale));
            return minimum + (1f - minimum) * t * t * (3f - 2f * t);
        }
    }
}
