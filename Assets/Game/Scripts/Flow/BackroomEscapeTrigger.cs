using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class BackroomEscapeTrigger : MonoBehaviour
    {
        [SerializeField] private BackroomGameplayManager manager;
        public void Configure(BackroomGameplayManager owner) => manager = owner;
        private void OnTriggerEnter(Collider other) => Handle(other);
        private void OnTriggerStay(Collider other) => Handle(other);
        private void Handle(Collider other)
        {
            if (manager == null) return;
            var player = other.GetComponentInParent<FirstPersonController>();
            if (player != null) manager.OnPlayerEscapedBackroom(player);
        }
    }
}
