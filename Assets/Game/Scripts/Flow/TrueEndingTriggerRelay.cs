using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public enum TrueEndingTriggerKind { Room1Start, Room1Complete, Room2Start, Room2Complete, BlueSkyEntry, EndingPlatform }

    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    public sealed class TrueEndingTriggerRelay : MonoBehaviour
    {
        [SerializeField] private TrueEndingGameplayController controller;
        [SerializeField] private TrueEndingTriggerKind kind;
        private bool consumed;
        public void Rearm() => consumed = false;
        private void OnTriggerEnter(Collider other)
        {
            if (consumed || controller == null) return;
            var player = other.GetComponentInParent<FirstPersonController>();
            if (player != null && controller.TryEnter(kind, player)) consumed = true;
        }
    }
}
