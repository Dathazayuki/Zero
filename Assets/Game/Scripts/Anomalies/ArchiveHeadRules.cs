using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArchiveHeadRules
    {
        public enum Phase { Waiting, Rattling, Falling, Growing, Escaped, Lunging, Caught }
        private readonly float warningSeconds, fallSeconds, growSeconds;
        public Phase State { get; private set; }
        public float Elapsed { get; private set; }
        public float Progress => Math.Min(1f, Elapsed / Duration());

        public ArchiveHeadRules(float warning, float fall, float grow)
        {
            warningSeconds = Math.Max(0.1f, warning);
            fallSeconds = Math.Max(0.1f, fall);
            growSeconds = Math.Max(0.1f, grow);
        }

        public void Tick(float delta, bool approached, bool looking)
        {
            if (State == Phase.Waiting)
            {
                if (approached) Enter(Phase.Rattling);
                return;
            }
            if (State == Phase.Escaped || State == Phase.Caught) return;
            Elapsed += Math.Max(0f, delta);
            if (Elapsed < Duration()) return;
            if (State == Phase.Rattling) Enter(Phase.Falling);
            else if (State == Phase.Falling) Enter(Phase.Growing);
            else if (State == Phase.Growing) Enter(looking ? Phase.Lunging : Phase.Escaped);
        }

        public void Catch() => Enter(Phase.Caught);
        public void Reset() => Enter(Phase.Waiting);

        private void Enter(Phase next)
        {
            State = next;
            Elapsed = 0f;
        }

        private float Duration()
        {
            if (State == Phase.Rattling) return warningSeconds;
            if (State == Phase.Falling) return fallSeconds;
            if (State == Phase.Growing) return growSeconds;
            return 0.35f;
        }
    }
}
