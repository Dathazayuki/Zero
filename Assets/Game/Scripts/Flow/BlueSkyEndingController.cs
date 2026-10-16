using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class BlueSkyEndingController : MonoBehaviour
    {
        [SerializeField] private Transform stairsStartMarker;
        [SerializeField] private Transform poseMarker;
        [SerializeField] private CinemachineCamera endingCamera;
        [SerializeField] private Canvas endCanvas;
        [SerializeField] private string walkState = "Walk";
        [SerializeField] private string presentationState = "CelebrateBow";
        [SerializeField, Min(0.01f)] private float enterFadeSeconds = 0.4f;
        [SerializeField, Min(1f)] private float walkSeconds = 14.5f;
        [SerializeField, Min(0.05f)] private float pauseBeforeBowSeconds = 0.5f;
        [SerializeField, Min(0.01f)] private float animationSeconds = 3.75f;
        [SerializeField, Min(0f)] private float holdSeconds = 1.5f;
        [SerializeField, Min(0.1f)] private float whiteFadeSeconds = 1.5f;

        private bool running;
        private bool finished;
        private PlayerLocomotionAnimator locomotion;
        private SceneFadeController fadeController;
        private CinemachineBrain brain;
        private CinemachineBlendDefinition previousBlend;
        private bool brainWasEnabled;
        private int previousPriority;
        private Camera physicalCamera;
        private int previousMask;
        private Vector3 cameraLocalPosition;
        private Quaternion cameraLocalRotation;
        private Quaternion endingCameraOriginalRotation;
        private float cameraFov;

        public bool IsConfigured => poseMarker != null && endingCamera != null;
        public float EnterFadeSeconds => enterFadeSeconds;
        public float WalkSeconds => walkSeconds;
        public float AnimationSeconds => animationSeconds;
        public float TotalCutsceneSeconds => enterFadeSeconds * 2f + walkSeconds + pauseBeforeBowSeconds + animationSeconds + holdSeconds + whiteFadeSeconds;

        private void Awake()
        {
            if (endCanvas != null) endCanvas.gameObject.SetActive(false);
            if (endingCamera != null) endingCamera.enabled = false;
        }

        public void Begin(FirstPersonController player, TrueEndingGameplayController flow, SceneFadeController fade)
        {
            if (running) return;
            running = true;
            StartCoroutine(Sequence(player, flow, fade));
        }

        private IEnumerator Sequence(FirstPersonController player, TrueEndingGameplayController flow, SceneFadeController fade)
        {
            fadeController = fade;
            if (fade != null && enterFadeSeconds > 0f)
            {
                yield return fade.FadeToColor(Color.black, 1f, enterFadeSeconds);
                yield return null;
            }

            Vector3 startPos = stairsStartMarker != null
                ? stairsStartMarker.position
                : (poseMarker != null ? new Vector3(poseMarker.position.x, 0f, 96.0f) : player.transform.position);
            Vector3 endPos = poseMarker.position;

            SetupStairsCutscene(player, startPos, endPos);
            yield return null;

            if (fade != null && enterFadeSeconds > 0f)
                yield return fade.FadeTo(0f, enterFadeSeconds);

            locomotion = player.GetComponent<PlayerLocomotionAnimator>();
            if (locomotion != null && !locomotion.TryPlayPresentationState(walkState, 0.2f))
                Debug.LogError($"TRUE_ENDING_ANIMATION_MISSING: {walkState}", this);

            yield return WalkUpStairs(player, startPos, endPos, walkSeconds);
            yield return TurnToCamera(player, endPos, pauseBeforeBowSeconds);

            if (locomotion != null && !locomotion.TryPlayPresentationState(presentationState, 0.15f))
                Debug.LogError($"TRUE_ENDING_ANIMATION_MISSING: {presentationState}", this);

            yield return new WaitForSeconds(animationSeconds + holdSeconds);

            if (fade != null) yield return fade.FadeToColor(Color.white, 1f, whiteFadeSeconds);
            flow.Finish();
            finished = true;
            Debug.Log("TRUE_ENDING_FINISHED", this);
        }

        private void SetupStairsCutscene(FirstPersonController player, Vector3 startPos, Vector3 endPos)
        {
            physicalCamera = player.PlayerCamera;
            cameraLocalPosition = physicalCamera.transform.localPosition;
            cameraLocalRotation = physicalCamera.transform.localRotation;
            cameraFov = physicalCamera.fieldOfView;
            previousMask = physicalCamera.cullingMask;

            Vector3 walkDir = Vector3.ProjectOnPlane(endPos - startPos, Vector3.up);
            Quaternion walkRot = walkDir.sqrMagnitude > 0.001f ? Quaternion.LookRotation(walkDir) : Quaternion.identity;
            player.Teleport(startPos, walkRot);
            Physics.SyncTransforms();

            foreach (var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>())
                physicalCamera.cullingMask |= 1 << renderer.gameObject.layer;

            endingCameraOriginalRotation = endingCamera.transform.rotation;
            Vector3 lookTarget = startPos + Vector3.up * 1.1f;
            endingCamera.transform.rotation = Quaternion.LookRotation(lookTarget - endingCamera.transform.position);

            brain = physicalCamera.GetComponent<CinemachineBrain>();
            if (brain == null) brain = physicalCamera.gameObject.AddComponent<CinemachineBrain>();
            brainWasEnabled = brain.enabled;
            previousBlend = brain.DefaultBlend;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);

            previousPriority = endingCamera.Priority;
            endingCamera.Priority = 1001;
            endingCamera.enabled = true;
            brain.enabled = true;
        }

        private IEnumerator WalkUpStairs(FirstPersonController player, Vector3 startPos, Vector3 endPos, float duration)
        {
            var cc = player.GetComponent<CharacterController>();
            float elapsed = 0f, verticalVelocity = -2f;
            float planarDistance = Vector3.Distance(new Vector3(startPos.x, 0f, startPos.z), new Vector3(endPos.x, 0f, endPos.z));
            float moveSpeed = duration > 0.01f ? planarDistance / duration : player.WalkSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Vector3 toTarget = Vector3.ProjectOnPlane(endPos - player.transform.position, Vector3.up);
                float remainingDistance = toTarget.magnitude;
                Vector3 moveDirection = remainingDistance > 0.05f ? toTarget / remainingDistance : Vector3.zero;
                if (moveDirection.sqrMagnitude > 0.001f) player.transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);

                if (cc != null && cc.enabled)
                {
                    verticalVelocity = (cc.isGrounded && verticalVelocity < 0f) ? -2f : verticalVelocity + Physics.gravity.y * Time.deltaTime;
                    Vector3 velocity = moveDirection * moveSpeed;
                    velocity.y = verticalVelocity;
                    cc.Move(velocity * Time.deltaTime);
                }

                if (endingCamera != null)
                {
                    Vector3 lookTarget = player.transform.position + Vector3.up * 1.1f;
                    endingCamera.transform.rotation = Quaternion.LookRotation(lookTarget - endingCamera.transform.position);
                }

                yield return null;
            }

            Physics.SyncTransforms();
        }

        private IEnumerator TurnToCamera(FirstPersonController player, Vector3 endPos, float duration)
        {
            var cc = player.GetComponent<CharacterController>();
            Vector3 toCam = endingCamera != null ? Vector3.ProjectOnPlane(endingCamera.transform.position - endPos, Vector3.up) : Vector3.back;
            Quaternion targetRot = toCam.sqrMagnitude > 0.001f ? Quaternion.LookRotation(toCam) : Quaternion.identity;
            Quaternion fromRot = player.transform.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                player.transform.rotation = Quaternion.Slerp(fromRot, targetRot, t);
                if (cc != null && cc.enabled) cc.Move(Vector3.down * 2f * Time.deltaTime);
                yield return null;
            }

            player.transform.rotation = targetRot;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (locomotion != null) locomotion.ClearPresentationOverride();
            if (endingCamera != null && running)
            {
                endingCamera.enabled = false;
                endingCamera.Priority = previousPriority;
                endingCamera.transform.rotation = endingCameraOriginalRotation;
            }
            if (brain != null) { brain.enabled = brainWasEnabled; brain.DefaultBlend = previousBlend; }
            if (physicalCamera != null)
            {
                physicalCamera.cullingMask = previousMask;
                physicalCamera.transform.SetLocalPositionAndRotation(cameraLocalPosition, cameraLocalRotation);
                physicalCamera.fieldOfView = cameraFov;
            }
            if (endCanvas != null) endCanvas.gameObject.SetActive(false);
            if (running && !finished && fadeController != null) fadeController.SetImmediate(0f);
        }
    }
}
