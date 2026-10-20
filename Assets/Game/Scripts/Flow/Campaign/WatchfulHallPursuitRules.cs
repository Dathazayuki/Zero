using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class WatchfulHallPursuitRules
    {
        public enum Phase { Patrolling, Investigating, Chasing, Searching }
        private readonly float searchSeconds;
        private float elapsed;
        public Phase State { get; private set; }
        public Vector3 LastKnownPosition { get; private set; }
        public float SearchElapsed => State == Phase.Searching ? elapsed : 0f;

        public WatchfulHallPursuitRules(float searchSeconds = 8f)
        {
            this.searchSeconds = Math.Max(0.1f, searchSeconds);
        }

        public static bool IsInsideViewCone(Vector3 origin, Vector3 forward, Vector3 target, float range, float angle)
        {
            Vector3 direction = target - origin;
            return range > 0f && angle > 0f && direction.sqrMagnitude < range * range
                && Vector3.Angle(forward, direction) < angle * 0.5f;
        }

        public static float WatcherScanYaw(float seconds, float halfAngle, float degreesPerSecond)
        {
            if (halfAngle <= 0f) return 0f;
            return Mathf.PingPong(Mathf.Max(0f, seconds) * Mathf.Max(0f, degreesPerSecond) + halfAngle,
                halfAngle * 2f) - halfAngle;
        }

        public void Observe(Vector3 position)
        {
            LastKnownPosition = position;
            Transition(Phase.Chasing);
        }

        public void Report(Vector3 position, bool hasDirectSight = false)
        {
            if (hasDirectSight) return;
            LastKnownPosition = position;
            Transition(Phase.Investigating);
        }

        public void Tick(bool visible, bool arrived, float deltaTime)
        {
            if (visible || deltaTime <= 0f) return;
            elapsed += deltaTime;
            if (State == Phase.Chasing && elapsed >= 0.8f) Transition(Phase.Investigating);
            else if (State == Phase.Investigating && arrived) Transition(Phase.Searching);
            else if (State == Phase.Investigating && elapsed >= 20f) Transition(Phase.Patrolling);
            else if (State == Phase.Searching && elapsed >= searchSeconds) Transition(Phase.Patrolling);
        }

        public void Reset()
        {
            LastKnownPosition = Vector3.zero;
            Transition(Phase.Patrolling);
        }

        private void Transition(Phase next)
        {
            State = next;
            elapsed = 0f;
        }
    }
}
