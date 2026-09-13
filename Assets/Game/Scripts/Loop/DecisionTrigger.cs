using Zero.AnomalyPrototype.Player;
using UnityEngine;

namespace Zero.AnomalyPrototype.Loop
{
    [RequireComponent(typeof(Collider))]
    public sealed class DecisionTrigger : MonoBehaviour
    {
        [SerializeField] private LoopChoice choice;
        private LoopManager loopManager;
        private bool consumed;

        public void Configure(LoopManager manager, LoopChoice configuredChoice)
        {
            loopManager = manager;
            choice = configuredChoice;
            Collider decisionCollider = GetComponent<Collider>();
            decisionCollider.isTrigger = true;
        }

        public void ResetTrigger()
        {
            consumed = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (consumed || loopManager == null || other.GetComponentInParent<FirstPersonController>() == null)
            {
                return;
            }

            consumed = true;
            loopManager.SubmitChoice(choice);
        }
    }
}
