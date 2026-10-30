using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBandRules
    {
        public enum Phase { Waiting, Playing, Escaped, DeathRequested }
        private readonly float duration;
        private float elapsed;
        public Phase State { get; private set; }
        public float Progress => Math.Min(1f, elapsed / duration);

        public CabaretBandRules(float escapeSeconds)
        {
            duration = Math.Max(.1f, escapeSeconds);
        }

        public void Tick(float delta, bool atCentre, bool atExit)
        {
            if (State == Phase.Waiting)
            {
                if (atCentre && !atExit) State = Phase.Playing;
                return;
            }
            if (State != Phase.Playing) return;
            if (atExit) { State = Phase.Escaped; return; }
            elapsed = Math.Min(duration, elapsed + Math.Max(0f, delta));
            if (elapsed >= duration) State = Phase.DeathRequested;
        }

        public void Reset()
        {
            elapsed = 0f;
            State = Phase.Waiting;
        }
    }
}
