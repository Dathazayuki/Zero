using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArchiveBodyDrawersAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform observer;
        [SerializeField] private Transform[] drawers;
        [SerializeField] private GameObject[] contents;
        [SerializeField, Min(0.1f)] private float triggerDistance = 2.4f;
        [SerializeField, Min(0.1f)] private float openSeconds = 0.8f;
        [SerializeField, Min(0.1f)] private float intervalSeconds = 1f;
        [SerializeField] private Vector3 worldOpenOffset = Vector3.left * 0.53f;
        [SerializeField] private float[] extensionMultipliers;
        private Vector3[] normalPositions;
        private Vector3[] localOffsets;
        private ArchiveDrawerSequence sequence;

        public int OpenedCount => sequence == null ? 0 : sequence.NextIndex;
        public bool IsConfigured => observer != null && drawers != null && contents != null
            && drawers.Length > 0 && drawers.Length == contents.Length;

        public void Configure(Transform player, Transform[] targets, GameObject[] parts)
        {
            observer = player;
            drawers = targets;
            contents = parts;
        }

        public override void CaptureNormalState()
        {
            if (normalPositions != null || !IsConfigured) return;
            normalPositions = new Vector3[drawers.Length];
            localOffsets = new Vector3[drawers.Length];
            for (int i = 0; i < drawers.Length; i++)
            {
                if (drawers[i] == null || contents[i] == null)
                {
                    normalPositions = null;
                    Debug.LogError("IR3_DRAWERS_CONFIGURATION_INVALID: missing drawer or body part", this);
                    return;
                }
                normalPositions[i] = drawers[i].localPosition;
                float extension = extensionMultipliers != null && i < extensionMultipliers.Length
                    ? Mathf.Max(0.1f, extensionMultipliers[i]) : 1f;
                localOffsets[i] = drawers[i].parent.InverseTransformVector(worldOpenOffset * extension);
            }
            sequence = new ArchiveDrawerSequence(drawers.Length, Mathf.Max(0.1f, openSeconds), Mathf.Max(0.1f, intervalSeconds));
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            sequence?.Reset();
        }

        private void Update()
        {
            if (!IsActive || !IsConfigured || sequence == null || Time.deltaTime <= 0f) return;
            bool near = NextIsNear();
            sequence.Advance(Time.deltaTime, near);
            for (int i = 0; i < sequence.NextIndex; i++) Present(i);
        }

        private bool NextIsNear()
        {
            int next = sequence.NextIndex;
            if (next >= drawers.Length || drawers[next] == null) return false;
            Vector3 offset = observer.position - drawers[next].parent.TransformPoint(normalPositions[next]);
            offset.y = 0f;
            return offset.sqrMagnitude <= triggerDistance * triggerDistance;
        }

        private void Present(int index)
        {
            if (drawers[index] == null || contents[index] == null) return;
            float t = sequence.Progress(index);
            float eased = t * t * (3f - 2f * t);
            drawers[index].localPosition = normalPositions[index] + localOffsets[index] * eased;
            contents[index].SetActive(true);
        }

        protected override void OnResetAnomaly()
        {
            sequence?.Reset();
            if (contents == null) return;
            for (int i = 0; i < contents.Length; i++)
            {
                if (contents[i] != null) contents[i].SetActive(false);
                if (normalPositions != null && i < normalPositions.Length && drawers[i] != null)
                    drawers[i].localPosition = normalPositions[i];
            }
        }

        private void OnDisable() => ResetAnomaly();
    }
}
