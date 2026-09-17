using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public enum DeathMotionStyle { Devour, Suffocation }

    public sealed class AnomalyDeathPresentation : MonoBehaviour
    {
        [SerializeField] private DeathMotionStyle motionStyle = DeathMotionStyle.Devour;
        public DeathMotionStyle MotionStyle => motionStyle;
        [SerializeField] private Transform focusTarget;
        [SerializeField] private Transform jumpscareCameraAnchor;
        [SerializeField] private Animator animator;
        [SerializeField] private string jumpscareStateName;
        [SerializeField] private AudioClip jumpscareAudioClip;

        public Transform FocusTarget => focusTarget;
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
