using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class CallingCorridorClockPuzzle : MonoBehaviour
    {
        [SerializeField] private CallingCorridorSession session;
        [SerializeField] private CallingCorridorClock[] clocks;
        private bool subscribed;

        private void OnEnable()
        {
            if (!ValidateClocks())
            {
                Debug.LogError("CALLING_CORRIDOR_CONFIGURATION_INVALID: three ordered clocks and session required", this);
                return;
            }
            session.DecisionChanged += ShowDecision;
            subscribed = true;
            ShowDecision(session.Decision);
        }

        private bool ValidateClocks()
        {
            if (session == null || clocks == null || clocks.Length != 3) return false;
            for (int i = 0; i < clocks.Length; i++)
                if (clocks[i] == null || !clocks[i].IsConfigured) return false;
            return clocks[0] != clocks[1] && clocks[1] != clocks[2] && clocks[0] != clocks[2];
        }

        private void ShowDecision(int decision)
        {
            for (int i = 0; i < clocks.Length; i++)
                clocks[i].SetHands(
                    CallingCorridorRules.ClockHandDigit(decision, i, CallingCorridorRules.Hand.Hour),
                    CallingCorridorRules.ClockHandDigit(decision, i, CallingCorridorRules.Hand.Minute));
        }

        private void OnDisable()
        {
            if (subscribed && session != null) session.DecisionChanged -= ShowDecision;
            subscribed = false;
        }
    }
}
