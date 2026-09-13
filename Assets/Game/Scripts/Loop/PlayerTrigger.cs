using System;
using Zero.AnomalyPrototype.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Zero.AnomalyPrototype.Loop
{
    [RequireComponent(typeof(Collider))]
    public sealed class PlayerTrigger : MonoBehaviour
    {
        [SerializeField] private bool triggerOncePerLoop = true;
        [SerializeField] private UnityEvent onPlayerEntered;

        private bool consumed;

        public event Action<FirstPersonController> Entered;

        private void Reset()
        {
            Collider triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        public void Configure(bool oncePerLoop)
        {
            triggerOncePerLoop = oncePerLoop;
            Collider triggerCollider = GetComponent<Collider>();
            triggerCollider.isTrigger = true;
        }

        public void ResetTrigger()
        {
            consumed = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (consumed && triggerOncePerLoop)
            {
                return;
            }

            FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
            if (player == null)
            {
                return;
            }

            consumed = true;
            onPlayerEntered?.Invoke();
            Entered?.Invoke(player);
        }
    }
}
