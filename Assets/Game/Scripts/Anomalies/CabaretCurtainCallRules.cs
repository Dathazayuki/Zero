using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretCurtainCallRules
    {
        public enum Phase { Waiting, Bowing, Spreading, Caught, Escaped }
        private readonly float spreadSpeed, initialRadius, maximumRadius;
        public Phase State { get; private set; }
        public float Radius { get; private set; }

        public CabaretCurtainCallRules(float speed, float initial, float maximum)
        {
            spreadSpeed = Math.Max(.1f, speed);
            initialRadius = Math.Max(.1f, initial);
            maximumRadius = Math.Max(initialRadius, maximum);
        }

        public void Tick(float delta, bool nearby, bool bowFinished, float playerDistance, bool outside)
        {
            if (State == Phase.Caught || State == Phase.Escaped) return;
            if (State != Phase.Waiting && outside) { State = Phase.Escaped; return; }
            if (State == Phase.Waiting)
            {
                if (nearby && !outside) State = Phase.Bowing;
                return;
            }
            if (State == Phase.Bowing)
            {
                if (!bowFinished) return;
                State = Phase.Spreading; Radius = initialRadius;
            }
            Radius = Math.Min(maximumRadius, Radius + spreadSpeed * Math.Max(0f, delta));
            if (playerDistance <= Radius) State = Phase.Caught;
        }

        public static float DistanceToCapsule(Vector3 point, Vector3 a, Vector3 b, float radius)
        {
            Vector3 axis = b - a;
            float t = axis.sqrMagnitude > .0001f ? Mathf.Clamp01(Vector3.Dot(point - a, axis) / axis.sqrMagnitude) : 0f;
            return Mathf.Max(0f, Vector3.Distance(point, a + axis * t) - radius);
        }

        public void Reset() { State = Phase.Waiting; Radius = 0f; }
    }
}
