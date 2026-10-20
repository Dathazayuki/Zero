using UnityEngine;
using UnityEngine.AI;

namespace Zero.AnomalyPrototype.Flow
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class WatchfulHallPatrol : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform[] route;
        [SerializeField, Min(0.1f)] private float patrolSpeed = 2.2f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 5.6f;
        [SerializeField, Min(1f)] private float viewDistance = 22f;
        [SerializeField, Range(10f, 180f)] private float viewAngle = 110f;
        [SerializeField, Min(0.1f)] private float catchDistance = 1.15f;
        [SerializeField, Min(0.1f)] private float cabinetInspectionDistance = 3f;
        [SerializeField, Min(0.1f)] private float searchSeconds = 8f;
        private static readonly int Walk = Animator.StringToHash("Walk");
        private static readonly int Run = Animator.StringToHash("Run");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private WatchfulHallThreatDirector director;
        private WatchfulHallPursuitRules rules;
        private NavMeshAgent agent;
        private NavMeshPath path;
        private readonly Vector3[] corners = new Vector3[64];
        private Vector3 spawn;
        private Quaternion spawnRotation;
        private float nextSense;
        private float nextPath;
        private bool visible;
        private int waypoint;
        private int animationState;
        private Transform knownCabinet;
        private Transform head;
        public Vector3 AttackLookPoint => head != null ? head.position : EyePosition;
        public WatchfulHallPursuitRules.Phase State => rules != null ? rules.State : WatchfulHallPursuitRules.Phase.Patrolling;
        public Vector3 LastKnownPosition => rules != null ? rules.LastKnownPosition : spawn;
        public bool IsReady { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            path = new NavMeshPath();
            spawn = transform.position;
            spawnRotation = transform.rotation;
            rules = new WatchfulHallPursuitRules(searchSeconds);
        }

        public bool Bind(WatchfulHallThreatDirector owner)
        {
            director = owner;
            IsReady = animator != null && animator.runtimeAnimatorController != null && route != null && route.Length >= 2;
            if (!IsReady) return false;
            head = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            animator.applyRootMotion = false;
            return ResetPatrol();
        }

        public bool ResetPatrol()
        {
            if (!IsReady) return false;
            if (!NavMesh.SamplePosition(spawn, out NavMeshHit hit, 2f, agent.areaMask)) return IsReady = false;
            agent.enabled = true;
            if (!agent.Warp(hit.position)) return IsReady = false;
            agent.ResetPath();
            agent.isStopped = false;
            transform.rotation = spawnRotation;
            rules.Reset();
            knownCabinet = null;
            visible = false;
            waypoint = 0;
            nextSense = nextPath = 0f;
            animationState = 0;
            animator.speed = 1f;
            return true;
        }

        private void Update()
        {
            if (!IsReady || director == null || !director.IsPlaying || Time.deltaTime <= 0f) return;
            if (!agent.enabled || !agent.isOnNavMesh) return;
            if (Time.time >= nextSense) Sense();
            bool arrived = !agent.pathPending && Vector3.Distance(transform.position, rules.LastKnownPosition) < 1.2f;
            rules.Tick(visible, arrived, Time.deltaTime);
            if (Time.time >= nextPath) Move();
            if (CanCatch()) director.TryCatch(this);
        }

        private void Sense()
        {
            nextSense = Time.time + 0.15f;
            Transform cabinet = director.Hiding.FindCabinet(director.Player.transform.position);
            bool closeCabinet = CanInspect(cabinet);
            visible = closeCabinet || director.CanSee(EyePosition, transform.forward, viewDistance, viewAngle);
            if (!visible) return;
            knownCabinet = closeCabinet ? cabinet : null;
            rules.Observe(closeCabinet ? WatchfulHallHidingSpots.Entrance(cabinet) : director.Player.transform.position);
        }

        private Vector3 EyePosition => transform.position + Vector3.up * 1.8f;

        private bool CanInspect(Transform cabinet)
        {
            if (cabinet == null) return false;
            Vector3 entrance = WatchfulHallHidingSpots.Entrance(cabinet);
            if (Vector3.Distance(transform.position, entrance) > cabinetInspectionDistance) return false;
            return director.ClearLine(EyePosition, entrance + Vector3.up, null);
        }

        private bool CanCatch()
        {
            Transform cabinet = director.Hiding.FindCabinet(director.Player.transform.position);
            if (cabinet != null)
                return cabinet == knownCabinet && CanInspect(cabinet)
                    && Vector3.Distance(transform.position, WatchfulHallHidingSpots.Entrance(cabinet)) < 1.4f;
            Vector3 playerPosition = director.Player.transform.position;
            return Vector3.Distance(transform.position, playerPosition) <= catchDistance
                && director.ClearLine(EyePosition, playerPosition + Vector3.up, director.Player.transform);
        }

        private bool Move()
        {
            nextPath = Time.time + 0.2f;
            bool running = State == WatchfulHallPursuitRules.Phase.Chasing || State == WatchfulHallPursuitRules.Phase.Investigating;
            agent.isStopped = false;
            agent.speed = running ? chaseSpeed : patrolSpeed;
            Vector3 destination = Destination();
            bool accepted = NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, agent.areaMask)
                && agent.SetDestination(hit.position);
            if (!accepted) agent.ResetPath();
            int nextAnimation = running ? Run : Walk;
            if (animationState == nextAnimation) return accepted;
            animationState = nextAnimation;
            animator.speed = 1f;
            animator.CrossFade(nextAnimation, 0.15f);
            return accepted;
        }

        private Vector3 Destination()
        {
            if (State == WatchfulHallPursuitRules.Phase.Patrolling)
            {
                if (route[waypoint] == null) return spawn;
                if (Vector3.Distance(transform.position, route[waypoint].position) < 0.8f) waypoint = (waypoint + 1) % route.Length;
                return route[waypoint] != null ? route[waypoint].position : spawn;
            }
            if (State != WatchfulHallPursuitRules.Phase.Searching) return rules.LastKnownPosition;
            float angle = Mathf.Floor(rules.SearchElapsed / 2f) * Mathf.PI * 0.5f;
            return rules.LastKnownPosition + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * 2.5f;
        }

        public bool ReceiveReport(Vector3 position)
        {
            if (!IsReady || !isActiveAndEnabled || !agent.enabled || !agent.isOnNavMesh) return false;
            rules.Report(position, visible);
            return Move();
        }

        public float PathDistance(Vector3 position)
        {
            if (!IsReady || !agent.enabled || !agent.isOnNavMesh) return float.PositiveInfinity;
            if (!NavMesh.SamplePosition(position, out NavMeshHit hit, 2f, agent.areaMask)) return float.PositiveInfinity;
            if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) return float.PositiveInfinity;
            int count = path.GetCornersNonAlloc(corners);
            float distance = 0f;
            for (int i = 1; i < count; i++) distance += Vector3.Distance(corners[i - 1], corners[i]);
            return distance;
        }

        public void StopPatrol()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
            if (animator != null) animator.speed = 0f;
            visible = false;
        }

        public void PlayAttack()
        {
            if (animator == null) return;
            animator.speed = 1f;
            animator.CrossFade(Attack, 0.05f);
        }

        private void OnDisable() => StopPatrol();
    }
}
