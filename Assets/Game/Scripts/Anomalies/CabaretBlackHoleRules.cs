using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBlackHoleRules
    {
        public enum Phase { Waiting, Rising, Snapping, Warning, Open, Falling, Caught, Finished }
        private readonly float standSeconds, snapSeconds, warningSeconds, openSeconds, fallSeconds;
        public Phase State { get; private set; }
        public float Elapsed { get; private set; }
        public float WarningProgress => Math.Min(1f, Elapsed / warningSeconds);
        public float FallProgress => Math.Min(1f, Elapsed / fallSeconds);

        public CabaretBlackHoleRules(float standing, float snapping, float warning, float open, float falling)
        {
            standSeconds = Math.Max(.01f, standing); snapSeconds = Math.Max(.01f, snapping);
            warningSeconds = Math.Max(.01f, warning); openSeconds = Math.Max(.01f, open);
            fallSeconds = Math.Max(.01f, falling);
        }

        public void Tick(float seconds, bool nearby, bool inside, float clipSeconds)
        {
            Elapsed += Math.Max(0f, seconds);
            switch (State)
            {
                case Phase.Waiting: if (nearby) Enter(Phase.Rising); break;
                case Phase.Rising: if (clipSeconds >= standSeconds) Enter(Phase.Snapping); break;
                case Phase.Snapping: if (clipSeconds >= snapSeconds) Enter(Phase.Warning); break;
                default: AdvanceHazard(inside); break;
            }
        }

        private void AdvanceHazard(bool inside)
        {
            switch (State)
            {
                case Phase.Warning:
                    if (Elapsed >= warningSeconds) Enter(inside ? Phase.Falling : Phase.Open);
                    break;
                case Phase.Open:
                    if (Elapsed >= openSeconds) Enter(Phase.Finished);
                    else if (inside) Enter(Phase.Falling);
                    break;
                case Phase.Falling: if (Elapsed >= fallSeconds) Enter(Phase.Caught); break;
            }
        }

        public void Cancel() => Enter(Phase.Finished);
        public void Reset() => Enter(Phase.Waiting);
        private void Enter(Phase next) { State = next; Elapsed = 0f; }
    }
}
