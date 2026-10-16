using System;
using System.Collections;
using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class DarumaSession : MonoBehaviour, IMinigameSession
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private Transform monster;
        [SerializeField] private Transform entrance;
        [SerializeField] private Transform finish;
        [SerializeField] private SceneFadeController fade;
        [SerializeField] private DarumaTimerDisplay timer;
        [SerializeField, Min(1f)] private float timeLimit = 120f;
        [SerializeField, Min(0.1f)] private float moveSeconds = 3f;
        [SerializeField, Min(0.1f)] private float watchSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float turnSeconds = 0.8f;
        [Header("Variable Rhythm")]
        [SerializeField] private bool randomizeTiming = true;
        [SerializeField] private Vector2 timingMultiplierRange = new Vector2(0.65f, 1.45f);
        [SerializeField, Range(0f, 0.5f)] private float finishPressure = 0.25f;
        [SerializeField, Min(0.001f)] private float movementTolerance = 0.06f;
        [SerializeField, Min(0f)] private float startLineDistance = 5.1f;
        [SerializeField, Min(0.2f)] private float catchDistance = 1.25f;
        [SerializeField, Min(0.1f)] private float catchHoldSeconds = 0.8f;
        private DarumaRules rules;
        private Animator animator;
        private Vector3 monsterPosition;
        private Quaternion monsterRotation;
        private Quaternion facingOffset;
        private Vector3 watchAnchor;
        private Quaternion cameraRotation;
        private bool ownsInput;
        private bool previousInput;
        private float animationSpeed;
        private Coroutine replay;
        private DarumaRules.Phase presentedState;
        public event Action Completed;
        public event Action<DarumaRules.Phase> StateChanged;
        public bool IsReady { get; private set; }
        public DarumaRules.Phase State => rules != null ? rules.State : DarumaRules.Phase.Waiting;
        public float Remaining => rules != null ? rules.Remaining : timeLimit;

        private void Awake()
        {
            IsReady = player != null && monster != null && entrance != null && finish != null && fade != null && timer != null;
            IsReady = IsReady && player.PlayerCamera != null;
            if (!IsReady) { Debug.LogError("DARUMA_CONFIGURATION_INVALID: missing reference", this); return; }
            animator = monster.GetComponentInChildren<Animator>();
            animationSpeed = animator != null ? animator.speed : 1f;
            monsterPosition = monster.position;
            monsterRotation = monster.rotation;
            Vector3 facing = Vector3.ProjectOnPlane(entrance.position - monsterPosition, Vector3.up);
            facingOffset = Quaternion.Inverse(Quaternion.LookRotation(facing.normalized)) * monsterRotation;
            rules = new DarumaRules(timeLimit, moveSeconds, watchSeconds, turnSeconds, movementTolerance);
            if (randomizeTiming)
                rules.ConfigureRandomTiming(Environment.TickCount, timingMultiplierRange.x, timingMultiplierRange.y, finishPressure);
        }

        private void Start()
        {
            if (IsReady) ResetAttempt();
        }

        public IEnumerator Prepare() { yield break; }

        public void Begin(MinigameContext context)
        {
            if (!IsReady || context == null || context.Player == null) return;
            player = context.Player;
            ResetAttempt();
        }

        private void LateUpdate()
        {
            if (!IsReady || player == null || Time.deltaTime <= 0f || replay != null) return;
            if (State == DarumaRules.Phase.Completed) return;
            Vector3 position = player.transform.position;
            if (!TryStart(position)) return;
            DarumaRules.Phase before = State;
            if (before == DarumaRules.Phase.Watching) rules.ObserveMovement(HorizontalDistance(position, watchAnchor));
            rules.Tick(Time.deltaTime, RouteProgress(position));
            PublishState();
            if (State == DarumaRules.Phase.Watching && before != State) watchAnchor = position;
            PresentMonster();
            timer.Show(Remaining);
            if (State == DarumaRules.Phase.Caught) { replay = StartCoroutine(ReplayCatch()); return; }
            CheckCompletion(position);
        }

        private bool TryStart(Vector3 position)
        {
            if (State != DarumaRules.Phase.Waiting) return true;
            if (Vector3.Dot(position - entrance.position, entrance.forward) < startLineDistance) return false;
            rules.Start();
            Debug.Log("DARUMA_STARTED", this);
            return true;
        }

        private void CheckCompletion(Vector3 position)
        {
            if (Vector3.Dot(position - finish.position, entrance.forward) < 0f) return;
            if (HorizontalDistance(position, finish.position) > 5.5f) return;
            if (!rules.Complete()) return;
            PublishState();
            Debug.Log("DARUMA_COMPLETED", this);
            Completed?.Invoke();
        }

        private static float HorizontalDistance(Vector3 first, Vector3 second)
        {
            first.y = second.y;
            return Vector3.Distance(first, second);
        }

        private float RouteProgress(Vector3 position)
        {
            float length = Vector3.Dot(finish.position - entrance.position, entrance.forward);
            return Mathf.Clamp01(Vector3.Dot(position - entrance.position, entrance.forward) / Mathf.Max(1f, length));
        }

        private void PublishState()
        {
            if (presentedState == State) return;
            presentedState = State;
            StateChanged?.Invoke(State);
        }

        private void PresentMonster()
        {
            float angle = 0f;
            if (State == DarumaRules.Phase.Moving) angle = 180f;
            else if (State == DarumaRules.Phase.TurningToWatch) angle = 180f * (1f - rules.TurnFraction);
            else if (State == DarumaRules.Phase.TurningAway) angle = 180f * rules.TurnFraction;
            monster.rotation = monsterRotation * Quaternion.Euler(0f, angle, 0f);
        }

        private IEnumerator ReplayCatch()
        {
            previousInput = player.InputEnabled;
            cameraRotation = player.PlayerCamera.transform.localRotation;
            ownsInput = true;
            player.SetCinematicLock(true);
            Transform view = player.PlayerCamera.transform;
            view.localRotation = Quaternion.identity;
            Vector3 forward = Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.01f) forward = player.transform.forward;
            monster.position = player.transform.position + forward * catchDistance;
            monster.rotation = Quaternion.LookRotation(-forward) * facingOffset;
            if (animator != null) animator.speed = 0f;
            Debug.Log("DARUMA_CAUGHT", this);
            yield return new WaitForSeconds(catchHoldSeconds);
            yield return fade.FadeToColor(Color.black, 1f, 0.2f);
            RestoreMonster();
            rules.Reset();
            PublishState();
            player.Teleport(entrance.position, entrance.rotation);
            timer.Show(Remaining);
            yield return fade.FadeTo(0f, 0.3f);
            ReleaseInput();
            replay = null;
            Debug.Log("DARUMA_RESET", this);
        }

        public void ResetAttempt()
        {
            if (!IsReady) return;
            StopAllCoroutines();
            replay = null;
            ReleaseInput();
            RestoreMonster();
            rules.Reset();
            PublishState();
            player.Teleport(entrance.position, entrance.rotation);
            fade.SetImmediate(0f);
            timer.Show(Remaining);
        }

        private void RestoreMonster()
        {
            if (monster != null) monster.SetPositionAndRotation(monsterPosition, monsterRotation);
            if (animator != null) animator.speed = animationSpeed;
        }

        private void ReleaseInput()
        {
            if (!ownsInput) return;
            ownsInput = false;
            if (player == null) return;
            if (player.PlayerCamera != null) player.PlayerCamera.transform.localRotation = cameraRotation;
            player.SetCinematicLock(false);
            player.SetInputEnabled(previousInput);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            replay = null;
            ReleaseInput();
            RestoreMonster();
            if (fade != null) fade.SetImmediate(0f);
            rules?.Reset();
            PublishState();
        }
    }
}
