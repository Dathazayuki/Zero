namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class TelevisionMotherRules
    {
        public enum PhaseState { Warning, Emerging, Chasing, Lunging, Caught, Finished }
        private readonly float warning, emergence, lifetime;
        private float chaseTime;
        public PhaseState Phase { get; private set; }
        public float Elapsed { get; private set; }
        public float WarningProgress => System.Math.Min(1f, System.Math.Max(0f, Elapsed / warning));
        public float EmergenceProgress => System.Math.Min(1f, System.Math.Max(0f, Elapsed / emergence));

        public TelevisionMotherRules(float warningSeconds, float emergenceSeconds, float chaseSeconds)
        {
            warning = System.Math.Max(0.1f, warningSeconds);
            emergence = System.Math.Max(0.1f, emergenceSeconds);
            lifetime = System.Math.Max(1f, chaseSeconds);
        }

        public void Tick(float delta, bool insideRoom, bool near)
        {
            if (Phase == PhaseState.Caught || Phase == PhaseState.Finished) return;
            if (!insideRoom) { Change(PhaseState.Finished); return; }
            Elapsed += System.Math.Max(0f, delta);
            if (Phase == PhaseState.Warning && Elapsed >= warning) Change(PhaseState.Emerging);
            else if (Phase == PhaseState.Emerging && Elapsed >= emergence) Change(PhaseState.Chasing);
            else if (Phase == PhaseState.Chasing) Chase(delta);
        }

        private void Chase(float delta)
        {
            chaseTime += System.Math.Max(0f, delta);
            if (chaseTime >= lifetime) Change(PhaseState.Finished);
        }

        public void Catch() => Change(PhaseState.Caught);
        public void Reset() { chaseTime = 0f; Change(PhaseState.Warning); }
        private void Change(PhaseState phase) { Phase = phase; Elapsed = 0f; }
    }
}
