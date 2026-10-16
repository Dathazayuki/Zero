using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    public sealed class CisternMannequinActivationTrigger : MonoBehaviour
    {
        private CisternMannequinController mannequin;

        private void Awake() => mannequin = GetComponentInParent<CisternMannequinController>();
        private void OnTriggerEnter(Collider other) => TryActivate(other);
        private void OnTriggerStay(Collider other) => TryActivate(other);

        private void TryActivate(Collider other)
        {
            if (mannequin == null || mannequin.State != CisternMannequinController.MotionState.Dormant) return;
            FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
            if (player != null) mannequin.TryActivate(player);
        }
    }
}
