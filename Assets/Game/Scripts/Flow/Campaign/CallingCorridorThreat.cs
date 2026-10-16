using UnityEngine;
using UnityEngine.AI;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent, RequireComponent(typeof(NavMeshAgent))]
    public sealed class CallingCorridorThreat : MonoBehaviour
    {
        public enum Motion { Dormant, Warning, Chasing, Passing }
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private GameObject eye1;
        [SerializeField] private GameObject eye2;
        [SerializeField, Min(0.5f)] private float warningSeconds = 1.8f;
        [SerializeField, Min(0.1f)] private float speed = 6.6f;
        [SerializeField, Min(0.1f)] private float catchDistance = 1.15f;
        [SerializeField, Min(2.5f)] private float minimumActiveSeconds = 20f;
        private NavMeshAgent agent;
        private CallingCorridorSession session;
        private FirstPersonController player;
        private CallingCorridorStation station;
        private float elapsed;
        private float activeElapsed;
        private float hiddenTime;
        private float nextRepath;
        private Vector3 exit;
        private Vector3 spawnPosition;
        private Vector3 passingTarget;
        public Motion State { get; private set; }
        public bool IsActive => State != Motion.Dormant;
        public bool IsConfigured => agent != null && animator != null && visualRoot != null;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator != null) animator.applyRootMotion = false;
            ResetThreat();
        }

        public void Bind(CallingCorridorSession owner, FirstPersonController target)
        {
            session = owner;
            player = target;
        }

        public bool BeginEncounter(CallingCorridorStation source)
        {
            if (!IsConfigured || IsActive || player == null || source == null) return false;
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            if (!NavMesh.SamplePosition(source.ThreatSpawn.position, out NavMeshHit spawn, 1f, filter)) return false;
            if (!NavMesh.SamplePosition(source.ThreatExit.position, out NavMeshHit end, 1f, filter)) return false;
            agent.enabled = true;
            if (!agent.Warp(spawn.position)) { ResetThreat(); return false; }
            exit = end.position;
            spawnPosition = spawn.position;
            passingTarget = exit;
            station = source;
            elapsed = 0f;
            activeElapsed = 0f;
            hiddenTime = 0f;
            nextRepath = 0f;
            agent.speed = speed;
            agent.acceleration = 18f;
            agent.stoppingDistance = 0.2f;
            visualRoot.SetActive(true);
            SetEyes(true);
            station.SetWarning(true);
            TransitionTo(Motion.Warning);
            agent.SetDestination(player.transform.position);
            Debug.Log("CALLING_CORRIDOR_THREAT_WARNING", this);
            return true;
        }

        private void Update()
        {
            if (!IsActive || session == null || !session.IsPlaying || player == null || Time.deltaTime <= 0f) return;
            if (!agent.enabled || !agent.isOnNavMesh) { AbortEncounter(); return; }
            elapsed += Time.deltaTime;
            activeElapsed += Time.deltaTime;
            if (State == Motion.Warning && elapsed >= warningSeconds)
                { elapsed = 0f; TransitionTo(Motion.Chasing); }
            TickPursuit();
        }

        private void TickPursuit()
        {
            bool hidden = session.IsSheltered;
            if (!hidden && CanCatch()) { session.TryCatch(); return; }
            hiddenTime = hidden ? hiddenTime + Time.deltaTime : 0f;
            if (State == Motion.Passing && !hidden) { elapsed = 0f; TransitionTo(Motion.Chasing); }
            if (State == Motion.Chasing && hiddenTime >= 0.6f)
            {
                elapsed = 0f;
                TransitionTo(Motion.Passing);
            }
            if (Time.time >= nextRepath)
            {
                nextRepath = Time.time + 0.2f;
                agent.SetDestination(State == Motion.Passing ? passingTarget : player.transform.position);
            }
            CheckPassage();
        }

        private void CheckPassage()
        {
            if (State != Motion.Passing) return;
            if (!agent.pathPending && agent.remainingDistance < 0.6f)
            {
                if (activeElapsed >= Mathf.Max(2.5f, minimumActiveSeconds))
                {
                    ResetThreat();
                    Debug.Log("CALLING_CORRIDOR_THREAT_PASSED", this);
                    return;
                }
                passingTarget = (passingTarget - exit).sqrMagnitude < 0.01f ? spawnPosition : exit;
                elapsed = 0f;
                nextRepath = 0f;
            }
            else if (elapsed > Mathf.Max(15f, minimumActiveSeconds + 5f)) AbortEncounter();
        }

        private bool CanCatch()
        {
            Vector3 offset = player.transform.position - transform.position;
            if (Mathf.Abs(offset.y) > 1.5f) return false;
            offset.y = 0f;
            if (offset.sqrMagnitude > catchDistance * catchDistance) return false;
            Vector3 origin = transform.position + Vector3.up;
            Vector3 direction = player.PlayerCamera.transform.position - origin;
            return !Physics.Raycast(origin, direction.normalized, out RaycastHit hit,
                direction.magnitude, ~0, QueryTriggerInteraction.Ignore) || hit.transform.IsChildOf(player.transform);
        }

        private void TransitionTo(Motion next)
        {
            State = next;
            bool moving = next != Motion.Dormant;
            if (agent.enabled && agent.isOnNavMesh) agent.isStopped = !moving;
            if (animator != null) animator.speed = moving ? 1f : 0f;
            nextRepath = 0f;
        }

        private void AbortEncounter()
        {
            ResetThreat();
            Debug.LogError("CALLING_CORRIDOR_CONFIGURATION_INVALID: threat navigation lost or passage blocked", this);
        }

        public void ResetThreat()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
            if (agent != null) { agent.enabled = false; State = Motion.Dormant; }
            if (animator != null) animator.speed = 0f;
            if (station != null) station.SetWarning(false);
            if (visualRoot != null) visualRoot.SetActive(false);
            SetEyes(false);
            station = null;
            elapsed = activeElapsed = hiddenTime = nextRepath = 0f;
        }

        private void SetEyes(bool active)
        {
            if (eye1 != null) eye1.SetActive(active);
            if (eye2 != null) eye2.SetActive(active);
        }

        private void OnDisable() => ResetThreat();
    }
}
