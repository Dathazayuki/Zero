using UnityEngine;
using Zero.AnomalyPrototype.Flow;

namespace Zero.AnomalyPrototype.Anomalies
{
    public enum DeathMotionStyle { Devour, Suffocation, FreezeBlackout }

    public sealed class AnomalyDeathPresentation : MonoBehaviour
    {
        [Header("Death Presentation Style")]
        [Tooltip("Chọn kiểu chết: Style1 (Shake + Zoom + Máu) hoặc Style2 (Bất động vài giây + Tắt phụt camera).")]
        [SerializeField] private AnomalyDeathStyle deathStyle = AnomalyDeathStyle.Style1_JumpscareBlood;
        public AnomalyDeathStyle DeathStyle => deathStyle;
        public void SetDeathStyle(AnomalyDeathStyle style)
        {
            deathStyle = style;
            if (style == AnomalyDeathStyle.Style2_FreezeBlackout)
            {
                motionStyle = DeathMotionStyle.FreezeBlackout;
            }
            else if (motionStyle == DeathMotionStyle.FreezeBlackout)
            {
                motionStyle = DeathMotionStyle.Devour;
            }
        }

        [Header("Motion Style (Style 1 details)")]
        [SerializeField] private DeathMotionStyle motionStyle = DeathMotionStyle.Devour;
        public DeathMotionStyle MotionStyle => motionStyle;
        public void SetMotionStyle(DeathMotionStyle style)
        {
            motionStyle = style;
            if (style == DeathMotionStyle.FreezeBlackout)
            {
                deathStyle = AnomalyDeathStyle.Style2_FreezeBlackout;
            }
            else
            {
                deathStyle = AnomalyDeathStyle.Style1_JumpscareBlood;
            }
        }

        [Header("Freeze death settings (Style 2)")]
        [SerializeField, Min(0f)] private float freezeDuration = 1f;
        [SerializeField] private AudioClip cameraCutAudioClip;

        public float FreezeDuration => freezeDuration;
        public void SetFreezeDuration(float seconds) => freezeDuration = Mathf.Max(0f, seconds);
        public AudioClip CameraCutAudioClip => cameraCutAudioClip;

        private void Awake()
        {
            cameraCutAudioClip = GameAudioBootstrap.ResolveDefaultClip(cameraCutAudioClip, "clip.LightGlitch");
        }

        [SerializeField] private Transform focusTarget;
        [SerializeField] private Transform jumpscareCameraAnchor;
        [SerializeField] private Animator animator;
        [SerializeField] private string jumpscareStateName;
        [SerializeField] private AudioClip jumpscareAudioClip;

        public Transform FocusTarget => focusTarget;
        public void SetFocusTarget(Transform target) => focusTarget = target;
        public Transform JumpscareCameraAnchor => jumpscareCameraAnchor;
        public AudioClip JumpscareAudioClip => jumpscareAudioClip;
        public bool HasCameraPose => jumpscareCameraAnchor != null;

        public void PlayJumpscareAnimation()
        {
            if (animator == null || string.IsNullOrWhiteSpace(jumpscareStateName))
            {
                return;
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.CrossFadeInFixedTime(jumpscareStateName, 0.05f, 0, 0f);
        }

        public bool TryGetCameraPose(out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = default;
            if (jumpscareCameraAnchor == null)
            {
                return false;
            }

            position = jumpscareCameraAnchor.position;
            if (focusTarget != null)
            {
                Vector3 direction = focusTarget.position - position;
                rotation = direction.sqrMagnitude > 0.0001f
                    ? Quaternion.LookRotation(direction.normalized, Vector3.up)
                    : jumpscareCameraAnchor.rotation;
            }
            else
            {
                rotation = jumpscareCameraAnchor.rotation;
            }

            return true;
        }

        public bool TryGetDevourTarget(out Vector3 position)
        {
            position = default;
            if (focusTarget == null)
            {
                return false;
            }

            position = focusTarget.position;
            return true;
        }
    }
}
