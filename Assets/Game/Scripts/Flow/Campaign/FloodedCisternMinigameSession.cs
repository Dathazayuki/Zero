using System;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Player;
using Zero.AnomalyPrototype.Utilities;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class FloodedCisternMinigameSession : MonoBehaviour, IMinigameSession
    {
        public enum AttemptState { Unprepared, Ready, Playing, Caught, Completed, Faulted }

        [SerializeField] private NavMeshSurface surface;
        [SerializeField] private Transform entrance;
        [SerializeField] private FirstPersonController demoPlayer;
        [SerializeField] private SceneFadeController localFade;
        [SerializeField] private CisternMannequinController[] mannequins;
        [SerializeField] private WaterFootstepRippleController ripples;
        [SerializeField] private Animator[] decorations;

        private FirstPersonController player;
        private PlayerInteraction interaction;
        private SceneFadeController fade;
        private Transform respawn;
        private bool ownsInput;
        private bool previousInteractionInput;
        private Vector3 cameraPosition;
        private Quaternion cameraRotation;

        public AttemptState State { get; private set; }
        public bool IsReady => State == AttemptState.Ready || State == AttemptState.Playing;
        public bool IsPlaying => State == AttemptState.Playing;
        public event Action Completed;

        private IEnumerator Start()
        {
            yield return null;
            if (SceneManager.GetActiveScene() != gameObject.scene || demoPlayer == null) yield break;
            yield return Prepare();
            if (IsReady) Begin(new MinigameContext(demoPlayer, localFade, entrance));
        }

        public IEnumerator Prepare()
        {
            if (State != AttemptState.Unprepared) yield break;
            if (surface == null || surface.navMeshData == null || entrance == null ||
                localFade == null || mannequins == null || mannequins.Length == 0)
            {
                FailConfiguration("navigation, entrance, local fade or mannequins missing; bake NavMesh before Play");
                yield break;
            }
            surface.RemoveData();
            surface.AddData();
            Physics.SyncTransforms();
            for (int i = 0; i < mannequins.Length; i++)
            {
                if (mannequins[i] != null && mannequins[i].PrepareNavigation()) continue;
                FailConfiguration("mannequin configuration or spawn is outside NavMesh");
                yield break;
            }
            localFade.SetImmediate(0f);
            SetDecorationAnimation(0f);
            TransitionTo(AttemptState.Ready);
            Debug.Log("CISTERN_MINIGAME_READY", this);
        }

        public void Begin(MinigameContext context)
        {
            if (State != AttemptState.Ready || context == null || context.Player == null ||
                context.Player.PlayerCamera == null) return;
            player = context.Player;
            fade = context.Fade != null ? context.Fade : localFade;
            respawn = context.Entrance != null ? context.Entrance : entrance;
            interaction = player.GetComponent<PlayerInteraction>();
            if (ripples != null) ripples.BindPlayer(player.GetComponent<CharacterController>());
            for (int i = 0; i < mannequins.Length; i++) mannequins[i].Bind(this, player);
            ResetAttempt();
            Debug.Log("CISTERN_ATTEMPT_STARTED", this);
        }

        public bool TryCatch(CisternMannequinController source)
        {
            if (!IsPlaying || source == null || source.State != CisternMannequinController.MotionState.Chasing ||
                Array.IndexOf(mannequins, source) < 0 || player == null) return false;
            TransitionTo(AttemptState.Caught);
            StopMannequins();
            StartCoroutine(CatchAndReplay(source.transform));
            return true;
        }

        public bool TryComplete(FirstPersonController target)
        {
            if (!IsPlaying || target != player) return false;
            TransitionTo(AttemptState.Completed);
            StopMannequins();
            Debug.Log("CISTERN_MINIGAME_COMPLETED", this);
            Completed?.Invoke();
            return true;
        }

        private IEnumerator CatchAndReplay(Transform threat)
        {
            Camera camera = player.PlayerCamera;
            cameraPosition = camera.transform.localPosition;
            cameraRotation = camera.transform.localRotation;
            previousInteractionInput = interaction != null && interaction.InputEnabled;
            ownsInput = true;
            player.SetCinematicLock(true);
            if (interaction != null) interaction.SetInputEnabled(false);
            threat.LookAt(new Vector3(player.transform.position.x, threat.position.y, player.transform.position.z));
            Quaternion start = camera.transform.rotation;
            Vector3 direction = threat.position + Vector3.up * 1.5f - camera.transform.position;
            Quaternion look = direction.sqrMagnitude > 0.01f ? Quaternion.LookRotation(direction) : start;
            Debug.Log("CISTERN_PLAYER_CAUGHT", this);
            for (float elapsed = 0f; elapsed < 0.4f; elapsed += Time.deltaTime)
            {
                camera.transform.rotation = Quaternion.Slerp(start, look, Mathf.Clamp01(elapsed / 0.4f));
                yield return null;
            }
            yield return fade.FadeToColor(Color.black, 1f, 0.2f);
            yield return null;
            player.Teleport(respawn.position, respawn.rotation);
            ResetMannequins();
            ripples?.ClearRipples();
            Physics.SyncTransforms();
            yield return fade.FadeTo(0f, 0.3f);
            RestoreInput(false);
            TransitionTo(AttemptState.Playing);
            Debug.Log("CISTERN_REPLAY_READY", this);
        }

        public void ResetAttempt()
        {
            if (State == AttemptState.Completed || State == AttemptState.Faulted ||
                State == AttemptState.Unprepared) return;
            StopAllCoroutines();
            RestoreInput(true);
            if (fade != null) fade.SetImmediate(0f);
            ResetMannequins();
            ripples?.ClearRipples();
            TransitionTo(player != null ? AttemptState.Playing : AttemptState.Ready);
        }

        private void ResetMannequins()
        {
            if (mannequins == null) return;
            for (int i = 0; i < mannequins.Length; i++) mannequins[i]?.ResetMonster();
        }

        private void StopMannequins()
        {
            if (mannequins == null) return;
            for (int i = 0; i < mannequins.Length; i++) mannequins[i]?.StopMonster();
        }

        private void SetDecorationAnimation(float speed)
        {
            if (decorations == null) return;
            for (int i = 0; i < decorations.Length; i++)
                if (decorations[i] != null) decorations[i].speed = speed;
        }

        private void RestoreInput(bool restoreCamera)
        {
            if (!ownsInput) return;
            ownsInput = false;
            if (player == null) return;
            if (restoreCamera && player.PlayerCamera != null)
                player.PlayerCamera.transform.SetLocalPositionAndRotation(cameraPosition, cameraRotation);
            player.SetCinematicLock(false);
            if (interaction != null) interaction.SetInputEnabled(previousInteractionInput);
        }

        private void FailConfiguration(string reason)
        {
            StopMannequins();
            TransitionTo(AttemptState.Faulted);
            if (localFade != null) localFade.SetImmediate(0f);
            Debug.LogError($"CISTERN_CONFIGURATION_INVALID: {reason}", this);
        }

        private void TransitionTo(AttemptState next) => State = next;

        private void OnDisable()
        {
            StopAllCoroutines();
            RestoreInput(true);
            ResetMannequins();
            SetDecorationAnimation(1f);
            ripples?.ClearRipples();
            if (fade != null) fade.SetImmediate(0f);
            TransitionTo(AttemptState.Unprepared);
            player = null;
        }
    }
}
