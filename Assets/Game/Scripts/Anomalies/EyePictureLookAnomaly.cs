using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class EyePictureLookAnomaly : AnomalyBehaviour
    {
        [Header("Picture Object")]
        [Tooltip("GameObject bức tranh (khi bình thường thì ẩn, khi có anomaly thì hiện).")]
        [SerializeField] private GameObject pictureObject;

        [Header("Eye / Pupil Settings (2D Movement)")]
        [Tooltip("Một hoặc nhiều Transform của con ngươi cần trượt 2D theo Player.")]
        [SerializeField] private Transform[] eyePupils;

        [Tooltip("Transform người chơi cần dõi theo (nếu để trống script sẽ tự tìm).")]
        [SerializeField] private Transform player;

        [Tooltip("Độ lệch vị trí nhìn trên người chơi (mặc định nhìn vào tầm mắt Y = 1.6m).")]
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

        [Tooltip("Tốc độ trượt của con ngươi dõi theo người chơi.")]
        [SerializeField, Min(0.1f)] private float trackingSpeed = 8f;

        [Tooltip("Độ trượt tối đa của con ngươi theo trục local X và Y.")]
        [SerializeField] private Vector2 maxPositionOffset = new Vector2(0.05f, 0.05f);

        private Vector3[] initialPupilPositions;
        private bool normalPictureWasActive;
        private bool normalStateCaptured;

        public GameObject PictureObject => pictureObject;
        public Transform[] EyePupils => eyePupils;

        public void BindPlayer(Transform playerTransform)
        {
            player = playerTransform;
        }

        public void Configure(GameObject picture, Transform[] pupils, Transform playerTransform = null)
        {
            pictureObject = picture;
            eyePupils = pupils;
            if (playerTransform != null) player = playerTransform;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            ResolveReferences();

            if (pictureObject != null)
            {
                normalPictureWasActive = pictureObject.activeSelf;
                // Khi normal thì ẩn bức tranh đi
                pictureObject.SetActive(false);
            }

            if (eyePupils != null && eyePupils.Length > 0)
            {
                initialPupilPositions = new Vector3[eyePupils.Length];
                for (int i = 0; i < eyePupils.Length; i++)
                {
                    if (eyePupils[i] != null)
                    {
                        initialPupilPositions[i] = eyePupils[i].localPosition;
                    }
                }
            }

            normalStateCaptured = true;
        }

        private void Update()
        {
            if (!IsActive || player == null || eyePupils == null || eyePupils.Length == 0)
            {
                return;
            }

            if (pictureObject != null && !pictureObject.activeInHierarchy)
            {
                return;
            }

            Vector3 targetPoint = player.position + targetOffset;
            float dt = Time.deltaTime;

            for (int i = 0; i < eyePupils.Length; i++)
            {
                Transform pupil = eyePupils[i];
                if (pupil == null) continue;

                Vector3 toTargetWorld = (targetPoint - pupil.position).normalized;
                Transform reference = pupil.parent != null ? pupil.parent : transform;
                Vector3 localDir = reference.InverseTransformDirection(toTargetWorld);

                Vector2 clampedDir = new Vector2(
                    Mathf.Clamp(localDir.x, -1f, 1f) * maxPositionOffset.x,
                    Mathf.Clamp(localDir.y, -1f, 1f) * maxPositionOffset.y);

                Vector3 basePos = initialPupilPositions != null && i < initialPupilPositions.Length
                    ? initialPupilPositions[i]
                    : pupil.localPosition;

                Vector3 targetPos = basePos + new Vector3(clampedDir.x, clampedDir.y, 0f);
                pupil.localPosition = Vector3.Lerp(pupil.localPosition, targetPos, dt * trackingSpeed);
            }
        }

        protected override void OnActivate()
        {
            ResolveReferences();
            if (!normalStateCaptured)
            {
                CaptureNormalState();
            }

            if (pictureObject != null)
            {
                pictureObject.SetActive(true);
            }

            Debug.Log($"EYE_PICTURE_ACTIVATED: anomaly={name}", this);
        }

        protected override void OnResetAnomaly()
        {
            if (eyePupils != null)
            {
                for (int i = 0; i < eyePupils.Length; i++)
                {
                    if (eyePupils[i] == null) continue;
                    if (initialPupilPositions != null && i < initialPupilPositions.Length)
                    {
                        eyePupils[i].localPosition = initialPupilPositions[i];
                    }
                }
            }

            if (pictureObject != null)
            {
                pictureObject.SetActive(normalStateCaptured ? normalPictureWasActive : false);
            }
        }

        private void OnDisable()
        {
            OnResetAnomaly();
        }

        private void ResolveReferences()
        {
            if (pictureObject == null)
            {
                pictureObject = gameObject;
            }

            if (player == null)
            {
                player = Camera.main != null ? Camera.main.transform : null;
            }
        }
    }
}
