using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class BackroomChaseStartTrigger : MonoBehaviour
    {
        [SerializeField] private BackroomGameplayManager manager;
        private bool consumed;
        public void Configure(BackroomGameplayManager owner) => manager = owner;
        public void Rearm() => consumed = false;
        private void OnTriggerEnter(Collider other) => Handle(other);
        private void OnTriggerStay(Collider other) => Handle(other);
        private void Handle(Collider other)
        {
            if (consumed || manager == null) return;
            var player = other.GetComponentInParent<FirstPersonController>();
            if (player != null) consumed = manager.OnPlayerEnterBackroom(player);
        }
    }
}
