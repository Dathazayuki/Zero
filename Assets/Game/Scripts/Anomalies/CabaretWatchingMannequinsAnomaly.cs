using UnityEngine;
using UnityEngine.Serialization;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretWatchingMannequinsAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform player;
        [FormerlySerializedAs("mannequins")]
        [SerializeField] private Transform[] necks;
        [SerializeField] private Vector3[] localFacingDirections;
        [SerializeField] private BoxCollider roomVolume;
        [SerializeField, Min(1f)] private float turnSpeed = 90f;
        [SerializeField, Range(0f, 180f)] private float maxYaw = 180f;
        [SerializeField, Range(0f, 80f)] private float maxPitch = 45f;
        private Quaternion[] normalRotations;
        public bool IsConfigured => player != null && roomVolume != null && necks != null
            && necks.Length > 0 && localFacingDirections != null && localFacingDirections.Length == necks.Length;

        public void Configure(Transform observer, Transform[] figures, Vector3[] facingDirections, BoxCollider room)
        {
            player = observer; necks = figures; localFacingDirections = facingDirections; roomVolume = room;
            normalRotations = null;
        }

        public override void CaptureNormalState()
        {
            if (!IsConfigured || (normalRotations != null && normalRotations.Length == necks.Length)) return;
            for (int i = 0; i < necks.Length; i++)
                if (necks[i] == null || localFacingDirections[i].sqrMagnitude < .001f) return;
            normalRotations = new Quaternion[necks.Length];
            for (int i = 0; i < necks.Length; i++) normalRotations[i] = necks[i].localRotation;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (normalRotations == null) Debug.LogError("CABARET_WATCHERS_CONFIGURATION_INVALID", this);
        }

        private void LateUpdate() => Advance(Time.deltaTime);

        public void Advance(float seconds)
        {
            if (!IsActive || !IsConfigured || normalRotations == null || normalRotations.Length != necks.Length || seconds <= 0f) return;
            var roomPoint = roomVolume.transform.InverseTransformPoint(player.position);
            if (!new Bounds(roomVolume.center, roomVolume.size).Contains(roomPoint)) return;
            for (int i = 0; i < necks.Length; i++) Turn(i, seconds);
        }

        private void Turn(int index, float seconds)
        {
            var neck = necks[index];
            if (neck == null || !neck.gameObject.activeInHierarchy) return;
            var direction = player.position - neck.position;
            if (direction.sqrMagnitude < .001f) return;
            var normal = neck.parent != null ? neck.parent.rotation * normalRotations[index] : normalRotations[index];
            var facing = normal * localFacingDirections[index]; facing.y = 0f;
            var flat = direction; flat.y = 0f;
            if (flat.sqrMagnitude < .001f || facing.sqrMagnitude < .001f) return;
            float yaw = Mathf.Clamp(Vector3.SignedAngle(facing, flat, Vector3.up), -maxYaw, maxYaw);
            var yawRotation = Quaternion.AngleAxis(yaw, Vector3.up);
            var aimed = yawRotation * facing.normalized;
            float pitch = Mathf.Clamp(Mathf.Atan2(direction.y, flat.magnitude) * Mathf.Rad2Deg, -maxPitch, maxPitch);
            var pitchRotation = Quaternion.AngleAxis(-pitch, Vector3.Cross(Vector3.up, aimed));
            var target = pitchRotation * yawRotation * normal;
            neck.rotation = Quaternion.RotateTowards(neck.rotation, target, turnSpeed * seconds);
        }

        protected override void OnResetAnomaly()
        {
            if (normalRotations == null || necks == null || normalRotations.Length != necks.Length) return;
            for (int i = 0; i < necks.Length; i++)
                if (necks[i] != null) necks[i].localRotation = normalRotations[i];
        }

        private void OnDisable() => ResetAnomaly();
    }
}
