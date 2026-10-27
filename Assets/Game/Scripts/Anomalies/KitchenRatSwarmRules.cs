using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenRatSwarmRules
    {
        public enum Phase { Waiting, Warning, Running, Falling, Trampling, Caught, Escaped }
        public Phase State { get; private set; }
        public float Elapsed { get; private set; }
        public void Reset() { State = Phase.Waiting; Elapsed = 0f; }
        public void Tick(float delta, bool trigger, bool touching, bool finished)
        {
            Elapsed += Math.Max(0f, delta);
            if (State == Phase.Running && touching) { Enter(Phase.Falling); return; }
            if (State == Phase.Running && finished) { Enter(Phase.Escaped); return; }
            bool advance = State switch
            {
                Phase.Waiting => trigger,
                Phase.Warning => Elapsed >= 0.8f,
                Phase.Falling => Elapsed >= 0.65f,
                Phase.Trampling => Elapsed >= 1.5f,
                _ => false
            };
            if (advance) Enter((Phase)((int)State + 1));
        }
        private void Enter(Phase phase) { State = phase; Elapsed = 0f; }
    }
}
