using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class SpeedAnimationAnomaly : AnomalyBehaviour
    {
        [Header("NPC & Animator References")]
        [Tooltip("Animator của NPC có animation chạy. Nếu để trống, code sẽ tự động tìm kiếm trên chính GameObject này hoặc các con của nó.")]
        [SerializeField] private Animator npcAnimator;
        [Tooltip("GameObject hoặc Transform gốc của NPC (tùy chọn, tự lấy từ Animator nếu để trống).")]
        [SerializeField] private GameObject npcObject;
        [Tooltip("Nếu tích chọn, NPC sẽ bị ẩn trong phòng bình thường và chỉ hiện khi Anomaly kích hoạt. Mặc định false (NPC chạy bình thường ở phòng thường và chạy siêu tốc ở phòng Anomaly).")]
        [SerializeField] private bool hideInNormalState = false;

        [Header("Animation Speed Settings")]
        [Tooltip("Hệ số tăng tốc animation chạy khi Anomaly kích hoạt (gấp x lần). Mặc định là 3.0x (nhanh gấp 3 lần).")]
        [SerializeField, Min(1.1f)] private float speedMultiplier = 3.0f;
        [Tooltip("Tốc độ animation ở trạng thái bình thường (mặc định là 1.0).")]
        [SerializeField, Min(0.1f)] private float normalAnimSpeed = 1.0f;

        [Header("Animation State & Parameters (Tùy chọn)")]
        [Tooltip("Tên State animation chạy trong Animator cần phát (ví dụ 'Run'). Nếu để trống, code chỉ điều chỉnh tốc độ của animation đang phát mà không ép đổi State.")]
        [SerializeField] private string runStateName = "Run";
        [Tooltip("Tên tham số bool chuyển trạng thái chạy trong Animator (nếu có, ví dụ 'IsRunning'). Để trống nếu không dùng.")]
        [SerializeField] private string isRunningBoolParam = "";
        [Tooltip("Tên tham số float tốc độ trong Animator (nếu có, ví dụ 'MoveSpeed'). Để trống nếu không dùng.")]
        [SerializeField] private string moveSpeedFloatParam = "";
        [Tooltip("Giá trị float gán cho tham số MoveSpeed khi chạy (mặc định 1.0).")]
        [SerializeField] private float moveSpeedValue = 1.0f;

        private bool baselineCaptured;
        private bool npcInitialActive = true;
        private float originalAnimatorSpeed = 1.0f;

        public void Configure(Animator targetAnimator, float multiplier = 3.0f, float normalSpeed = 1.0f)
        {
            npcAnimator = targetAnimator;
            speedMultiplier = multiplier;
            normalAnimSpeed = normalSpeed;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            ResolveReferences();
            if (baselineCaptured) return;

            if (npcObject != null)
            {
                npcInitialActive = npcObject.activeSelf;
            }

            if (npcAnimator != null)
            {
                originalAnimatorSpeed = normalAnimSpeed > 0f ? normalAnimSpeed : npcAnimator.speed;
            }

            baselineCaptured = true;

            if (hideInNormalState && npcObject != null)
            {
                npcObject.SetActive(false);
            }
            else
            {
                ApplyNormalSpeed();
            }
        }

        protected override void OnActivate()
        {
            ResolveReferences();

            if (npcObject != null)
            {
                npcObject.SetActive(true);
            }

            ApplyFastSpeed();
            Debug.Log($"SPEED_ANIMATION_ACTIVATED: anomaly={name}, speedMultiplier={speedMultiplier}x (speed={normalAnimSpeed * speedMultiplier})", this);
        }

        protected override void OnResetAnomaly()
        {
            if (npcObject != null)
            {
                npcObject.SetActive(!hideInNormalState && npcInitialActive);
            }

            ApplyNormalSpeed();
        }

        private void OnDisable()
        {
            OnResetAnomaly();
        }

        private void ApplyFastSpeed()
        {
            if (npcAnimator == null) return;

            // Đảm bảo Animator được bật
            npcAnimator.enabled = true;

            // Thiết lập State chạy nếu có chỉ định
            TriggerRunState();

            // Tăng tốc độ animation lên gấp x lần
            npcAnimator.speed = normalAnimSpeed * speedMultiplier;
        }

        private void ApplyNormalSpeed()
        {
            if (npcAnimator == null) return;

            // Đảm bảo Animator được bật
            npcAnimator.enabled = true;

            // Thiết lập State chạy nếu có chỉ định
            TriggerRunState();

            // Khôi phục tốc độ animation về trạng thái bình thường (1x)
            npcAnimator.speed = normalAnimSpeed;
        }

        private void TriggerRunState()
        {
            if (npcAnimator == null) return;

            // Kích hoạt tham số bool nếu có
            if (!string.IsNullOrEmpty(isRunningBoolParam))
            {
                npcAnimator.SetBool(isRunningBoolParam, true);
            }

            // Kích hoạt tham số float MoveSpeed nếu có
            if (!string.IsNullOrEmpty(moveSpeedFloatParam))
            {
                npcAnimator.SetFloat(moveSpeedFloatParam, moveSpeedValue);
            }

            // Play state Run nếu có
            if (!string.IsNullOrEmpty(runStateName) && npcAnimator.isActiveAndEnabled)
            {
                int stateHash = Animator.StringToHash(runStateName);
                if (npcAnimator.HasState(0, stateHash))
                {
                    npcAnimator.Play(stateHash, 0);
                }
            }
        }

        public void ResolveReferences()
        {
            if (npcAnimator == null)
            {
                npcAnimator = GetComponentInChildren<Animator>(true);
            }

            if (npcObject == null && npcAnimator != null)
            {
                npcObject = npcAnimator.gameObject;
            }
        }

        [ContextMenu("Debug Trigger Fast Run (x Multiplier)")]
        public void DebugTriggerFastRun()
        {
            OnActivate();
        }

        [ContextMenu("Debug Reset Normal Run (1x)")]
        public void DebugResetNormalRun()
        {
            OnResetAnomaly();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
        }
#endif
    }
}
