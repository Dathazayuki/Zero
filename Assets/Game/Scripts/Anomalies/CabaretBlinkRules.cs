using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBlinkRules
    {
        public enum Phase { Waiting, Dark, Light, Escaped, Caught }
        private readonly float darkSeconds, lightSeconds;
        private float remaining;
        public Phase State { get; private set; }
        public bool IsDark => State == Phase.Dark;

        public CabaretBlinkRules(float dark, float light)
        {
            darkSeconds = Math.Max(.05f, dark);
            lightSeconds = Math.Max(.1f, light);
        }

        public bool Tick(float delta, bool onStage, bool outside)
        {
            if (State == Phase.Caught || State == Phase.Escaped) return false;
            if (State != Phase.Waiting && outside) { State = Phase.Escaped; return false; }
            if (State == Phase.Waiting)
            {
                if (!onStage || outside) return false;
                State = Phase.Dark; remaining = darkSeconds; return true;
            }
            remaining -= Math.Max(0f, delta);
            if (remaining > 0f) return false;
            State = IsDark ? Phase.Light : Phase.Dark;
            remaining = IsDark ? darkSeconds : lightSeconds;
            return IsDark;
        }

        public void Catch() { if (State == Phase.Dark) State = Phase.Caught; }
        public void Reset() { State = Phase.Waiting; remaining = 0f; }
    }
}
