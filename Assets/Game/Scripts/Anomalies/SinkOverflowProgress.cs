using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class SinkOverflowProgress
    {
        private readonly float fillDuration, spreadDuration;
        private float elapsed;
        public bool HasStarted { get; private set; }
        public float Fill => HasStarted ? Mathf.Clamp01(elapsed / fillDuration) : 0f;
        public float Spill => HasStarted ? Mathf.Clamp01((elapsed - fillDuration) / spreadDuration) : 0f;
        public bool IsOverflowing => HasStarted && elapsed >= fillDuration;

        public SinkOverflowProgress(float fillSeconds, float spreadSeconds)
        {
            fillDuration = Mathf.Max(0.1f, fillSeconds);
            spreadDuration = Mathf.Max(0.1f, spreadSeconds);
        }

        public void Start() => HasStarted = true;

        public void Advance(float deltaTime)
        {
            if (HasStarted) elapsed = Mathf.Min(fillDuration + spreadDuration, elapsed + Mathf.Max(0f, deltaTime));
        }

        public void Reset()
        {
            HasStarted = false;
            elapsed = 0f;
        }
    }
}
