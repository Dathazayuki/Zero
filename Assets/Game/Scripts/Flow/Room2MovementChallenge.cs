using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class Room2MovementChallenge : MonoBehaviour
    {
        [SerializeField] private TrueEndingGameplayController flow;
        [SerializeField] private Room2LightController lightController;
        [SerializeField, Min(0f)] private float graceSeconds = 0.18f;
        [SerializeField, Min(0f)] private float speedThreshold = 0.10f;
        [SerializeField, Min(0.01f)] private float confirmationSeconds = 0.08f;
        private FirstPersonController player;
        private Vector3 previousPosition;
        private float movingTime;
        private bool running;
        public bool IsConfigured => lightController != null;
        public void Begin(FirstPersonController target, bool retry)
        {
            player = target;
            Rebase();
            running = true;
            lightController.Begin(retry);
        }
        public void Rebase() { if (player != null) previousPosition = player.transform.position; movingTime = 0f; }
        public void StopChallenge() { running = false; movingTime = 0f; if (lightController != null) lightController.StopCycle(); }
        public static float HorizontalSpeed(Vector3 displacement, float deltaTime) => deltaTime > 0f ? Vector3.ProjectOnPlane(displacement, Vector3.up).magnitude / deltaTime : 0f;
        private void LateUpdate()
        {
            if (!running || player == null) return;
            Vector3 now = player.transform.position;
            float speed = HorizontalSpeed(now - previousPosition, Time.deltaTime);
            previousPosition = now;
            if (!player.InputEnabled || !lightController.IsPunishing || lightController.OnElapsed < graceSeconds) { movingTime = 0f; return; }
            movingTime = speed > speedThreshold ? movingTime + Time.deltaTime : 0f;
            if (movingTime >= confirmationSeconds) flow.RequestReset(TrueEndingPhase.Room2);
        }
        private void OnDisable() => StopChallenge();
    }
}
