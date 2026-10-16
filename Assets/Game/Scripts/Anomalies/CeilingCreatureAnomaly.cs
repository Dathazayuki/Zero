using System.Collections;
using Zero.AnomalyPrototype.Player;
using Zero.EnvironmentLoop;
using UnityEngine;
using UnityEngine.AI;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CeilingCreatureAnomaly : AnomalyBehaviour
    {
        private enum CreatureState
        {
            Dormant,
            CeilingCrossing,
            WaitingForArrow,
            Ambush,
            Landing,
            Chasing
        }

        [Header("Creature")]
        [SerializeField] private Transform creature;
        [SerializeField] private Animator animator;
        [SerializeField] private NavMeshAgent navMeshAgent;

        [Header("Ceiling pass")]
        [SerializeField] private CeilingCreatureEntryTrigger roomEntryTrigger;
        [SerializeField] private Transform ceilingStartPoint;
        [SerializeField] private Transform ceilingEndPoint;
        [SerializeField, Min(0.1f)] private float ceilingPassDuration = 1.4f;
        [SerializeField] private string skitterStateName = "Skitter";
        [SerializeField] private bool faceAlongCeilingPath = true;
        [SerializeField] private Vector3 ceilingFacingEulerOffset;

        [Header("Arrow ambush")]
        [SerializeField] private Transform ambushSpawnPoint;
        [SerializeField] private CorrectArrowController arrowController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LayerMask arrowObstructionMask = ~0;
        [SerializeField, Min(0f)] private float arrowTargetClearance = 0.08f;
        [SerializeField, Min(0.1f)] private float chaseLookDuration = 3f;
        [SerializeField] private string staticPoseStateName = "IdLE";

        [Header("Chase")]
        [SerializeField] private BoxCollider chaseBounds;
        [SerializeField] private string landingStateName = "Landing_1";
        [SerializeField, Min(0.1f)] private float landingMaximumDuration = 2f;
        [SerializeField] private string walkStateName = "Walk";
        [SerializeField, Min(0.05f)] private float navMeshSampleDistance = 1.5f;
        [SerializeField, Min(0.05f)] private float repathInterval = 0.2f;
        [SerializeField, Min(0.5f)] private float stoppingDistance = 1.4f;
        [SerializeField] private AnomalyKillTrigger killTrigger;
        [SerializeField] private AnomalyDeathPresentation deathPresentation;

        private Transform player;
        private StairLoopManager loopManager;
        private FloorSegment ownerSegment;
        private Coroutine ceilingRoutine;
        private Coroutine landingRoutine;
        private CreatureState state;
        private float creatureLookTimer;
        private float nextRepathTime;
        private Vector3 normalLocalPosition;
        private Quaternion normalLocalRotation;
        private bool normalActiveSelf;
        private Renderer arrowRenderer;
        private Renderer creatureRenderer;

        public string CurrentState => state.ToString();
        public float CreatureLookTime => creatureLookTimer;
        public bool IsChasing => state == CreatureState.Chasing;

        public void BindPlayer(Transform playerTransform, Camera camera, CorrectArrowController controller)
        {
            player = playerTransform;
            playerCamera = camera;
            arrowController = controller;
            arrowRenderer = controller != null && controller.ArrowTransform != null
                ? controller.ArrowTransform.GetComponentInChildren<Renderer>()
                : null;
        }

        public void BindFloorContext(StairLoopManager manager, FloorSegment segment)
        {
            loopManager = manager;
            ownerSegment = segment;
            ResolveReferences();
            DisableAgent();
            killTrigger?.Configure(this, deathPresentation);
            roomEntryTrigger?.Configure(this);
            roomEntryTrigger?.SetArmed(false);
        }

        public override void CaptureNormalState()
        {
            if (creature == null)
            {
                return;
            }

            normalLocalPosition = creature.localPosition;
            normalLocalRotation = creature.localRotation;
            normalActiveSelf = creature.gameObject.activeSelf;
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }

            if (!HasCurrentSegmentAuthority())
            {
                if (state != CreatureState.Dormant)
                {
                    StopCeilingRoutine();
                    StopLandingRoutine();
                    DisableAgent();
                    killTrigger?.SetArmed(false);
                    SetCreatureActive(false);
                    state = CreatureState.Dormant;
                    creatureLookTimer = 0f;
                    Debug.Log(
                        $"CEILING_CREATURE_STOPPED: segment={ownerSegment?.SegmentId ?? "Unbound"}, " +
                        "reason=segment-is-no-longer-current",
                        this);
                }

                return;
            }

            if (state == CreatureState.WaitingForArrow)
            {
                if (IsArrowVisible())
                {
                    SpawnAtAmbushPoint();
                }

                return;
            }

            if (state == CreatureState.Ambush)
            {
                if (!IsCreatureVisible())
                {
                    if (creatureLookTimer > 0f)
                    {
                        Debug.Log(
                            $"CEILING_CREATURE_GAZE_RESET: segment={ownerSegment.SegmentId}, " +
                            $"elapsed={creatureLookTimer:F2}",
                            this);
                    }

                    creatureLookTimer = 0f;
                    return;
                }

                if (creatureLookTimer <= 0f)
                {
                    Debug.Log($"CEILING_CREATURE_GAZE_STARTED: segment={ownerSegment.SegmentId}", this);
                }

                creatureLookTimer += Time.deltaTime;
                if (creatureLookTimer >= chaseLookDuration)
                {
                    StartChase();
                }

                return;
            }

            if (state == CreatureState.Chasing)
            {
                UpdateChase();
            }
        }

        public bool TryStartCeilingPass(FirstPersonController triggeringPlayer)
        {
            if (!IsActive || state != CreatureState.Dormant || triggeringPlayer == null ||
                !HasCurrentSegmentAuthority())
            {
                return false;
            }

            ResolveReferences();
            if (creature == null || animator == null || ceilingStartPoint == null || ceilingEndPoint == null)
            {
                Debug.LogWarning(
                    $"CEILING_CREATURE_REJECTED: segment={ownerSegment?.SegmentId ?? "Unbound"}, " +
                    "reason=missing-ceiling-pass-reference",
                    this);
                return false;
            }

            player = triggeringPlayer.transform;
            playerCamera = triggeringPlayer.PlayerCamera;
            roomEntryTrigger?.SetArmed(false);
            ceilingRoutine = StartCoroutine(CeilingPassRoutine());
            return true;
        }

        protected override void OnActivate()
        {
            ResolveReferences();
            arrowRenderer = arrowController != null && arrowController.ArrowTransform != null
                ? arrowController.ArrowTransform.GetComponentInChildren<Renderer>() : null;
            StopCeilingRoutine();
            StopLandingRoutine();
            DisableAgent();
            killTrigger?.SetArmed(false);
            state = CreatureState.Dormant;
            creatureLookTimer = 0f;
            nextRepathTime = 0f;
            SetCreatureActive(false);
            roomEntryTrigger?.SetArmed(true);

            if (creature == null || animator == null || navMeshAgent == null || roomEntryTrigger == null ||
                ceilingStartPoint == null || ceilingEndPoint == null || ambushSpawnPoint == null ||
                arrowController == null || arrowController.ArrowTransform == null || chaseBounds == null)
            {
                Debug.LogWarning(
                    $"CEILING_CREATURE_CONFIGURATION_INVALID: segment={ownerSegment?.SegmentId ?? "Unbound"}, " +
                    $"creature={creature != null}, animator={animator != null}, agent={navMeshAgent != null}, " +
                    $"trigger={roomEntryTrigger != null}, ceilingPath={ceilingStartPoint != null && ceilingEndPoint != null}, " +
                    $"ambush={ambushSpawnPoint != null}, arrow={arrowController != null && arrowController.ArrowTransform != null}, " +
                    $"bounds={chaseBounds != null}",
                    this);
            }

            Debug.Log($"CEILING_CREATURE_ARMED: segment={ownerSegment?.SegmentId ?? "Unbound"}", this);
        }

        protected override void OnResetAnomaly()
        {
            StopCeilingRoutine();
            StopLandingRoutine();
            DisableAgent();
            killTrigger?.SetArmed(false);
            state = CreatureState.Dormant;
            creatureLookTimer = 0f;
            nextRepathTime = 0f;
            roomEntryTrigger?.SetArmed(false);

            if (creature != null)
            {
                creature.localPosition = normalLocalPosition;
                creature.localRotation = normalLocalRotation;
                creature.gameObject.SetActive(normalActiveSelf);
            }
        }

        private IEnumerator CeilingPassRoutine()
        {
            state = CreatureState.CeilingCrossing;
            DisableAgent();
            SetCreatureActive(true);
            Vector3 pathDirection = ceilingEndPoint.position - ceilingStartPoint.position;
            Quaternion startRotation = ResolveCeilingPathRotation(ceilingStartPoint, pathDirection);
            Quaternion endRotation = ResolveCeilingPathRotation(ceilingEndPoint, pathDirection);
            creature.SetPositionAndRotation(ceilingStartPoint.position, startRotation);
            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.CrossFadeInFixedTime(skitterStateName, 0.05f, 0, 0f);
            Debug.Log(
                $"CEILING_CREATURE_SKITTER_STARTED: segment={ownerSegment.SegmentId}, " +
                $"pathDirection={pathDirection.normalized}, rotation={startRotation.eulerAngles}",
                this);

            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, ceilingPassDuration);
            while (elapsed < duration && IsActive && HasCurrentSegmentAuthority())
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                creature.position = Vector3.Lerp(ceilingStartPoint.position, ceilingEndPoint.position, t);
                creature.rotation = Quaternion.Slerp(startRotation, endRotation, t);
                yield return null;
            }

            ceilingRoutine = null;
            SetCreatureActive(false);
            state = CreatureState.WaitingForArrow;
            Debug.Log($"CEILING_CREATURE_SKITTER_COMPLETED: segment={ownerSegment.SegmentId}", this);
        }

        private void SpawnAtAmbushPoint()
        {
            DisableAgent();
            SetCreatureActive(true);
            creatureRenderer = creature.GetComponentInChildren<Renderer>();
            creature.SetPositionAndRotation(ambushSpawnPoint.position, ambushSpawnPoint.rotation);

            animator.enabled = true;
            animator.applyRootMotion = false;
            if (!string.IsNullOrEmpty(staticPoseStateName))
            {
                animator.Play(staticPoseStateName, 0, 0f);
                animator.Update(0f);
            }

            animator.enabled = false;
            creatureLookTimer = 0f;
            state = CreatureState.Ambush;
            Debug.Log($"CEILING_CREATURE_AMBUSH_SPAWNED: segment={ownerSegment.SegmentId}", this);
        }

        private void StartChase()
        {
            if (!TrySampleNavMesh(ambushSpawnPoint.position, out NavMeshHit spawnHit))
            {
                creatureLookTimer = 0f;
                Debug.LogWarning(
                    $"CEILING_CREATURE_CHASE_REJECTED: segment={ownerSegment.SegmentId}, " +
                    "reason=ambush-point-is-off-navmesh",
                    this);
                return;
            }

            DisableAgent();
            creature.position = spawnHit.position;
            state = CreatureState.Landing;
            creatureLookTimer = 0f;
            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.CrossFadeInFixedTime(landingStateName, 0.05f, 0, 0f);
            landingRoutine = StartCoroutine(LandingBeforeChaseRoutine());
            Debug.Log($"CEILING_CREATURE_LANDING_STARTED: segment={ownerSegment.SegmentId}", this);
        }

        private IEnumerator LandingBeforeChaseRoutine()
        {
            int landingStateHash = Animator.StringToHash(landingStateName);
            bool enteredLandingState = false;
            float elapsed = 0f;
            float timeout = Mathf.Max(0.1f, landingMaximumDuration);

            while (elapsed < timeout && IsActive && HasCurrentSegmentAuthority() &&
                   state == CreatureState.Landing)
            {
                elapsed += Time.deltaTime;
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.shortNameHash == landingStateHash)
                {
                    enteredLandingState = true;
                    if (stateInfo.normalizedTime >= 0.98f)
                    {
                        break;
                    }
                }

                yield return null;
            }

            landingRoutine = null;
            if (!IsActive || !HasCurrentSegmentAuthority() || state != CreatureState.Landing)
            {
                yield break;
            }

            if (!enteredLandingState)
            {
                Debug.LogWarning(
                    $"CEILING_CREATURE_LANDING_STATE_NOT_FOUND: segment={ownerSegment.SegmentId}, " +
                    $"state={landingStateName}",
                    this);
            }

            Debug.Log($"CEILING_CREATURE_LANDING_COMPLETED: segment={ownerSegment.SegmentId}", this);
            BeginNavMeshChase();
        }

        private void BeginNavMeshChase()
        {
            if (!TrySampleNavMesh(creature.position, out NavMeshHit spawnHit))
            {
                ReturnToAmbushAfterChaseFailure("landing-position-is-off-navmesh");
                return;
            }

            creature.position = spawnHit.position;
            navMeshAgent.enabled = true;
            if (!navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.enabled = false;
                ReturnToAmbushAfterChaseFailure("agent-bind-failed");
                return;
            }

            navMeshAgent.stoppingDistance = Mathf.Max(navMeshAgent.stoppingDistance, stoppingDistance);
            navMeshAgent.isStopped = false;
            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.CrossFadeInFixedTime(walkStateName, 0.1f, 0, 0f);
            state = CreatureState.Chasing;
            nextRepathTime = 0f;
            killTrigger?.SetArmed(true);
            Debug.Log($"CEILING_CREATURE_CHASE_STARTED: segment={ownerSegment.SegmentId}", this);
        }

        private void ReturnToAmbushAfterChaseFailure(string reason)
        {
            creatureLookTimer = 0f;
            state = CreatureState.Ambush;
            killTrigger?.SetArmed(false);
            animator.enabled = true;
            if (!string.IsNullOrEmpty(staticPoseStateName))
            {
                animator.Play(staticPoseStateName, 0, 0f);
                animator.Update(0f);
            }

            animator.enabled = false;
            Debug.LogWarning(
                $"CEILING_CREATURE_CHASE_REJECTED: segment={ownerSegment.SegmentId}, reason={reason}",
                this);
        }

        private void UpdateChase()
        {
            if (player == null || navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
            {
                return;
            }

            if (!IsInsideBounds(player.position))
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
                return;
            }

            navMeshAgent.isStopped = false;
            if (Time.time < nextRepathTime)
            {
                return;
            }

            if (TrySampleNavMesh(player.position, out NavMeshHit playerHit))
            {
                navMeshAgent.SetDestination(playerHit.position);
            }

            nextRepathTime = Time.time + repathInterval;
        }

        private bool IsArrowVisible()
        {
            if (playerCamera == null || arrowController == null || arrowController.ArrowTransform == null)
            {
                return false;
            }

            return AnomalyGazeVisibility.IsVisible(playerCamera, arrowController.ArrowTransform,
                arrowRenderer, arrowObstructionMask, arrowTargetClearance);
        }

        private bool IsCreatureVisible()
        {
            return creature != null && creature.gameObject.activeInHierarchy &&
                   AnomalyGazeVisibility.IsVisible(playerCamera, creature, creatureRenderer,
                       arrowObstructionMask, arrowTargetClearance);
        }

        private Quaternion ResolveCeilingPathRotation(Transform pathPoint, Vector3 pathDirection)
        {
            Quaternion offset = Quaternion.Euler(ceilingFacingEulerOffset);
            if (!faceAlongCeilingPath || pathDirection.sqrMagnitude <= 0.0001f)
            {
                return pathPoint.rotation * offset;
            }

            Vector3 up = pathPoint.up;
            Vector3 forward = Vector3.ProjectOnPlane(pathDirection, up);
            if (forward.sqrMagnitude <= 0.0001f)
            {
                return pathPoint.rotation * offset;
            }

            return Quaternion.LookRotation(forward.normalized, up) * offset;
        }

        private bool TrySampleNavMesh(Vector3 position, out NavMeshHit hit)
        {
            hit = default;
            if (navMeshAgent == null)
            {
                return false;
            }

            var filter = new NavMeshQueryFilter
            {
                agentTypeID = navMeshAgent.agentTypeID,
                areaMask = navMeshAgent.areaMask
            };
            return NavMesh.SamplePosition(position, out hit, navMeshSampleDistance, filter);
        }

        private bool IsInsideBounds(Vector3 position)
        {
            if (chaseBounds == null)
            {
                return false;
            }

            Vector3 localPoint = chaseBounds.transform.InverseTransformPoint(position);
            Vector3 offset = localPoint - chaseBounds.center;
            Vector3 halfSize = chaseBounds.size * 0.5f;
            return Mathf.Abs(offset.x) <= halfSize.x &&
                   Mathf.Abs(offset.y) <= halfSize.y &&
                   Mathf.Abs(offset.z) <= halfSize.z;
        }

        private bool HasCurrentSegmentAuthority()
        {
            return loopManager != null && ownerSegment != null && loopManager.CurrentSegment == ownerSegment;
        }

        public override void OnPlayerDeathAccepted()
        {
            StopCeilingRoutine();
            StopLandingRoutine();
            killTrigger?.SetArmed(false);
            DisableAgent();
            if (animator != null)
            {
                animator.enabled = false;
            }
        }

        private void ResolveReferences()
        {
            if (creature != null)
            {
                animator ??= creature.GetComponentInChildren<Animator>(true);
                navMeshAgent ??= creature.GetComponent<NavMeshAgent>();
            }

            if (roomEntryTrigger == null)
            {
                roomEntryTrigger = GetComponentInChildren<CeilingCreatureEntryTrigger>(true);
            }

            deathPresentation ??= GetComponentInChildren<AnomalyDeathPresentation>(true);
            killTrigger ??= GetComponentInChildren<AnomalyKillTrigger>(true);
            if (killTrigger != null)
            {
                killTrigger.Configure(this, deathPresentation);
            }

            if (ownerSegment == null)
            {
                return;
            }

            if (chaseBounds == null)
            {
                BoxCollider[] colliders = ownerSegment.GetComponentsInChildren<BoxCollider>(true);
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i].name == "CeilingCreatureChaseBounds" ||
                        colliders[i].name == "ObserverChaseBounds")
                    {
                        chaseBounds = colliders[i];
                        break;
                    }
                }
            }

            roomEntryTrigger?.Configure(this);
        }

        private void DisableAgent()
        {
            if (navMeshAgent == null)
            {
                return;
            }

            if (navMeshAgent.enabled && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.isStopped = true;
                navMeshAgent.ResetPath();
                navMeshAgent.velocity = Vector3.zero;
            }

            navMeshAgent.enabled = false;
        }

        private void SetCreatureActive(bool active)
        {
            if (creature != null)
            {
                creature.gameObject.SetActive(active);
            }
        }

        private void StopCeilingRoutine()
        {
            if (ceilingRoutine == null)
            {
                return;
            }

            StopCoroutine(ceilingRoutine);
            ceilingRoutine = null;
        }

        private void StopLandingRoutine()
        {
            if (landingRoutine == null)
            {
                return;
            }

            StopCoroutine(landingRoutine);
            landingRoutine = null;
        }
    }
}
