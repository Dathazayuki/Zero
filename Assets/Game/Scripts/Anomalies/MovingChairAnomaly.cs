using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class MovingChairAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform chair;
        [SerializeField] private Vector3 localOffset = new Vector3(-1.4f, 0f, 1.1f);
        [SerializeField] private Vector3 rotationOffset = new Vector3(0f, 28f, 0f);

        private Vector3 normalLocalPosition;
        private Quaternion normalLocalRotation;

        public void Configure(Transform chairTransform, Vector3 offset, Vector3 eulerOffset)
        {
            chair = chairTransform;
            localOffset = offset;
            rotationOffset = eulerOffset;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            if (chair == null)
            {
                return;
            }

            normalLocalPosition = chair.localPosition;
            normalLocalRotation = chair.localRotation;
        }

        protected override void OnActivate()
        {
            if (chair == null)
            {
                return;
            }

            chair.localPosition = normalLocalPosition + localOffset;
            chair.localRotation = normalLocalRotation * Quaternion.Euler(rotationOffset);
        }

        protected override void OnResetAnomaly()
        {
            if (chair == null)
            {
                return;
            }

            chair.localPosition = normalLocalPosition;
            chair.localRotation = normalLocalRotation;
        }

    }
}
