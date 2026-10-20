using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArchiveDrawerSequence
    {
        private readonly float[] starts;
        private readonly float openSeconds;
        private readonly float interval;
        private float elapsed;
        private float nextAt;

        public int NextIndex { get; private set; }
        public int Count => starts.Length;

        public ArchiveDrawerSequence(int count, float openSeconds, float interval)
        {
            if (count <= 0 || openSeconds <= 0f || interval <= 0f)
                throw new ArgumentOutOfRangeException(nameof(count));
            starts = new float[count];
            this.openSeconds = openSeconds;
            this.interval = interval;
            Reset();
        }

        public void Advance(float seconds, bool nextIsNear)
        {
            elapsed += Math.Max(0f, seconds);
            if (NextIndex >= Count || !nextIsNear || elapsed < nextAt) return;
            starts[NextIndex++] = elapsed;
            nextAt = elapsed + interval;
        }

        public float Progress(int index)
        {
            if (starts[index] < 0f) return 0f;
            return Math.Min(1f, Math.Max(0f, (elapsed - starts[index]) / openSeconds));
        }

        public bool HasStarted(int index) => starts[index] >= 0f;

        public void Reset()
        {
            elapsed = 0f;
            nextAt = 0f;
            NextIndex = 0;
            for (int i = 0; i < starts.Length; i++) starts[i] = -1f;
        }
    }
}
