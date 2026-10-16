using System;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class CallingCorridorSession : MonoBehaviour, IMinigameSession
    {
        public enum AttemptState { Unprepared, Ready, Playing, Resetting, Completed, Faulted }
        [SerializeField] private NavMeshSurface surface;
        [SerializeField] private FirstPersonController demoPlayer;
        [SerializeField] private SceneFadeController localFade;
        [SerializeField] private CallingCorridorStation[] stations;
        [SerializeField] private CallingCorridorGate[] gates;
        [SerializeField] private CallingCorridorThreat threat;
        [SerializeField] private CallingCorridorSafeAlcove[] alcoves;
        private readonly CallingCorridorRules rules = new CallingCorridorRules();
        private FirstPersonController player;
        private SceneFadeController fade;
        private bool ownsInput;
        private bool previousInput;
        private Quaternion cameraRotation;
        public AttemptState State { get; private set; }
        public int Round => rules.Round;
        public int Decision => rules.Decision;
        public bool IsPlaying => State == AttemptState.Playing;
        public bool IsReady => State == AttemptState.Ready || IsPlaying;
        public Transform PlayerTransform => player != null ? player.transform : null;
        public event Action Completed;
        public event Action<int> DecisionChanged;
        public bool IsSheltered => player != null && ContainsShelter(player.transform.position);

        private bool ContainsShelter(Vector3 position)
        {
            for (int i = 0; i < alcoves.Length; i++)
                if (alcoves[i] != null && alcoves[i].Contains(position)) return true;
            return false;
        }

        private IEnumerator Start()
        {
            yield return null;
            if (SceneManager.GetActiveScene() != gameObject.scene || demoPlayer == null) yield break;
            yield return Prepare();
            if (IsReady) Begin(new MinigameContext(demoPlayer, localFade, stations[0].Checkpoint));
        }

        public IEnumerator Prepare()
        {
            if (State != AttemptState.Unprepared) yield break;
            if (!ValidateComposition())
            {
                TransitionTo(AttemptState.Faulted);
                if (localFade != null) localFade.SetImmediate(0f);
                Debug.LogError("CALLING_CORRIDOR_CONFIGURATION_INVALID: references or baked navigation missing", this);
                yield break;
            }
            surface.RemoveData();
            surface.AddData();
            localFade.SetImmediate(0f);
            TransitionTo(AttemptState.Ready);
            Debug.Log("CALLING_CORRIDOR_READY", this);
        }

        private bool ValidateComposition()
        {
            if (!HasInfrastructure()) return false;
            if (stations == null || gates == null || alcoves == null) return false;
            if (stations.Length != 4 || gates.Length != 1 || alcoves.Length == 0) return false;
            return ValidateStations() && ValidateGates();
        }

        private bool HasInfrastructure() => surface != null && surface.navMeshData != null && localFade != null;

        private bool ValidateStations()
        {
            for (int i = 0; i < stations.Length; i++)
            {
                if (stations[i] == null || !stations[i].IsConfigured) return false;
                stations[i].Bind(this, i);
            }
            return true;
        }

        private bool ValidateGates()
        {
            for (int i = 0; i < gates.Length; i++)
            {
                if (gates[i] == null) return false;
                gates[i].Bind(this, i);
                if (!gates[i].IsConfigured) return false;
            }
            return true;
        }

        public void Begin(MinigameContext context)
        {
            if (State != AttemptState.Ready || context == null || context.Player == null) return;
            if (context.Player.PlayerCamera == null) return;
            player = context.Player;
            fade = context.Fade != null ? context.Fade : localFade;
            if (threat != null) threat.Bind(this, player);
            ResetAttempt();
        }

        public bool IsPlayer(Collider other) => other != null && player != null &&
            other.GetComponentInParent<FirstPersonController>() == player;

        public bool CanChoose(int decision) => IsPlaying &&
            !rules.IsComplete && !rules.AwaitingPassage && !rules.AwaitingRetry && decision == rules.Decision;

        public bool TryChoose(CallingCorridorGate gate, CallingCorridorDoor door)
        {
            if (gate == null || door == null || !CanChoose(gate.Index)) return false;
            if (gates[0] != gate || !gate.Contains(door)) return false;
            CallingCorridorRoomEncounter room = gate.SelectedRoom;
            if (room == null) return false;
            int decision = Decision;
            CallingCorridorRules.Choice result = rules.EnterRoom(decision, door.Code);
            if (result == CallingCorridorRules.Choice.Rejected) return false;
            stations[Round].Show(false);
            if (result == CallingCorridorRules.Choice.Wrong)
            {
                TransitionTo(AttemptState.Resetting);
                StartCoroutine(Replay(room));
            }
            else
            {
                room.BeginFlee();
                if (threat != null) threat.ResetThreat();
                RefreshPresentation();
                TryStartThreat();
            }
            Debug.Log($"CALLING_CORRIDOR_CHOICE: decision={decision}, result={result}", this);
            return true;
        }

        public bool TryPassage(int decision) => false;

        public bool TryVisitExit(CallingCorridorRoomEncounter room)
        {
            if (!IsPlaying || !rules.IsComplete || room == null) return false;
            if (room != gates[0].ExitRoom || room.State != CallingCorridorRoomEncounter.OccupantState.RabbitWaiting) return false;
            if (!room.GreetAtExit()) return false;
            return TryFinish();
        }

        public bool TryFinish()
        {
            if (!IsPlaying || !rules.IsComplete || !gates[0].ExitRoom.IsConsumed) return false;
            TransitionTo(AttemptState.Completed);
            if (threat != null) threat.ResetThreat();
            gates[0].ShowCompletion();
            Debug.Log("CALLING_CORRIDOR_COMPLETED", this);
            Completed?.Invoke();
            return true;
        }

        public bool TryStartThreat()
        {
            if (!IsPlaying || !rules.CanStartMannequin || threat == null || threat.IsActive) return false;
            if (!threat.BeginEncounter(stations[Round])) return false;
            return rules.TryConsumeEncounter(true);
        }

        public bool TryCatch()
        {
            if (!IsPlaying || threat == null || !threat.IsActive || IsSheltered) return false;
            TransitionTo(AttemptState.Resetting);
            StartCoroutine(ReplayThreat());
            return true;
        }

        private IEnumerator ReplayThreat()
        {
            LockInput();
            Vector3 direction = threat.transform.position + Vector3.up * 1.5f - player.PlayerCamera.transform.position;
            if (direction.sqrMagnitude > 0.01f) player.PlayerCamera.transform.rotation = Quaternion.LookRotation(direction);
            threat.ResetThreat();
            yield return fade.FadeToColor(Color.black, 1f, 0.2f);
            rules.Reset();
            gates[0].ResetRooms();
            ResetShelterDoors();
            player.Teleport(stations[0].Checkpoint.position, stations[0].Checkpoint.rotation);
            RefreshPresentation();
            Physics.SyncTransforms();
            yield return fade.FadeTo(0f, 0.3f);
            RestoreInput(true);
            TransitionTo(AttemptState.Playing);
            Debug.Log("CALLING_CORRIDOR_MANNEQUIN_DEATH_RESET", this);
        }

        private IEnumerator Replay(CallingCorridorRoomEncounter room)
        {
            if (threat != null) threat.ResetThreat();
            LockInput();
            yield return Jumpscare(room);
            yield return room.Attack(player);
            yield return fade.FadeToColor(Color.black, 1f, 0.2f);
            rules.Reset();
            gates[0].ResetRooms();
            ResetShelterDoors();
            player.Teleport(stations[Round].Checkpoint.position, stations[Round].Checkpoint.rotation);
            RefreshPresentation();
            Physics.SyncTransforms();
            yield return null;
            yield return fade.FadeTo(0f, 0.3f);
            RestoreInput(false);
            TransitionTo(AttemptState.Playing);
            Debug.Log("CALLING_CORRIDOR_DEATH_RESET", this);
        }

        private IEnumerator Jumpscare(CallingCorridorRoomEncounter room)
        {
            Camera view = player.PlayerCamera;
            Quaternion start = view.transform.rotation;
            Vector3 direction = room.AttackTarget.position - view.transform.position;
            Quaternion target = direction.sqrMagnitude > 0.01f ? Quaternion.LookRotation(direction) : start;
            for (float elapsed = 0f; elapsed < 0.4f; elapsed += Time.deltaTime)
            {
                view.transform.rotation = Quaternion.Slerp(start, target, Mathf.Clamp01(elapsed / 0.2f));
                yield return null;
            }
        }

        private void LockInput()
        {
            previousInput = player.InputEnabled;
            cameraRotation = player.PlayerCamera.transform.localRotation;
            ownsInput = true;
            player.SetCinematicLock(true);
        }

        private void RestoreInput(bool restoreCamera)
        {
            if (!ownsInput) return;
            ownsInput = false;
            if (player == null) return;
            if (restoreCamera && player.PlayerCamera != null) player.PlayerCamera.transform.localRotation = cameraRotation;
            player.SetCinematicLock(false);
            player.SetInputEnabled(previousInput);
        }

        public void ResetAttempt()
        {
            if (State == AttemptState.Unprepared || State == AttemptState.Faulted || State == AttemptState.Completed) return;
            StopAllCoroutines();
            RestoreInput(true);
            if (threat != null) threat.ResetThreat();
            gates[0].ResetRooms();
            ResetShelterDoors();
            rules.Reset();
            if (fade != null) fade.SetImmediate(0f);
            if (player != null) player.Teleport(stations[0].Checkpoint.position, stations[0].Checkpoint.rotation);
            RefreshPresentation();
            Physics.SyncTransforms();
            TransitionTo(player != null ? AttemptState.Playing : AttemptState.Ready);
        }

        private void RefreshPresentation()
        {
            for (int i = 0; i < gates.Length; i++) gates[i].Refresh(Decision);
            bool show = !rules.IsComplete && Decision == CallingCorridorRules.RoundStart(Round);
            for (int i = 0; i < stations.Length; i++) stations[i].Show(false);
            stations[Round].Show(show);
            DecisionChanged?.Invoke(Decision);
        }

        private void TransitionTo(AttemptState next) => State = next;

        private void OnDisable()
        {
            StopAllCoroutines();
            RestoreInput(true);
            if (threat != null) threat.ResetThreat();
            ResetShelterDoors();
            if (fade != null) fade.SetImmediate(0f);
            if (gates != null) ResetDoors();
            TransitionTo(AttemptState.Unprepared);
            player = null;
        }

        private void ResetDoors()
        {
            for (int i = 0; i < gates.Length; i++)
            {
                gates[i]?.ResetRooms();
            }
        }

        private void ResetShelterDoors()
        {
            if (alcoves == null) return;
            for (int i = 0; i < alcoves.Length; i++) alcoves[i]?.ResetDoor();
        }
    }
}
