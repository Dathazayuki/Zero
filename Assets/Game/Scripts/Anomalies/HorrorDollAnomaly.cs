using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class HorrorDollAnomaly : AnomalyBehaviour
    {
        [Header("Doll & Bone References")]
        [Tooltip("Khúc xương đầu của búp bê (bone 16). Kéo trực tiếp bone 16 vào đây.")]
        [SerializeField] private Transform headBone;
        [Tooltip("GameObject gốc của búp bê (tùy chọn, để bật/tắt). Nếu để trống sẽ dùng chính GameObject này.")]
        [SerializeField] private GameObject dollObject;
        [Tooltip("Nếu tích chọn, búp bê sẽ bị ẩn trong phòng bình thường và chỉ xuất hiện khi Anomaly kích hoạt. Mặc định true.")]
        [SerializeField] private bool hideInNormalState = true;

        [Header("Head Tracking Settings")]
        [Tooltip("Tốc độ quay đầu (độ/giây). Mặc định 180 độ/giây xoay mượt mà. Nếu đặt <= 0 đầu sẽ quay bám sát tức thì không có độ trễ.")]
        [SerializeField, Min(0f)] private float turnSpeed = 180f;
        [Tooltip("Bù trừ góc xoay (Euler angles) nếu trục bone đầu bị lệch (ví dụ nếu đầu quay sang ngang, cúi gập hoặc ngửa lên thì chỉnh trục X, Y, Z tại đây). Mặc định (0, 0, 0).")]
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;
        [Tooltip("Khoảng cách tối đa người chơi đứng gần để búp bê bắt đầu quay đầu nhìn (mét).")]
        [SerializeField, Min(1f)] private float maxTrackingDistance = 25f;

        [Header("Rotation Limits")]
        [Tooltip("Cho phép đầu quay 360 độ quanh cổ kiểu búp bê kinh dị (không giới hạn góc nhìn). Mặc định true.")]
        [SerializeField] private bool full360Rotation = true;
        [Tooltip("Góc quay tối đa so với hướng trước mặt của thân búp bê nếu tắt chế độ 360 độ (độ).")]
        [SerializeField, Range(30f, 180f)] private float maxAngleFromForward = 90f;

        [Header("Audio (Tùy chọn)")]
        [Tooltip("Âm thanh vặn cổ rắc rắc khi búp bê quay đầu (tùy chọn).")]
        [SerializeField] private AudioClip neckCreakAudio;
        [SerializeField] private AudioSource audioSource;
        [Tooltip("Khoảng thời gian tối thiểu giữa các lần phát tiếng vặn cổ (giây).")]
        [SerializeField, Min(0.1f)] private float soundInterval = 0.6f;

        private Transform player;
        private Camera playerCamera;
        private bool baselineCaptured;
        private bool dollInitialActive = true;
        private Vector3 initialHeadLocalPosition;
        private Quaternion initialHeadLocalRotation;
        private Vector3 dollForward;
        private float lastSoundTime;

        public void BindPlayer(Transform playerTransform, Camera cameraReference)
        {
            player = playerTransform;
            playerCamera = cameraReference;
        }

        public override void CaptureNormalState()
        {
            ResolveReferences();
            if (baselineCaptured) return;

            if (dollObject != null)
            {
                dollInitialActive = dollObject.activeSelf;
            }

            if (headBone != null)
            {
                initialHeadLocalPosition = headBone.localPosition;
                initialHeadLocalRotation = headBone.localRotation;
            }

            dollForward = transform.forward;
            baselineCaptured = true;

            if (hideInNormalState && dollObject != null)
            {
                dollObject.SetActive(false);
            }
            else
            {
                RestoreHeadPose();
            }
        }

        protected override void OnActivate()
        {
            ResolveReferences();

            if (dollObject != null)
            {
                dollObject.SetActive(true);
            }

            RestoreHeadPose();
            Debug.Log($"HORROR_DOLL_ACTIVATED: anomaly={name}, headBoneBound={headBone != null}", this);
        }

        protected override void OnResetAnomaly()
        {
            if (dollObject != null)
            {
                dollObject.SetActive(!hideInNormalState && dollInitialActive);
            }

            RestoreHeadPose();

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        private void OnDisable()
        {
            OnResetAnomaly();
        }

        private void LateUpdate()
        {
            // Chỉ quay đầu khi Anomaly đang kích hoạt và đã gán bone đầu
            if (!IsActive || headBone == null) return;

            // Xác định vị trí mục tiêu cần nhìn vào (mắt/Camera của Player)
            Vector3 targetPosition = GetTargetLookPosition();
            if (targetPosition == Vector3.zero) return;

            // Kiểm tra khoảng cách
            float distance = Vector3.Distance(headBone.position, targetPosition);
            if (distance > maxTrackingDistance) return;

            Vector3 directionToPlayer = (targetPosition - headBone.position).normalized;
            if (directionToPlayer.sqrMagnitude < 0.0001f) return;

            // Nếu không bật 360 độ thì kiểm tra giới hạn góc nhìn so với hướng trước mặt
            if (!full360Rotation)
            {
                float angle = Vector3.Angle(transform.forward, directionToPlayer);
                if (angle > maxAngleFromForward)
                {
                    return;
                }
            }

            // Tính toán góc quay nhìn thẳng vào Player
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer, Vector3.up);

            // Áp dụng góc xoay bù trừ (rotationOffset) nếu trục bone bị lệch
            Quaternion desiredRotation = lookRotation * Quaternion.Euler(rotationOffset);

            // Quay đầu theo tốc độ mượt mà hoặc quay tức thì
            Quaternion oldRotation = headBone.rotation;
            if (turnSpeed > 0f)
            {
                headBone.rotation = Quaternion.RotateTowards(headBone.rotation, desiredRotation, turnSpeed * Time.deltaTime);
            }
            else
            {
                headBone.rotation = desiredRotation;
            }

            // Phát âm thanh vặn cổ rắc rắc nếu đầu đang xoay một góc đáng kể
            float rotDelta = Quaternion.Angle(oldRotation, headBone.rotation);
            if (rotDelta > 0.5f && neckCreakAudio != null && audioSource != null)
            {
                if (Time.time - lastSoundTime >= soundInterval)
                {
                    audioSource.PlayOneShot(neckCreakAudio);
                    lastSoundTime = Time.time;
                }
            }
        }

        private Vector3 GetTargetLookPosition()
        {
            if (playerCamera != null)
            {
                return playerCamera.transform.position;
            }

            if (player != null)
            {
                return player.position + Vector3.up * 1.6f;
            }

            // Tự động tìm Camera.main nếu chưa được gán
            if (Camera.main != null)
            {
                playerCamera = Camera.main;
                return playerCamera.transform.position;
            }

            return Vector3.zero;
        }

        private void RestoreHeadPose()
        {
            if (headBone == null) return;

            if (baselineCaptured)
            {
                headBone.localPosition = initialHeadLocalPosition;
                headBone.localRotation = initialHeadLocalRotation;
            }
        }

        private void ResolveReferences()
        {
            if (dollObject == null)
            {
                dollObject = gameObject;
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (player == null && playerCamera != null)
            {
                player = playerCamera.transform.root;
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        [ContextMenu("Apply Look At Camera Now")]
        public void ApplyLookAtCameraNow()
        {
            if (headBone == null) return;
            Vector3 target = GetTargetLookPosition();
            if (target != Vector3.zero)
            {
                Vector3 dir = (target - headBone.position).normalized;
                headBone.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(rotationOffset);
            }
        }

        [ContextMenu("Reset Head Pose")]
        public void ResetHeadPoseContextMenu()
        {
            RestoreHeadPose();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (dollObject == null)
            {
                dollObject = gameObject;
            }
        }
#endif
    }
}
