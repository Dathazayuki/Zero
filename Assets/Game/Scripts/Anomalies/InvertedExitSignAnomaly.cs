using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class InvertedExitSignAnomaly : AnomalyBehaviour
    {
        [Header("Target Exit Sign")]
        [Tooltip("Transform của bảng Exit cần lộn ngược.")]
        [SerializeField] private Transform exitSignTransform;

        [Header("Rotation Settings")]
        [Tooltip("Góc xoay khi bị lộn ngược (mặc định lộn ngược theo trục Z là 180 độ).")]
        [SerializeField] private Vector3 rotationOffset = new Vector3(0f, 0f, 180f);

        [Tooltip("Độ lệch vị trí bổ sung nếu tâm pivot của bảng không nằm ở giữa.")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;

        [Header("Alternative: Object Swap Mode (Optional)")]
        [Tooltip("Nếu bạn có 2 GameObject riêng biệt (1 bình thường, 1 lộn ngược), kéo vào đây. Nếu để trống, script sẽ tự động xoay exitSignTransform.")]
        [SerializeField] private GameObject normalSignObject;
        [SerializeField] private GameObject invertedSignObject;

        private Vector3 normalLocalPosition;
        private Quaternion normalLocalRotation;
        private Vector3 normalLocalScale;
        private bool normalObjectWasActive;
        private bool invertedObjectWasActive;
        private bool normalStateCaptured;

        public Transform ExitSignTransform => exitSignTransform;
        public Vector3 RotationOffset => rotationOffset;
        public Vector3 PositionOffset => positionOffset;

        public void Configure(Transform signTransform, Vector3 rotOffset, Vector3 posOffset)
        {
            exitSignTransform = signTransform;
            rotationOffset = rotOffset;
            positionOffset = posOffset;
            CaptureNormalState();
        }

        public void Configure(GameObject normalObj, GameObject invertedObj)
        {
            normalSignObject = normalObj;
            invertedSignObject = invertedObj;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            if (exitSignTransform != null)
            {
                normalLocalPosition = exitSignTransform.localPosition;
                normalLocalRotation = exitSignTransform.localRotation;
                normalLocalScale = exitSignTransform.localScale;
            }

            if (normalSignObject != null)
            {
                normalObjectWasActive = normalSignObject.activeSelf;
            }

            if (invertedSignObject != null)
            {
                invertedObjectWasActive = invertedSignObject.activeSelf;
            }

            normalStateCaptured = true;
        }

        protected override void OnActivate()
        {
            // Chế độ đổi GameObject (nếu được thiết lập)
            if (normalSignObject != null && invertedSignObject != null)
            {
                normalSignObject.SetActive(false);
                invertedSignObject.SetActive(true);
                return;
            }

            // Chế độ xoay Transform
            if (exitSignTransform != null)
            {
                exitSignTransform.localPosition = normalLocalPosition + positionOffset;
                exitSignTransform.localRotation = normalLocalRotation * Quaternion.Euler(rotationOffset);
            }
            else
            {
                Debug.LogWarning($"INVERTED_EXIT_SIGN_INVALID: Chưa gán Exit Sign Transform trên {name}", this);
            }
        }

        protected override void OnResetAnomaly()
        {
            if (normalSignObject != null && invertedSignObject != null)
            {
                normalSignObject.SetActive(normalStateCaptured ? normalObjectWasActive : true);
                invertedSignObject.SetActive(normalStateCaptured ? invertedObjectWasActive : false);
                return;
            }

            if (exitSignTransform != null && normalStateCaptured)
            {
                exitSignTransform.localPosition = normalLocalPosition;
                exitSignTransform.localRotation = normalLocalRotation;
                exitSignTransform.localScale = normalLocalScale;
            }
        }
    }
}
