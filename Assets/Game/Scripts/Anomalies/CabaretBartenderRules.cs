using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBartenderRules
    {
        public enum Phase { Waiting, Fetching, Uncapping, Pouring, Serving, Returning, Finished }
        private readonly float fetch, uncap, pour, serve, returning;
        public Phase State { get; private set; }
        public float Elapsed { get; private set; }
        public float Progress => Math.Min(1f, Elapsed / Duration);
        private float Duration => State switch
        {
            Phase.Fetching => fetch, Phase.Uncapping => uncap, Phase.Pouring => pour,
            Phase.Serving => serve, Phase.Returning => returning, _ => float.MaxValue
        };

        public CabaretBartenderRules(float fetching, float uncapping, float pouring, float serving, float returnSeconds)
        {
            fetch = Math.Max(.01f, fetching); uncap = Math.Max(.01f, uncapping);
            pour = Math.Max(.01f, pouring); serve = Math.Max(.01f, serving);
            returning = Math.Max(.01f, returnSeconds);
        }

        public bool TryStart()
        {
            if (State != Phase.Waiting) return false;
            State = Phase.Fetching;
            Elapsed = 0f;
            return true;
        }

        public void Tick(float seconds)
        {
            if (State == Phase.Waiting || State == Phase.Finished || seconds <= 0f) return;
            Elapsed += seconds;
            while (Elapsed >= Duration && State != Phase.Finished)
            {
                Elapsed -= Duration;
                State++;
            }
        }

        public void Reset() { State = Phase.Waiting; Elapsed = 0f; }
    }
}
