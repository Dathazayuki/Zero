using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class BackroomGameplayManager : MonoBehaviour
    {
        public enum ChaseState { Preparing, Waiting, Chasing, Caught, Respawning, Escaped, Faulted }
        [SerializeField] private NavMeshSurface surface;
        [SerializeField] private BackroomMonsterController monster;
        [SerializeField] private Transform respawnPoint;
        [SerializeField] private BackroomChaseStartTrigger startTrigger;
        [SerializeField] private CanvasGroup blackout;
        [SerializeField, Min(0f)] private float catchDuration = 0.65f;
        [SerializeField, Min(0f)] private float fadeDuration = 0.4f;
        private FirstPersonController player;
        private PlayerInteraction interaction;
        private bool oldInteractionInput;
        private bool ownsInput;
        private Camera cameraReference;
        private Vector3 cameraLocalPosition;
        private Quaternion cameraLocalRotation;
        private float cameraFov;
        private bool preparing;
        private bool ready;
        public ChaseState State { get; private set; } = ChaseState.Preparing;
        public bool IsReady => ready;
        public Transform RespawnPoint => respawnPoint;
        public event System.Action Completed;

        public void Configure(NavMeshSurface navigation, BackroomMonsterController creature, Transform respawn,
            BackroomChaseStartTrigger entry)
        { surface = navigation; monster = creature; respawnPoint = respawn; startTrigger = entry; }

        private IEnumerator Start()
        {
            yield return null;
            // Additive loading is prepared explicitly after the root is aligned.
            if (SceneManager.GetActiveScene() == gameObject.scene) yield return PrepareNavigation();
        }

        public IEnumerator PrepareNavigation()
        {
            if (State == ChaseState.Escaped) yield break;
            while (preparing) yield return null;
            if (ready) yield break;
            preparing = true;
            if (surface == null || monster == null || respawnPoint == null || startTrigger == null)
            {
                State = ChaseState.Faulted;
                preparing = false;
                Debug.LogError("BACKROOM_CHASE_CONFIGURATION_INVALID", this);
                yield break;
            }
            Physics.SyncTransforms();
            bool valid = true;
            try
            {
                surface.RemoveData();
                if (surface.navMeshData == null) surface.BuildNavMesh();
                else surface.AddData();
                valid = surface.navMeshData != null && monster.ResetMonster();
                var agent = monster.GetComponent<NavMeshAgent>();
                if (valid)
                {
                    var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                    var path = new NavMeshPath();
                    valid = NavMesh.SamplePosition(respawnPoint.position, out var destination, 2f, filter) &&
                        NavMesh.CalculatePath(monster.transform.position, destination.position, filter, path) &&
                        path.status == NavMeshPathStatus.PathComplete;
                }
            }
            catch (System.Exception exception) { valid = false; Debug.LogException(exception, this); }
            ready = valid;
            preparing = false;
            State = valid ? ChaseState.Waiting : ChaseState.Faulted;
            if (valid) { EnsureOverlay(); Debug.Log("BACKROOM_CHASE_READY", this); }
            else Debug.LogError("BACKROOM_CHASE_NAVIGATION_INVALID: check colliders, agent type, spawn and complete route to respawn.", this);
        }

        public bool OnPlayerEnterBackroom(FirstPersonController target)
        {
            if (!ready || State != ChaseState.Waiting || target == null || !target.InputEnabled) return false;
            player = target;
            cameraReference = player.PlayerCamera;
            if (cameraReference == null) return false;
            interaction = player.GetComponent<PlayerInteraction>();
            monster.Bind(this, player);
            if (!monster.StartChase()) return false;
            State = ChaseState.Chasing;
            return true;
        }

        public bool TryCatchPlayer(BackroomMonsterController source)
        {
            if (State != ChaseState.Chasing || source != monster || player == null) return false;
            State = ChaseState.Caught;
            monster.MarkCaught();
            StartCoroutine(CatchAndReplay());
            return true;
        }

        public void OnPlayerEscapedBackroom(FirstPersonController target)
        {
            if (State != ChaseState.Chasing || target != player) return;
            State = ChaseState.Escaped;
            monster.StopChase();
            Debug.Log("BACKROOM_PLAYER_ESCAPED", this);
            Completed?.Invoke();
        }

        public void ResetAttempt()
        {
            StopAllCoroutines();
            if (ownsInput) RestoreCamera(true);
            RestoreInput();
            if (blackout != null) blackout.alpha = 0f;
            if (!ready || monster == null) return;
            State = monster.ResetMonster() ? ChaseState.Waiting : ChaseState.Faulted;
            startTrigger?.Rearm();
        }

        public bool TryEnterSafeEnding(FirstPersonController target)
        {
            if (target == null || State == ChaseState.Caught || State == ChaseState.Respawning || preparing) return false;
            if (player != null && player != target) return false;
            player = target;
            State = ChaseState.Escaped;
            if (monster != null) monster.StopChase();
            return true;
        }

        private IEnumerator CatchAndReplay()
        {
            cameraLocalPosition = cameraReference.transform.localPosition;
            cameraLocalRotation = cameraReference.transform.localRotation;
            cameraFov = cameraReference.fieldOfView;
            oldInteractionInput = interaction != null && interaction.InputEnabled;
            if (interaction != null) interaction.SetInputEnabled(false);
            ownsInput = true;
            player.SetCinematicLock(true);
            player.ClearElevatorRideFeedback();
            Quaternion startRotation = cameraReference.transform.rotation;
            Vector3 startPosition = cameraReference.transform.position;
            Debug.Log("BACKROOM_PLAYER_CAUGHT", this);
            float elapsed = 0f;
            while (elapsed < catchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, catchDuration));
                Vector3 direction = monster.FocusTarget.position - startPosition;
                Quaternion look = direction.sqrMagnitude > 0.01f ? Quaternion.LookRotation(direction, Vector3.up) : startRotation;
                float noise = (Mathf.PerlinNoise(elapsed * 23f, 0.7f) - 0.5f) * 2f;
                cameraReference.transform.SetPositionAndRotation(startPosition + Vector3.up * (noise * 0.015f),
                    Quaternion.Slerp(startRotation, look, Mathf.SmoothStep(0, 1, t)) * Quaternion.Euler(0, 0, noise * 1.5f));
                cameraReference.fieldOfView = Mathf.Lerp(cameraFov, 42f, t);
                yield return null;
            }
            yield return Fade(1f);
            yield return null; // Present an opaque frame before teleport/agent reset.
            State = ChaseState.Respawning;
            player.ResetLocomotionModifiers();
            player.Teleport(respawnPoint.position, respawnPoint.rotation);
            RestoreCamera(false);
            bool reset = monster.ResetMonster();
            Physics.SyncTransforms();
            startTrigger.Rearm();
            yield return Fade(0f);
            RestoreInput();
            State = reset ? ChaseState.Waiting : ChaseState.Faulted;
            Debug.Log(reset ? "BACKROOM_REPLAY_READY" : "BACKROOM_REPLAY_FAILED", this);
        }

        private IEnumerator Fade(float target)
        {
            float from = blackout.alpha;
            for (float elapsed = 0; elapsed < fadeDuration; elapsed += Time.unscaledDeltaTime)
            {
                blackout.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(elapsed / fadeDuration));
                yield return null;
            }
            blackout.alpha = target;
        }

        private void EnsureOverlay()
        {
            if (blackout != null) { blackout.alpha = 0f; return; }
            var canvasObject = new GameObject("BackroomCatchCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            blackout = canvasObject.GetComponent<CanvasGroup>();
            blackout.alpha = 0f;
            blackout.blocksRaycasts = false;
            var imageObject = new GameObject("Blackout", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);
            var image = imageObject.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.one;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
        }

        private void RestoreCamera(bool originalRotation)
        {
            if (cameraReference == null) return;
            cameraReference.transform.localPosition = cameraLocalPosition;
            cameraReference.transform.localRotation = originalRotation ? cameraLocalRotation : Quaternion.identity;
            cameraReference.fieldOfView = cameraFov;
        }
        private void RestoreInput()
        {
            if (!ownsInput) return;
            ownsInput = false;
            if (interaction != null) interaction.SetInputEnabled(oldInteractionInput);
            if (player != null) player.SetCinematicLock(false);
        }
        private void OnDisable()
        {
            StopAllCoroutines();
            if (monster != null) monster.StopChase();
            if (ownsInput) RestoreCamera(true);
            RestoreInput();
            if (blackout != null) blackout.alpha = 0f;
            preparing = false;
        }
    }
}
