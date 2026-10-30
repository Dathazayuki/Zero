using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretRitualRules
    {
        public enum Phase { Waiting, Blackout, Rising, Active, Attacking, Caught, Releasing, Safe, Warning, Recovery }
        private readonly float blackoutSeconds, riseSeconds, trackingSeconds, warningSeconds, releaseSeconds;
        private const int DefaultTrackingSeed = 51717;
        private int trackingSeed = DefaultTrackingSeed;
        private Random trackingRandom = new Random(DefaultTrackingSeed);
        private float trackingVariationSeconds = 1.5f;
        private float activeTrackingSeconds;
        public Phase State { get; private set; }
        public float Elapsed { get; private set; }
        public int StabilizedMask { get; private set; }
        public int DefeatedCount => ((StabilizedMask & 1) != 0 ? 1 : 0)
            + ((StabilizedMask & 2) != 0 ? 1 : 0) + ((StabilizedMask & 4) != 0 ? 1 : 0);
        public float Difficulty => 1f + DefeatedCount * .4f;
        public float PhaseProgress => Math.Min(1f, Elapsed / PhaseDuration());
        public bool StrikeIsLive => State == Phase.Attacking && Elapsed >= .25f;

        public static bool CanEyesFlee(Phase phase, int defeatedMask)
            => phase == Phase.Active && (defeatedMask & 7) != 0;

        public static float MinimumEscapeDistanceSquared(float currentDistanceSquared)
            => Math.Min(9f, Math.Max(6.25f, currentDistanceSquared + .5f));

        public static float EyeSpeedMultiplier(int defeatedMask)
        {
            int mask = defeatedMask & 7;
            return mask == 3 || mask == 5 || mask == 6 ? 1.5f : 1f;
        }

        public CabaretRitualRules(float blackout, float rise, float tracking, float warning, float release)
        {
            blackoutSeconds = Math.Max(.1f, blackout); riseSeconds = Math.Max(.1f, rise);
            trackingSeconds = Math.Max(2f, tracking); warningSeconds = Math.Max(.8f, warning);
            releaseSeconds = Math.Max(.1f, release);
        }

        public void Tick(float delta, bool entered, bool playerInStrike, int targetMask)
        {
            if (delta < 0f || State == Phase.Safe || State == Phase.Caught) return;
            if (State == Phase.Waiting) { if (entered) Enter(Phase.Blackout); return; }
            Elapsed += delta;
            if (StrikeIsLive)
            {
                if (playerInStrike) { Enter(Phase.Caught); return; }
                StabilizedMask |= targetMask & 7;
            }
            if (Elapsed >= PhaseDuration()) AdvanceTimedPhase();
        }

        private void AdvanceTimedPhase()
        {
            switch (State)
            {
                case Phase.Blackout: Enter(Phase.Rising); break;
                case Phase.Rising: Enter(Phase.Active); break;
                case Phase.Active: Enter(Phase.Warning); break;
                case Phase.Warning: Enter(Phase.Attacking); break;
                case Phase.Attacking: Enter(StabilizedMask == 7 ? Phase.Releasing : Phase.Recovery); break;
                case Phase.Recovery: Enter(Phase.Active); break;
                case Phase.Releasing: Enter(Phase.Safe); break;
            }
        }

        private float PhaseDuration()
        {
            switch (State)
            {
                case Phase.Blackout: return blackoutSeconds;
                case Phase.Rising: return riseSeconds;
                case Phase.Releasing: return releaseSeconds;
                case Phase.Active: return activeTrackingSeconds / Difficulty;
                case Phase.Warning: return Math.Max(.8f, warningSeconds / (1f + DefeatedCount * .15f));
                case Phase.Attacking: return .6f;
                case Phase.Recovery: return .8f;
                default: return 1f;
            }
        }

        private void Enter(Phase phase)
        {
            State = phase; Elapsed = 0f;
            if (phase == Phase.Active)
                activeTrackingSeconds = Math.Max(2f, trackingSeconds + (float)(trackingRandom.NextDouble() * 2d - 1d) * trackingVariationSeconds);
        }

        public void SetRandomSeed(int seed)
        {
            trackingSeed = seed;
            trackingRandom = new Random(seed);
        }

        public void SetTrackingVariation(float seconds)
            => trackingVariationSeconds = Math.Max(0f, seconds);

        public void Reset()
        {
            State = Phase.Waiting; Elapsed = 0f; StabilizedMask = 0;
            trackingRandom = new Random(trackingSeed);
        }
    }
}
