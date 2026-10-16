using UnityEngine;
using UnityEngine.AI;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class BackroomMonsterController : MonoBehaviour
    {
        private SourceSoundPlayback soundPlayback;
        public enum MonsterState { Dormant, Chasing, Caught, Escaped }
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform focusTarget;
        [SerializeField] private string chaseState = "Base Layer.HorrorGirl";
        [SerializeField, Min(0.1f)] private float chaseSpeed = 4.6f;
        [SerializeField, Min(0.1f)] private float catchDistance = 1.4f;
        [SerializeField] private LayerMask obstructionMask = ~0;

        [Header("Audio")]
        [SerializeField] private AudioSource footstepAudioSource;
        [SerializeField] private AudioClip footstepAudioClip;
        [SerializeField, Min(0.1f)] private float footstepInterval = 0.38f;

        private NavMeshAgent agent;
        private FirstPersonController player;
        private BackroomGameplayManager manager;
        private float nextRepath;
        private float footstepTimer;
        private bool faultLogged;
        private readonly RaycastHit[] visibilityHits = new RaycastHit[24];
        public MonsterState State { get; private set; }
        public Transform FocusTarget => focusTarget != null ? focusTarget : transform;

        public void Configure(Transform spawn, Animator animation, Transform focus, AudioClip footstep = null)
        {
            spawnPoint = spawn;
            animator = animation;
            focusTarget = focus;
            if (footstep != null) footstepAudioClip = footstep;
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (animator != null) { animator.applyRootMotion = false; animator.speed = 0f; }
            agent.enabled = false;

            if (footstepAudioSource == null)
            {
                footstepAudioSource = GetComponent<AudioSource>();
                if (footstepAudioSource == null)
                {
                    footstepAudioSource = gameObject.AddComponent<AudioSource>();
                    footstepAudioSource.playOnAwake = false;
                    footstepAudioSource.spatialBlend = 1f;
                    footstepAudioSource.rolloffMode = AudioRolloffMode.Linear;
                    footstepAudioSource.minDistance = 2f;
                    footstepAudioSource.maxDistance = 25f;
                }
            }
            footstepAudioClip = GameAudioBootstrap.ResolveDefaultClip(footstepAudioClip, "clip.HorrorGirl_FootStep");
        }

        public void Bind(BackroomGameplayManager owner, FirstPersonController target)
        { manager = owner; player = target; }

        public bool ResetMonster()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            StopAgent();
            agent.enabled = false;
            State = MonsterState.Dormant;
            if (spawnPoint == null || !NavMesh.SamplePosition(spawnPoint.position, out var hit, 2f,
                    new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask }))
            {
                Debug.LogError("BACKROOM_MONSTER_SPAWN_INVALID: spawn must be within 2m of the correct NavMesh.", this);
                return false;
            }
            transform.SetPositionAndRotation(hit.position, spawnPoint.rotation);
            agent.enabled = true;
            if (!agent.isOnNavMesh || !agent.Warp(hit.position)) { agent.enabled = false; return false; }
            agent.isStopped = true;
            faultLogged = false;
            return true;
        }

        public bool StartChase()
        {
            if (State != MonsterState.Dormant || player == null || agent == null || !agent.isOnNavMesh) return false;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = 0.2f;
            agent.isStopped = false;
            State = MonsterState.Chasing;
            nextRepath = 0f;
            if (animator != null)
            {
                animator.speed = 1f;
                int hash = Animator.StringToHash(chaseState);
                if (animator.HasState(0, hash)) animator.CrossFadeInFixedTime(hash, 0.1f);
            }
            Debug.Log("BACKROOM_CHASE_STARTED", this);
            return true;
        }

        public void StopChase() { StopAgent(); State = MonsterState.Escaped; }
        public void MarkCaught() { StopAgent(); State = MonsterState.Caught; }
        private void StopAgent()
        {
            if (agent != null && agent.enabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
            if (animator != null) animator.speed = 0f;
            footstepTimer = 0f;
            if (footstepAudioSource != null && footstepAudioSource.isPlaying) footstepAudioSource.Stop();
        }

        private void Update()
        {
            if (State != MonsterState.Chasing || player == null || manager == null) return;
            if (!agent.enabled || !agent.isOnNavMesh)
            {
                if (!faultLogged) { Debug.LogError("BACKROOM_MONSTER_NAVMESH_LOST", this); faultLogged = true; }
                return;
            }

            UpdateFootsteps();
            UpdateDestination();
            Vector3 delta = player.transform.position - transform.position;
            if (Mathf.Abs(delta.y) <= 1.5f && new Vector2(delta.x, delta.z).sqrMagnitude <= catchDistance * catchDistance && CanSeePlayer())
                manager.TryCatchPlayer(this);
        }

        private void UpdateDestination()
        {
            if (Time.time >= nextRepath)
            {
                nextRepath = Time.time + 0.15f;
                if (NavMesh.SamplePosition(player.transform.position, out var target, 1.5f,
                        new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask }))
                    agent.SetDestination(target.position);
            }
        }

        private void UpdateFootsteps()
        {
            if (agent.velocity.sqrMagnitude <= 0.05f || footstepAudioClip == null || footstepAudioSource == null)
            {
                footstepTimer = 0f;
                return;
            }
            footstepTimer += Time.deltaTime;
            if (footstepTimer < footstepInterval) return;
            footstepTimer = 0f;
            GameAudioBootstrap.BindSource(ref soundPlayback, footstepAudioSource).Play(null, footstepAudioClip);
        }

        private bool CanSeePlayer()
        {
            Vector3 origin = transform.position + Vector3.up;
            Vector3 target = player.transform.position + Vector3.up;
            Vector3 direction = target - origin;
            int count = Physics.RaycastNonAlloc(origin, direction.normalized, visibilityHits, direction.magnitude,
                obstructionMask, QueryTriggerInteraction.Ignore);
            if (count == visibilityHits.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Transform hit = visibilityHits[i].transform;
                if (!hit.IsChildOf(transform) && !hit.IsChildOf(player.transform)) return false;
            }
            return true;
        }

        private void OnDisable() { StopAgent(); }
    }
}
