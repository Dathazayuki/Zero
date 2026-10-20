using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArchiveCabinetCrushRules
    {
        public enum Phase { Waiting, Warning, Lunging, Spent, Caught }
        private readonly float warning, lunge;
        public Phase State { get; private set; }
        public float Elapsed { get; private set; }
        public float Progress => State == Phase.Lunging ? Math.Min(1f, Elapsed / lunge) : 0f;

        public ArchiveCabinetCrushRules(float warningSeconds, float lungeSeconds)
        {
            warning = Math.Max(0.1f, warningSeconds);
            lunge = Math.Max(0.1f, lungeSeconds);
        }

        public void Tick(float delta, bool near)
        {
            if (State == Phase.Caught || State == Phase.Spent) return;
            if (State == Phase.Waiting)
            {
                if (near) Change(Phase.Warning);
                return;
            }
            Elapsed += Math.Max(0f, delta);
            if (State == Phase.Warning && Elapsed >= warning) Change(Phase.Lunging);
            else if (State == Phase.Lunging && Elapsed >= lunge) Change(Phase.Spent);
        }

        public void Catch() => Change(Phase.Caught);
        public void Reset() => Change(Phase.Waiting);
        private void Change(Phase next) { State = next; Elapsed = 0f; }
    }
}
