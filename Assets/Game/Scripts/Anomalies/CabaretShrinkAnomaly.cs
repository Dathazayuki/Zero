using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretShrinkAnomaly : AnomalyBehaviour
    {
        [SerializeField] private CharacterController playerBody;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private BoxCollider roomArea;
        [SerializeField] private BoxCollider stageArea;
        [SerializeField, Range(.1f, 1f)] private float minimumScale = .4f;
        [SerializeField, Min(.1f)] private float fullSizeDistance = 10.5f;
        [SerializeField, Min(.01f)] private float scaleChangeSpeed = .7f;
        [SerializeField] private LayerMask obstructionMask = 1;
        private readonly Collider[] overlaps = new Collider[16];
        private Vector3 originalScale;
        private bool captured;
        private float factor = 1f;
        public float CurrentFactor => factor;
        public bool IsConfigured => playerBody != null && roomArea != null && stageArea != null;

        public override void CaptureNormalState()
        {
            if (captured || playerBody == null) return;
            originalScale = playerBody.transform.localScale;
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!IsConfigured) Debug.LogError("CABARET_SHRINK_CONFIGURATION_INVALID", this);
        }

        private void LateUpdate()
        {
            if (!IsActive || !captured || !IsConfigured || Time.deltaTime <= 0f) return;
            if (death != null && death.IsDeathInProgress) { RestoreScale(); return; }
            Vector3 position = playerBody.transform.position;
            if (!roomArea.bounds.Contains(position)) { RestoreScale(); return; }
            Vector3 offset = position - stageArea.bounds.ClosestPoint(position);
            offset.y = 0f;
            float target = CabaretShrinkRules.Evaluate(offset.magnitude, fullSizeDistance, minimumScale);
            float next = Mathf.MoveTowards(factor, target, scaleChangeSpeed * Time.deltaTime);
            if (Mathf.Approximately(next, factor)) return;
            if (next > factor && !CanGrow(next)) return;
            factor = next;
            playerBody.transform.localScale = originalScale * factor;
        }

        private bool CanGrow(float next)
        {
            Transform root = playerBody.transform;
            float ratio = next / factor;
            Vector3 scale = root.lossyScale * ratio;
            Vector3 centre = root.position + (root.TransformPoint(playerBody.center) - root.position) * ratio;
            float radius = playerBody.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float halfHeight = Mathf.Max(radius, playerBody.height * Mathf.Abs(scale.y) * .5f);
            Vector3 axis = root.up * (halfHeight - radius);
            int count = Physics.OverlapCapsuleNonAlloc(centre + axis, centre - axis,
                Mathf.Max(.01f, radius - .01f), overlaps, obstructionMask, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) return false;
            for (int i = 0; i < count; i++)
                if (!overlaps[i].transform.IsChildOf(root)) return false;
            return true;
        }

        private void RestoreScale()
        {
            if (captured && playerBody != null) playerBody.transform.localScale = originalScale;
            factor = 1f;
        }

        public override void OnPlayerDeathAccepted() => RestoreScale();
        protected override void OnResetAnomaly() => RestoreScale();
        private void OnDisable() => ResetAnomaly();
    }
}
