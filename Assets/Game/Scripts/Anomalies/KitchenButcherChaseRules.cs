using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenButcherChaseRules
    {
        public enum Phase { Waiting, Chasing, Facing, Lifting, Caught, Escaped }
        private readonly float faceSeconds, liftSeconds;
        private float elapsed;
        public Phase State { get; private set; }
        public float FaceProgress => Math.Min(1f, elapsed / faceSeconds);
        public float LiftProgress => Math.Min(1f, elapsed / liftSeconds);

        public KitchenButcherChaseRules(float facingSeconds, float liftingSeconds)
        { faceSeconds = Math.Max(0.01f, facingSeconds); liftSeconds = Math.Max(0.01f, liftingSeconds); }

        public void Tick(float deltaTime, bool insideTrigger, bool insideRoom)
        {
            if (State == Phase.Chasing && !insideRoom) { Change(Phase.Escaped); return; }
            if (State == Phase.Waiting && insideTrigger) { Change(Phase.Chasing); return; }
            AdvanceGrab(Math.Max(0f, deltaTime));
        }

        private void AdvanceGrab(float deltaTime)
        {
            if (State != Phase.Facing && State != Phase.Lifting) return;
            elapsed += deltaTime;
            if (State == Phase.Facing && elapsed >= faceSeconds) Change(Phase.Lifting);
            else if (State == Phase.Lifting && elapsed >= liftSeconds) Change(Phase.Caught);
        }

        public bool Catch()
        {
            if (State != Phase.Chasing) return false;
            Change(Phase.Facing); return true;
        }

        public void Reset() => Change(Phase.Waiting);
        private void Change(Phase phase) { State = phase; elapsed = 0f; }
    }
}
