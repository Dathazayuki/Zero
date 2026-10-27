namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretAudienceRules
    {
        private enum Phase { WaitingStage, WaitingForward, WaitingTurn, Revealed }
        private Phase phase;
        public bool HasRevealed => phase == Phase.Revealed;

        public bool Advance(bool onStage, bool facingStage, bool seesAudience)
        {
            if (phase == Phase.WaitingStage)
            {
                if (onStage) phase = Phase.WaitingForward;
                return false;
            }
            if (phase == Phase.WaitingForward)
            {
                if (onStage && facingStage) phase = Phase.WaitingTurn;
                return false;
            }
            if (phase != Phase.WaitingTurn || !seesAudience) return false;
            phase = Phase.Revealed;
            return true;
        }

        public void Reset() => phase = Phase.WaitingStage;
    }
}
