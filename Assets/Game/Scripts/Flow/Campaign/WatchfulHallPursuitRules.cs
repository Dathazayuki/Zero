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

        public void Observe(Vector3 position)
        {
            LastKnownPosition = position;
            Transition(Phase.Chasing);
        }

        public void Report(Vector3 position)
        {
            if (State == Phase.Chasing) return;
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
