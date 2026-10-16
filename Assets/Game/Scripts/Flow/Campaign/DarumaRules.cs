using System;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class DarumaRules
    {
        public enum Phase { Waiting, Moving, TurningToWatch, Watching, TurningAway, Caught, Completed }
        private readonly float duration;
        private readonly float moveSeconds;
        private readonly float watchSeconds;
        private readonly float turnSeconds;
        private readonly float movementTolerance;
        private float phaseElapsed;
        private float phaseDuration;
        private float routeProgress;
        private Random random;
        private float minimumMultiplier;
        private float maximumMultiplier;
        private float finishPressure;
        public Phase State { get; private set; }
        public float Remaining { get; private set; }
        public float PhaseDuration => phaseDuration;
        public float TurnFraction => Math.Min(1f, phaseElapsed / Math.Max(0.1f, phaseDuration));

        public DarumaRules(float duration, float moveSeconds, float watchSeconds, float turnSeconds, float movementTolerance)
        {
            this.duration = Math.Max(1f, duration);
            this.moveSeconds = Math.Max(0.1f, moveSeconds);
            this.watchSeconds = Math.Max(0.1f, watchSeconds);
            this.turnSeconds = Math.Max(0.1f, turnSeconds);
            this.movementTolerance = Math.Max(0.001f, movementTolerance);
            Reset();
        }

        public void ConfigureRandomTiming(int seed, float minimum, float maximum, float pressure)
        {
            random = new Random(seed);
            minimumMultiplier = Math.Max(0.1f, Math.Min(minimum, maximum));
            maximumMultiplier = Math.Max(minimumMultiplier, Math.Max(minimum, maximum));
            finishPressure = Math.Max(0f, Math.Min(0.5f, pressure));
        }

        public void Reset()
        {
            Remaining = duration;
            routeProgress = 0f;
            Transition(Phase.Waiting);
        }

        public void Start()
        {
            if (State == Phase.Waiting) Transition(Phase.Moving);
        }

        public void Tick(float deltaTime, float progress = 0f)
        {
            if (State == Phase.Waiting || State == Phase.Caught || State == Phase.Completed) return;
            routeProgress = Math.Max(0f, Math.Min(1f, progress));
            float step = Math.Max(0f, deltaTime);
            Remaining = Math.Max(0f, Remaining - step);
            if (Remaining <= 0f) { Catch(); return; }
            phaseElapsed += step;
            AdvanceCycle();
        }

        private void AdvanceCycle()
        {
            if (phaseElapsed < phaseDuration) return;
            switch (State)
            {
                case Phase.Moving: Transition(Phase.TurningToWatch); break;
                case Phase.TurningToWatch: Transition(Phase.Watching); break;
                case Phase.Watching: Transition(Phase.TurningAway); break;
                case Phase.TurningAway: Transition(Phase.Moving); break;
            }
        }

        public bool ObserveMovement(float distance)
        {
            if (State != Phase.Watching || distance <= movementTolerance) return false;
            return Catch();
        }

        public bool Catch()
        {
            if (State == Phase.Caught || State == Phase.Completed || State == Phase.Waiting) return false;
            Transition(Phase.Caught);
            return true;
        }

        public bool Complete()
        {
            if (State == Phase.Waiting || State == Phase.Caught || State == Phase.Completed) return false;
            Transition(Phase.Completed);
            return true;
        }

        private void Transition(Phase next)
        {
            State = next;
            phaseElapsed = 0f;
            phaseDuration = SelectDuration(next);
        }

        private float SelectDuration(Phase phase)
        {
            if (phase == Phase.Waiting || phase == Phase.Caught || phase == Phase.Completed) return 0f;
            float seconds;
            switch (phase)
            {
                case Phase.Moving: seconds = moveSeconds; break;
                case Phase.Watching: seconds = watchSeconds; break;
                default: seconds = turnSeconds; break;
            }
            if (random == null) return seconds;
            float multiplier = minimumMultiplier + (maximumMultiplier - minimumMultiplier) * (float)random.NextDouble();
            float pressure = routeProgress * finishPressure;
            seconds *= multiplier * (phase == Phase.Watching ? 1f + pressure : 1f - pressure);
            bool turning = phase == Phase.TurningToWatch || phase == Phase.TurningAway;
            return Math.Max(turning ? 0.45f : 0.7f, seconds);
        }
    }
}
