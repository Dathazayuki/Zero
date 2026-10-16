using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    public sealed class CisternExitTrigger : MonoBehaviour
    {
        [SerializeField] private FloodedCisternMinigameSession session;

        private void OnTriggerEnter(Collider other) => TryComplete(other);
        private void OnTriggerStay(Collider other) => TryComplete(other);

        private void TryComplete(Collider other)
        {
            if (session == null || !session.IsPlaying) return;
            FirstPersonController player = other.GetComponentInParent<FirstPersonController>();
            if (player != null) session.TryComplete(player);
        }
    }
}
