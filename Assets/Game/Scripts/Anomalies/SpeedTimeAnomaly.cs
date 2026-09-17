using UnityEngine;
using Zero.EnvironmentLoop;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class SpeedTimeAnomaly : AnomalyBehaviour
    {
        [Header("Clock Hand References")]
        [Tooltip("Transform kim giờ của đồng hồ.")]
        [SerializeField] private Transform hourHand;

        [Tooltip("Transform kim giây của đồng hồ.")]
        [SerializeField] private Transform secondHand;

        [Tooltip("Transform kim phút của đồng hồ (tùy chọn).")]
        [SerializeField] private Transform minuteHand;

        [Tooltip("GameObject đồng hồ (dùng để tự tìm kiếm các kim nếu chưa gán).")]
        [SerializeField] private GameObject clockRoot;

        [Header("Rotation Settings")]
        [Tooltip("Trục xoay của các kim đồng hồ (mặc định trục Z / forward theo local space).")]
        [SerializeField] private Vector3 rotationAxis = new Vector3(0f, 0f, 1f);

        [Tooltip("Tốc độ quay kim giây (độ / giây). Ví dụ: 2160 deg/s = 6 vòng / giây.")]
        [SerializeField, Min(0f)] private float secondHandSpeed = 2160f;

        [Tooltip("Tốc độ quay kim giờ (độ / giây). Ví dụ: 360 deg/s = 1 vòng / giây.")]
        [SerializeField, Min(0f)] private float hourHandSpeed = 360f;

        [Tooltip("Tốc độ quay kim phút nếu có (độ / giây). Ví dụ: 720 deg/s = 2 vòng / giây.")]
        [SerializeField, Min(0f)] private float minuteHandSpeed = 720f;

        [Tooltip("Quay theo chiều kim đồng hồ (true) hay ngược chiều (false).")]
        [SerializeField] private bool rotateClockwise = true;

        [Header("Optional Audio")]
        [Tooltip("AudioSource phát tiếng tíc tắc siêu tốc (tùy chọn).")]
        [SerializeField] private AudioSource fastTickAudio;

        private Quaternion normalHourRotation;
        private Quaternion normalSecondRotation;
        private Quaternion normalMinuteRotation;
        private bool baselineCaptured;

        public Transform HourHand => hourHand;
        public Transform SecondHand => secondHand;
        public Transform MinuteHand => minuteHand;

        public void Configure(
            Transform hour,
            Transform second,
            Transform minute = null,
            Vector3? axis = null,
            AnomalyData anomalyData = null)
        {
            hourHand = hour;
            secondHand = second;
            minuteHand = minute;
            if (axis.HasValue) rotationAxis = axis.Value;
            if (anomalyData != null) Initialize(anomalyData);
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            ResolveReferences();

            if (hourHand != null) normalHourRotation = hourHand.localRotation;
            if (secondHand != null) normalSecondRotation = secondHand.localRotation;
            if (minuteHand != null) normalMinuteRotation = minuteHand.localRotation;

            baselineCaptured = true;
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }

            float directionSign = rotateClockwise ? -1f : 1f;
            float dt = Time.deltaTime;

            if (secondHand != null)
            {
                secondHand.Rotate(rotationAxis, directionSign * secondHandSpeed * dt, Space.Self);
            }

            if (hourHand != null)
            {
                hourHand.Rotate(rotationAxis, directionSign * hourHandSpeed * dt, Space.Self);
            }

            if (minuteHand != null)
            {
                minuteHand.Rotate(rotationAxis, directionSign * minuteHandSpeed * dt, Space.Self);
            }
        }

        protected override void OnActivate()
        {
            ResolveReferences();
            if (!baselineCaptured)
            {
                CaptureNormalState();
            }

            if (hourHand == null && secondHand == null)
            {
                Debug.LogWarning($"SPEED_TIME_NO_HANDS: Chưa gán kim giờ hoặc kim giây cho {name}!", this);
                return;
            }

            if (fastTickAudio != null)
            {
                fastTickAudio.Play();
            }

            Debug.Log($"SPEED_TIME_ACTIVATED: anomaly={name}, hourSpeed={hourHandSpeed}, secondSpeed={secondHandSpeed}", this);
        }

        protected override void OnResetAnomaly()
        {
            if (fastTickAudio != null && fastTickAudio.isPlaying)
            {
                fastTickAudio.Stop();
            }

            if (!baselineCaptured)
            {
                return;
            }

            if (hourHand != null) hourHand.localRotation = normalHourRotation;
            if (secondHand != null) secondHand.localRotation = normalSecondRotation;
            if (minuteHand != null) minuteHand.localRotation = normalMinuteRotation;
        }

        private void OnDisable()
        {
            OnResetAnomaly();
        }

        private void ResolveReferences()
        {
            if (clockRoot == null)
            {
                var missingClock = GetComponentInParent<MissingClockAnomaly>() ?? FindAnyObjectByType<MissingClockAnomaly>();
                if (missingClock != null && missingClock.ClockObject != null)
                {
                    clockRoot = missingClock.ClockObject;
                }
            }

            Transform searchRoot = clockRoot != null ? clockRoot.transform : transform;

            if (hourHand == null)
            {
                hourHand = FindHandByName(searchRoot, "hour");
            }

            if (secondHand == null)
            {
                secondHand = FindHandByName(searchRoot, "second", "sec");
            }

            if (minuteHand == null)
            {
                minuteHand = FindHandByName(searchRoot, "minute", "min");
            }
        }

        private static Transform FindHandByName(Transform root, params string[] keywords)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                string lower = all[i].name.ToLowerInvariant();
                for (int k = 0; k < keywords.Length; k++)
                {
                    if (lower.Contains(keywords[k]))
                    {
                        return all[i];
                    }
                }
            }
            return null;
        }
    }
}
