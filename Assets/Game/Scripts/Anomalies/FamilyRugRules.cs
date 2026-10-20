namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class FamilyRugRules
    {
        public enum PhaseState { Waiting, Warning, Grabbing, Attacking, Caught, Finished }
        private readonly float warningSeconds;
        public PhaseState Phase { get; private set; }
        public float Elapsed { get; private set; }
        public float Progress => Phase == PhaseState.Warning ? System.Math.Min(1f, Elapsed / warningSeconds)
            : Phase >= PhaseState.Grabbing ? 1f : 0f;

        public FamilyRugRules(float seconds) => warningSeconds = System.Math.Max(0.1f, seconds);

        public void Tick(float delta, bool inside)
        {
            if (Phase == PhaseState.Caught || Phase == PhaseState.Finished) return;
            if (!inside) { Reset(); return; }
            if (Phase == PhaseState.Waiting) Phase = PhaseState.Warning;
            Elapsed += System.Math.Max(0f, delta);
            if (Phase == PhaseState.Warning && Elapsed >= warningSeconds) Change(PhaseState.Grabbing);
        }

        public void Catch() => Change(PhaseState.Caught);
        public void Reset() => Change(PhaseState.Waiting);
        private void Change(PhaseState phase) { Phase = phase; Elapsed = 0f; }
    }
}
