using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class Room1DontLookBackController : MonoBehaviour
    {
        [SerializeField] private TrueEndingGameplayController flow;
        [SerializeField] private Transform forwardReference;
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform completePoint;
        [SerializeField] private AudioSource[] baitSources = new AudioSource[3];
        [SerializeField, Range(90f, 180f)] private float failAngle = 120f;
        [SerializeField, Min(0.01f)] private float confirmationSeconds = 0.2f;
        private FirstPersonController player;
        private float backwardsTime;
        private int nextCue;
        private bool running;
        private readonly SourceSoundPlayback[] baitPlayback = new SourceSoundPlayback[3];
        public Quaternion ForwardRotation => Quaternion.LookRotation(Vector3.ProjectOnPlane(forwardReference.forward, Vector3.up).normalized);
        public bool IsConfigured => forwardReference != null && startPoint != null && completePoint != null &&
            Vector3.ProjectOnPlane(forwardReference.forward, Vector3.up).sqrMagnitude > 0.01f;

        public void Begin(FirstPersonController target)
        {
            StopChallenge();
            player = target;
            nextCue = 0;
            running = IsConfigured && player != null && player.PlayerCamera != null;
        }

        public void StopChallenge()
        {
            running = false;
            backwardsTime = 0f;
            foreach (var source in baitSources) if (source != null) source.Stop();
        }

        public static bool IsLookingBack(Vector3 view, Vector3 forward, float threshold)
        {
            view = Vector3.ProjectOnPlane(view, Vector3.up);
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);
            return view.sqrMagnitude > 0.0001f && forward.sqrMagnitude > 0.0001f && Vector3.Angle(view, forward) > threshold;
        }

        private void LateUpdate()
        {
            if (!running || !player.InputEnabled) return;
            bool backwards = IsLookingBack(player.PlayerCamera.transform.forward, forwardReference.forward, failAngle);
            backwardsTime = backwards ? backwardsTime + Time.deltaTime : 0f;
            if (backwardsTime >= confirmationSeconds)
            {
                flow.RequestReset(TrueEndingPhase.Room1);
                return;
            }
            Vector3 forward = Vector3.ProjectOnPlane(forwardReference.forward, Vector3.up).normalized;
            float length = Vector3.Dot(completePoint.position - startPoint.position, forward);
            if (length <= 0.01f) return;
            float progress = Vector3.Dot(player.transform.position - startPoint.position, forward) / length;
            while (nextCue < 3 && progress >= (nextCue + 1) * 0.25f)
            {
                if (nextCue < baitSources.Length && baitSources[nextCue] != null)
                    GameAudioBootstrap.BindSource(ref baitPlayback[nextCue], baitSources[nextCue]).Play(
                        null, baitSources[nextCue].clip, false);
                nextCue++;
            }
        }
        private void OnDisable() => StopChallenge();
    }
}
