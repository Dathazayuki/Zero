using UnityEngine;
using UnityEngine.AI;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
    public sealed class CisternMannequinController : MonoBehaviour
    {
        public enum MotionState { Dormant, Alert, Frozen, Chasing, Stopped }

        [SerializeField] private Animator animator;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private GameObject eye1;
        [SerializeField] private GameObject eye2;
        [SerializeField] private LayerMask obstructionMask = ~0;
        [SerializeField, Min(0f)] private float activationDelay = 0.35f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 3.8f;
        [SerializeField, Min(0.1f)] private float catchDistance = 1.2f;
        [SerializeField, Min(0.05f)] private float repathInterval = 0.2f;

        private NavMeshAgent agent;
        private FloodedCisternMinigameSession session;
        private FirstPersonController player;
        private Camera view;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private float originalAnimationSpeed;
        private float alertRemaining;
        private float nextRepath;
        private bool spawnCached;

        public MotionState State { get; private set; }
        public bool IsConfigured => animator != null && animator.runtimeAnimatorController != null &&
            bodyRenderer != null && eye1 != null && eye2 != null && agent != null;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator != null) originalAnimationSpeed = animator.speed;
            SetEyes(false);
            StopMotion();
        }

        public bool PrepareNavigation()
        {
            if (!IsConfigured) return false;
            if (!spawnCached)
            {
                spawnPosition = transform.position;
                spawnRotation = transform.rotation;
                spawnCached = true;
            }
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, 1f, filter)) return false;
            agent.enabled = true;
            if (!agent.Warp(hit.position)) return false;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = 0.2f;
            animator.applyRootMotion = false;
            ResetMonster();
            return true;
        }

        public void Bind(FloodedCisternMinigameSession owner, FirstPersonController target)
        {
            session = owner;
            player = target;
            view = target != null ? target.PlayerCamera : null;
        }

        public bool TryActivate(FirstPersonController target)
        {
            if (State != MotionState.Dormant || target != player || session == null ||
                !session.IsPlaying || !target.InputEnabled || !agent.isOnNavMesh) return false;
            alertRemaining = activationDelay;
            SetEyes(true);
            TransitionTo(MotionState.Alert);
            Debug.Log("CISTERN_MANNEQUIN_ACTIVATED", this);
            return true;
        }

        private void Update()
        {
            if (State == MotionState.Dormant || State == MotionState.Stopped ||
                session == null || !session.IsPlaying || player == null || view == null ||
                !player.InputEnabled || Time.deltaTime <= 0f) return;
            TickMotion(Time.deltaTime);
        }

        private void TickMotion(float deltaTime)
        {
            if (alertRemaining > 0f)
            {
                alertRemaining -= deltaTime;
                return;
            }
            bool observed = AnomalyGazeVisibility.IsVisible(view, transform, bodyRenderer, obstructionMask, 0.08f);
            TransitionTo(observed ? MotionState.Frozen : MotionState.Chasing);
            if (observed || !agent.isOnNavMesh) return;
            Repath();
            if (CanCatchPlayer()) session.TryCatch(this);
        }

        private void Repath()
        {
            if (Time.time < nextRepath) return;
            nextRepath = Time.time + repathInterval;
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (NavMesh.SamplePosition(player.transform.position, out NavMeshHit hit, 1.5f, filter))
                agent.SetDestination(hit.position);
        }

        private bool CanCatchPlayer()
        {
            Vector3 offset = player.transform.position - transform.position;
            if (Mathf.Abs(offset.y) > 1.5f) return false;
            offset.y = 0f;
            if (offset.sqrMagnitude > catchDistance * catchDistance) return false;
            Vector3 origin = transform.position + Vector3.up;
            Vector3 direction = view.transform.position - origin;
            return !Physics.Raycast(origin, direction.normalized, out RaycastHit hit,
                direction.magnitude, obstructionMask, QueryTriggerInteraction.Ignore) ||
                hit.transform == player.transform || hit.transform.IsChildOf(player.transform);
        }

        private void TransitionTo(MotionState next)
        {
            if (State == next) return;
            State = next;
            bool moving = next == MotionState.Chasing;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = !moving;
                agent.velocity = Vector3.zero;
            }
            if (animator != null) animator.speed = moving ? originalAnimationSpeed : 0f;
            if (moving) nextRepath = 0f;
        }

        public void StopMonster()
        {
            TransitionTo(MotionState.Stopped);
            StopMotion();
        }

        public void ResetMonster()
        {
            StopMotion();
            if (spawnCached && agent != null && agent.enabled && agent.isOnNavMesh)
                agent.Warp(spawnPosition);
            if (spawnCached) transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Rebind();
                animator.Update(0f);
                animator.speed = 0f;
            }
            alertRemaining = 0f;
            nextRepath = 0f;
            SetEyes(false);
            TransitionTo(MotionState.Dormant);
        }

        private void StopMotion()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;
            }
            if (animator != null) animator.speed = 0f;
        }

        private void SetEyes(bool active)
        {
            if (eye1 != null) eye1.SetActive(active);
            if (eye2 != null) eye2.SetActive(active);
        }

        private void OnDisable()
        {
            StopMonster();
            SetEyes(false);
            if (animator != null) animator.speed = originalAnimationSpeed;
        }
    }
}
