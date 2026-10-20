using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArchiveAssemblyRules
    {
        public enum Phase { Scattered, CrawlingTogether, Assembling, Chasing, Caught }
        private readonly float scatter, crawl, assemble;
        public Phase State { get; private set; }
        public float Elapsed { get; private set; }
        public float Progress => (float)Math.Min(1d, Elapsed / Duration());

        public ArchiveAssemblyRules(float scatterSeconds, float crawlSeconds, float assembleSeconds)
        {
            scatter = Math.Max(0.1f, scatterSeconds);
            crawl = Math.Max(0.1f, crawlSeconds);
            assemble = Math.Max(0.1f, assembleSeconds);
        }

        public void Tick(float delta)
        {
            if (State == Phase.Caught || State == Phase.Chasing) return;
            Elapsed += Math.Max(0f, delta);
            if (Elapsed < Duration()) return;
            if (State == Phase.Scattered) Change(Phase.CrawlingTogether);
            else if (State == Phase.CrawlingTogether) Change(Phase.Assembling);
            else Change(Phase.Chasing);
        }

        private float Duration()
        {
            if (State == Phase.Scattered) return scatter;
            if (State == Phase.CrawlingTogether) return crawl;
            return assemble;
        }

        public void Catch() => Change(Phase.Caught);
        public void Reset() => Change(Phase.Scattered);
        private void Change(Phase next) { State = next; Elapsed = 0f; }
    }
}
