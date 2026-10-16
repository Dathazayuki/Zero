using TMPro;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class CallingCorridorDoor : MonoBehaviour
    {
        [SerializeField] private Transform movingLeaf;
        [SerializeField] private Collider leafCollider;
        [SerializeField] private TMP_Text plaque;
        [SerializeField] private Transform crossingPlane;
        [SerializeField] private Transform hinge;
        [SerializeField] private Transform movingHardware;
        [SerializeField, Min(0.1f)] private float openingDuration = 0.65f;
        private CallingCorridorGate gate;
        private Transform player;
        private Vector3 closedPosition;
        private Quaternion closedRotation;
        private Vector3 hardwarePosition;
        private Quaternion hardwareRotation;
        private bool initialized;
        private float opening;
        private bool consumed;
        public string Code { get; private set; }
        public bool HasReturnedToHall => player != null && crossingPlane != null &&
            Vector3.Dot(player.position - transform.position, crossingPlane.forward) < -0.4f;
        public bool IsConfigured => movingLeaf != null && leafCollider != null && plaque != null &&
            crossingPlane != null && hinge != null && movingHardware != null;

        private void Awake()
        {
            if (!IsConfigured) return;
            closedPosition = hinge.InverseTransformPoint(movingLeaf.position);
            closedRotation = Quaternion.Inverse(hinge.rotation) * movingLeaf.rotation;
            hardwarePosition = hinge.InverseTransformPoint(movingHardware.position);
            hardwareRotation = Quaternion.Inverse(hinge.rotation) * movingHardware.rotation;
            initialized = true;
        }

        public void Bind(CallingCorridorGate owner, string code)
        {
            gate = owner;
            Code = code;
            if (plaque != null) plaque.text = code;
            ResetDoor();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (gate == null || !gate.IsAvailable || !gate.IsPlayer(other)) return;
            player = other.GetComponentInParent<Zero.AnomalyPrototype.Player.FirstPersonController>().transform;
        }

        private void OnTriggerExit(Collider other)
        {
            if (gate != null && gate.IsPlayer(other) && !consumed) player = null;
        }

        private void OnTriggerStay(Collider other)
        {
            if (player == null) OnTriggerEnter(other);
        }

        private void Update()
        {
            if (!initialized || gate == null || player == null || Time.deltaTime <= 0f) return;
            opening = Mathf.MoveTowards(opening, 1f, Time.deltaTime / openingDuration);
            ApplyOpening();
            leafCollider.enabled = opening < 0.95f;
        }

        public void Consume(Transform target)
        {
            player = target;
            consumed = true;
        }

        private void ApplyOpening()
        {
            Quaternion rotation = Quaternion.Euler(0f, -90f * Mathf.SmoothStep(0f, 1f, opening), 0f);
            movingLeaf.SetPositionAndRotation(hinge.TransformPoint(rotation * closedPosition),
                hinge.rotation * rotation * closedRotation);
            movingHardware.SetPositionAndRotation(hinge.TransformPoint(rotation * hardwarePosition),
                hinge.rotation * rotation * hardwareRotation);
        }

        public void ResetDoor()
        {
            player = null;
            consumed = false;
            opening = 0f;
            if (initialized && IsConfigured) ApplyOpening();
            if (leafCollider != null) leafCollider.enabled = true;
        }

        private void OnDisable() => ResetDoor();
    }
}
