using System;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBarGuestRules
    {
        public enum Phase { Unseen, Watching, Armed, Vanished }
        public Phase State { get; private set; }
        private float away;
        public bool Advance(float seconds, bool visible)
        {
            if (State == Phase.Vanished) return false;
            if (State == Phase.Unseen) { if (visible) State = Phase.Watching; return false; }
            if (State == Phase.Armed)
            { if (!visible) return false; State = Phase.Vanished; return true; }
            away = visible ? 0f : away + Math.Max(0f, seconds);
            if (away >= .3f) State = Phase.Armed;
            return false;
        }
        public void Reset() { State = Phase.Unseen; away = 0f; }
    }
}
