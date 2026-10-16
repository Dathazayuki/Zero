using UnityEngine;
using Zero.AnomalyPrototype.Player;
using Zero.AnomalyPrototype.Flow;

namespace Zero.EnvironmentLoop
{
    public enum ElevatorTriggerKind { Approach, Entry, Exit, Unload }

    [RequireComponent(typeof(BoxCollider))]
    public sealed class ElevatorEndingTrigger : MonoBehaviour
    {
        [SerializeField] private EndFloorElevatorController elevator;
        [SerializeField] private ElevatorTriggerKind kind;
        private bool consumed;
        public void Rearm() => consumed = false;
        private void OnTriggerEnter(Collider other) => Handle(other);
        private void OnTriggerStay(Collider other) => Handle(other);
        private void Handle(Collider other)
        {
            if (consumed) return;
            var player = other.GetComponentInParent<FirstPersonController>();
            if (player == null) return;
            if (kind == ElevatorTriggerKind.Unload)
                consumed = GameFlowController.Instance != null && GameFlowController.Instance.FinishBackroomHandoff(player);
            else if (elevator != null)
                consumed = elevator.HandleTrigger(kind, player);
        }
    }
}
