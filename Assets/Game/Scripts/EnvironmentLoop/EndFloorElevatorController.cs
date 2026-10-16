using System.Collections;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Player;

namespace Zero.EnvironmentLoop
{
    public enum ElevatorEndingState
    {
        Dormant, Available, DoorsOpening, AwaitingPlayer, DoorsClosing, LoadingBackroom,
        Riding, Arrived, DoorsOpeningAtDestination, PlayerExited, Consumed
    }

    public sealed class EndFloorElevatorController : MonoBehaviour
    {
        [SerializeField] private Transform leftDoor;
        [SerializeField] private Transform rightDoor;
        [SerializeField] private Collider doorwayBlocker;
        [SerializeField] private Transform cabinBackroomAnchor;
        [SerializeField] private GameObject[] departureOnlyObjects;
        [SerializeField] private AudioSource motorAudio;
        [SerializeField] private AudioSource doorAudio;
        [SerializeField] private AudioSource arrivalAudio;
        [SerializeField] private Light cabinLight;
        [SerializeField] private TMPro.TMP_Text floorIndicator;
        [SerializeField, Min(0.1f)] private float doorDuration = 0.8f;
        [SerializeField, Min(5f)] private float minimumRideDuration = 8f;
        [SerializeField, Range(0.5f, 5f)] private float arrivalWarmupDelay = 1.5f;
        private EndFloorSequenceController sequence;
        private DirectionalAnomalyGameManager manager;
        private Collider[] doorColliders;
        private ElevatorEndingTrigger[] triggers;
        private Vector3 leftClosed, rightClosed;
        private bool[] departureActive;
        private float lightIntensity;
        private float motorVolume;
        private SourceSoundPlayback motorPlayback;
        private SourceSoundPlayback doorPlayback;
        private SourceSoundPlayback arrivalPlayback;
        private bool geometryHidden;
        private readonly System.Collections.Generic.List<GameObject> autoHiddenObjects = new System.Collections.Generic.List<GameObject>();
        private PlayerDeathController death;
        public FirstPersonController Player { get; private set; }
        public Transform ArrivalAnchor => cabinBackroomAnchor;
        public ElevatorEndingState State { get; private set; }
        public bool IsRideActive => isActiveAndEnabled && State >= ElevatorEndingState.LoadingBackroom &&
                                    State <= ElevatorEndingState.DoorsOpeningAtDestination;

        private void Awake()
        {
            if (leftDoor != null) leftClosed = leftDoor.localScale;
            if (rightDoor != null) rightClosed = rightDoor.localScale;
            var colliders = new System.Collections.Generic.List<Collider>();
            if (leftDoor != null) colliders.AddRange(leftDoor.GetComponentsInChildren<Collider>(true));
            if (rightDoor != null) colliders.AddRange(rightDoor.GetComponentsInChildren<Collider>(true));
            doorColliders = colliders.ToArray();
            triggers = GetComponentsInChildren<ElevatorEndingTrigger>(true);
            departureOnlyObjects = departureOnlyObjects ?? System.Array.Empty<GameObject>();
            departureActive = new bool[departureOnlyObjects.Length];
            if (cabinLight != null) lightIntensity = cabinLight.intensity;
            if (motorAudio != null) motorVolume = motorAudio.volume;
            SetDoorScale(0f);
            if (doorwayBlocker != null) doorwayBlocker.enabled = true;
        }

        public void MakeAvailable(EndFloorSequenceController owner)
        {
            if (State != ElevatorEndingState.Dormant) return;
            sequence = owner;
            manager = FindFirstObjectByType<DirectionalAnomalyGameManager>();
            if (leftDoor == null || rightDoor == null || cabinBackroomAnchor == null || doorwayBlocker == null ||
                Mathf.Abs(leftClosed.x) < 0.001f || Mathf.Abs(rightClosed.x) < 0.001f)
            {
                Debug.LogError("ELEVATOR_CONFIGURATION_INVALID: doors, closed scale, anchor or blocker missing.", this);
                return;
            }
            State = ElevatorEndingState.Available;
        }

        public bool HandleTrigger(ElevatorTriggerKind kind, FirstPersonController player)
        {
            if (player == null) return false;
            if (kind == ElevatorTriggerKind.Approach && State == ElevatorEndingState.Available)
            {
                if (manager == null && GameFlowController.Instance?.Campaign?.IsActive != true) return false;
                State = ElevatorEndingState.DoorsOpening;
                StartCoroutine(OpenForEntry());
                return true;
            }
            if (kind == ElevatorTriggerKind.Entry && State == ElevatorEndingState.AwaitingPlayer)
            {
                if (!TrySelectDestination()) return false;
                Player = player;
                death = player.GetComponent<PlayerDeathController>();
                sequence?.SetElevatorSelected(true);
                State = ElevatorEndingState.DoorsClosing;
                StartCoroutine(Ride());
                Debug.Log("ELEVATOR_PLAYER_ENTERED", this);
                return true;
            }
            if (kind == ElevatorTriggerKind.Exit && State == ElevatorEndingState.Arrived && player == Player)
            {
                if (manager != null && GameFlowController.Instance?.Campaign?.IsActive != true)
                    manager.CompleteRunFromBackroomExit();
                State = ElevatorEndingState.PlayerExited;
                GameFlowController.Instance?.CommitBackroomHandoff(player);
                StartCoroutine(CloseAfterExit());
                Debug.Log("BACKROOM_PLAYER_EXITED", this);
                return true;
            }
            return false;
        }

        private bool TrySelectDestination()
        {
            var campaign = GameFlowController.Instance?.Campaign;
            return campaign?.IsActive == true ? campaign.TryResolveDestination(out _) :
                manager != null && manager.TrySelectEndingRoute(EndingRoute.BackroomElevator);
        }

        private IEnumerator OpenForEntry()
        {
            yield return TweenDoors(true);
            State = ElevatorEndingState.AwaitingPlayer;
        }

        private IEnumerator Ride()
        {
            yield return TweenDoors(false);
            State = ElevatorEndingState.LoadingBackroom;
            if (GameFlowController.Instance == null || !GameFlowController.Instance.BeginBackroomRide(this))
            {
                RecoverFailedRide();
                yield break;
            }
            HideDeparture();
            State = ElevatorEndingState.Riding;
            if (motorAudio != null)
            {
                motorAudio.loop = true;
                GameAudioBootstrap.BindSource(ref motorPlayback, motorAudio).Play(null, motorAudio.clip, false);
            }
            Debug.Log("ELEVATOR_RIDE_STARTED", this);
            float elapsed = 0f;
            bool slowLogged = false;
            float minimum = Mathf.Max(5f, minimumRideDuration);
            while (elapsed < minimum || !GameFlowController.Instance.IsBackroomReady || GameFlowController.Instance.IsTransitioning)
            {
                elapsed += Time.deltaTime;
                ApplyRideFeedback(elapsed, minimum);
                if (elapsed >= (minimum + 5f) && !slowLogged)
                {
                    slowLogged = true;
                    Debug.LogWarning("BACKROOM_LOAD_SLOW", this);
                }
                yield return null;
            }

            float settleElapsed = 0f;
            float settleDuration = Mathf.Max(0.5f, arrivalWarmupDelay);
            while (settleElapsed < settleDuration)
            {
                settleElapsed += Time.deltaTime;
                float settleT = 1f - Mathf.Clamp01(settleElapsed / settleDuration);
                if (Player != null) Player.SetElevatorRideFeedback(settleT * 0.15f);
                if (motorAudio != null) motorAudio.volume = motorVolume * settleT * 0.4f;
                yield return null;
            }

            ClearFeedback();
            if (arrivalAudio != null)
                GameAudioBootstrap.BindSource(ref arrivalPlayback, arrivalAudio).Play(null, arrivalAudio.clip, false);
            Debug.Log("ELEVATOR_ARRIVED", this);
            State = ElevatorEndingState.DoorsOpeningAtDestination;
            yield return TweenDoors(true);
            State = ElevatorEndingState.Arrived;
            Debug.Log("ELEVATOR_DOORS_OPENED_AT_DESTINATION", this);
        }

        private void ApplyRideFeedback(float elapsed, float minimum)
        {
            float strength = Mathf.Clamp01(elapsed / 0.5f);
            if (elapsed > minimum - 0.5f) strength *= 0.3f;
            Player.SetElevatorRideFeedback(strength);
            if (motorAudio != null) motorAudio.volume = motorVolume * Mathf.Lerp(0.5f, 1f, strength);
            if (cabinLight != null) cabinLight.intensity = lightIntensity * (1f + Mathf.Sin(elapsed * 17f) * 0.015f * strength);
            if (floorIndicator == null) return;
            float progress = Mathf.Clamp01(elapsed / minimum);
            floorIndicator.text = progress < 0.35f ? "8" : progress < 0.7f ? "B1" : "B2";
        }

        private IEnumerator TweenDoors(bool open)
        {
            if (doorAudio != null)
                GameAudioBootstrap.BindSource(ref doorPlayback, doorAudio).Play(null, doorAudio.clip, false);
            Debug.Log(open ? "ELEVATOR_DOORS_OPENING" : "ELEVATOR_DOORS_CLOSING", this);
            if (!open && doorwayBlocker != null) doorwayBlocker.enabled = true;
            // Independent blocker supplies collision; zero-scaled door colliders stay disabled.
            foreach (Collider item in doorColliders) if (item != null) item.enabled = false;
            float start = leftDoor != null ? 1f - leftDoor.localScale.x / leftClosed.x : 0f;
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, doorDuration);
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetDoorScale(Mathf.Lerp(start, open ? 1f : 0f, Mathf.SmoothStep(0f, 1f, elapsed / duration)));
                yield return null;
            }
            SetDoorScale(open ? 1f : 0f);
            if (doorwayBlocker != null) doorwayBlocker.enabled = !open;
            if (doorAudio != null) doorAudio.Stop();
            if (!open) Debug.Log("ELEVATOR_DOORS_CLOSED", this);
        }

        private void SetDoorScale(float openness)
        {
            if (leftDoor != null) { var scale = leftClosed; scale.x *= 1f - openness; leftDoor.localScale = scale; }
            if (rightDoor != null) { var scale = rightClosed; scale.x *= 1f - openness; rightDoor.localScale = scale; }
        }

        private void HideDeparture()
        {
            autoHiddenObjects.Clear();
            for (int i = 0; i < departureOnlyObjects.Length; i++)
            {
                var item = departureOnlyObjects[i];
                if (item == null || transform.IsChildOf(item.transform) || !item.activeSelf) continue;
                departureActive[i] = true;
                item.SetActive(false);
                autoHiddenObjects.Add(item);
            }

            // Auto-hide all siblings under parent segment so nothing outside the cabin renders
            Transform current = transform.parent;
            while (current != null)
            {
                for (int i = 0; i < current.childCount; i++)
                {
                    Transform child = current.GetChild(i);
                    if (child == transform || transform.IsChildOf(child) || !child.gameObject.activeSelf) continue;
                    child.gameObject.SetActive(false);
                    autoHiddenObjects.Add(child.gameObject);
                }
                if (current.GetComponent<FloorSegment>() != null || current.parent == null) break;
                current = current.parent;
            }

            geometryHidden = true;
        }

        private void RestoreDeparture()
        {
            if (!geometryHidden) return;
            for (int i = 0; i < departureOnlyObjects.Length; i++)
                if (departureOnlyObjects[i] != null && !transform.IsChildOf(departureOnlyObjects[i].transform))
                    departureOnlyObjects[i].SetActive(departureActive[i]);

            foreach (var go in autoHiddenObjects)
            {
                if (go != null) go.SetActive(true);
            }
            autoHiddenObjects.Clear();
            geometryHidden = false;
        }

        public void RecoverFailedRide()
        {
            StopAllCoroutines();
            ClearFeedback();
            RestoreDeparture();
            manager?.ReleaseEndingRoute(EndingRoute.BackroomElevator);
            sequence?.SetElevatorSelected(false);
            foreach (var trigger in triggers) if (trigger != null) trigger.Rearm();
            State = ElevatorEndingState.DoorsOpening;
            StartCoroutine(OpenForEntry());
        }

        public IEnumerator CloseAfterExit()
        {
            yield return TweenDoors(false);
            State = ElevatorEndingState.Consumed;
        }

        public void DisableRoute()
        {
            StopAllCoroutines();
            ClearFeedback();
            State = ElevatorEndingState.Consumed;
            SetDoorScale(0f);
            if (doorwayBlocker != null) doorwayBlocker.enabled = true;
        }

        private void ClearFeedback()
        {
            if (Player != null) Player.ClearElevatorRideFeedback();
            if (motorAudio != null) { motorAudio.Stop(); motorAudio.volume = motorVolume; }
            if (cabinLight != null) cabinLight.intensity = lightIntensity;
        }

        private void Update()
        {
            if (death != null && death.IsDeathInProgress && State != ElevatorEndingState.Dormant && State != ElevatorEndingState.Consumed)
            {
                GameFlowController.Instance?.CancelBackroomTransition();
                StopAllCoroutines();
                ClearFeedback();
                State = ElevatorEndingState.Dormant;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ClearFeedback();
            if (State >= ElevatorEndingState.LoadingBackroom && State <= ElevatorEndingState.DoorsOpeningAtDestination)
                GameFlowController.Instance?.CancelBackroomTransition();
        }
    }
}
