using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenWhiteRatRules
    {
        public enum Phase { Waiting, Falling, HatRolling, Walking, Idle, Jumping, Shaking, Caught }
        public Phase State { get; private set; }
        public float Elapsed { get; private set; }
        public void Reset() { State = Phase.Waiting; Elapsed = 0f; }
        public void Tick(float delta, bool trigger, bool deathFinished, bool nearby)
        {
            Elapsed += Math.Max(0f, delta);
            bool advance = State switch
            {
                Phase.Waiting => trigger,
                Phase.Falling => deathFinished,
                Phase.HatRolling => Elapsed >= 1f,
                Phase.Walking => Elapsed >= 2f,
                Phase.Idle => nearby,
                Phase.Jumping => Elapsed >= 0.45f,
                Phase.Shaking => Elapsed >= 1.2f,
                _ => false
            };
            if (!advance) return;
            State = (Phase)((int)State + 1);
            Elapsed = 0f;
        }
    }
}
