using Zero.AnomalyPrototype.Utilities;
using Zero.AnomalyPrototype.Player;
using Zero.EnvironmentLoop;
using UnityEngine;
using UnityEngine.AI;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ObserverStatueAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform statue;
        [SerializeField] private Transform player;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private NavMeshAgent navMeshAgent;
        [SerializeField] private BoxCollider chaseBounds;
        [SerializeField] private ObserverStatueChaseTrigger chaseTrigger;
        [SerializeField] private AnomalyKillTrigger killTrigger;
        [SerializeField] private AnomalyDeathPresentation deathPresentation;
        [SerializeField] private LayerMask visibilityMask = ~0;
        [SerializeField, Min(0.05f)] private float repathInterval = 0.2f;
        [SerializeField, Min(0.05f)] private float navMeshSampleDistance = 1.2f;
        [SerializeField, Min(0.5f)] private float minimumPlayerDistance = 1.8f;

        private Vector3 normalLocalPosition;
        private Quaternion normalLocalRotation;
        private StairLoopManager loopManager;
        private FloorSegment ownerSegment;
        private Renderer[] observationRenderers;
        private float nextRepathTime;
        private bool chaseStarted;

        public bool IsCurrentlyObserved { get; private set; }
        public bool HasChaseStarted => chaseStarted;
        public bool IsAgentReady => navMeshAgent != null && navMeshAgent.enabled && navMeshAgent.isOnNavMesh;
        public FloorSegment OwnerSegment => ownerSegment;

        public void Configure(Transform statueTransform, Transform playerTransform, Camera cameraReference)
        {
            statue = statueTransform;
            observationRenderers = null;
            player = playerTransform;
            playerCamera = cameraReference;
            if (statue != null)
            {
                normalLocalPosition = statue.localPosition;
                normalLocalRotation = statue.localRotation;
            }
        }

        public void BindPlayer(Transform playerTransform, Camera cameraReference)
        {
            player = playerTransform;
            playerCamera = cameraReference;
        }

        public void BindFloorContext(StairLoopManager manager, FloorSegment segment)
        {
            loopManager = manager;
            ownerSegment = segment;
            ResolveNavigationReferences();
            DisableAgent();
            killTrigger?.Configure(this, deathPresentation);
            if (chaseTrigger != null)
            {
                chaseTrigger.Configure(this);
                chaseTrigger.SetArmed(false);
            }
        }

        public void ConfigureNavigation(
            NavMeshAgent agent,
            BoxCollider allowedBounds,
            ObserverStatueChaseTrigger startTrigger)
        {
            navMeshAgent = agent;
            chaseBounds = allowedBounds;
            chaseTrigger = startTrigger;
            chaseTrigger?.Configure(this);
        }

        public override void CaptureNormalState()
        {
            if (statue != null)
            {
                normalLocalPosition = statue.localPosition;
                normalLocalRotation = statue.localRotation;
            }
        }

        private void Update()
        {
            if (!IsActive || !chaseStarted || statue == null || player == null || playerCamera == null)
            {
                return;
            }

            if (!HasCurrentSegmentAuthority())
            {
                StopChase("segment-is-no-longer-current");
                return;
            }

            if (!IsPlayerInsideChaseBounds(player.position))
            {
                PauseAgent(true);
                nextRepathTime = 0f;
                return;
            }

            if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
            {
                StopChase("agent-left-navmesh");
                return;
            }

            IsCurrentlyObserved = IsStatueObserved();

            Vector3 toPlayer = player.position - statue.position;
            toPlayer.y = 0f;
            if (IsCurrentlyObserved ||
                toPlayer.sqrMagnitude <= minimumPlayerDistance * minimumPlayerDistance)
            {
                PauseAgent(false);
                return;
            }

            navMeshAgent.isStopped = false;
            if (Time.time < nextRepathTime)
            {
                return;
            }

            if (TrySampleCurrentNavMesh(player.position, out NavMeshHit targetHit))
            {
                navMeshAgent.SetDestination(targetHit.position);
            }

            nextRepathTime = Time.time + repathInterval;
        }

        public bool TryStartChase(FirstPersonController triggeringPlayer)
        {
            if (!IsActive || chaseStarted || triggeringPlayer == null)
            {
                return false;
            }

            ResolveNavigationReferences();
            if (!HasCurrentSegmentAuthority() || statue == null || navMeshAgent == null || chaseBounds == null)
            {
                Debug.LogWarning(
                    $"OBSERVER_CHASE_REJECTED: segment={ownerSegment?.SegmentId ?? "Unbound"}, " +
                    "reason=missing-current-authority-or-navigation-reference",
                    this);
                return false;
            }

            player = triggeringPlayer.transform;
            playerCamera = triggeringPlayer.PlayerCamera;
            if (!IsPlayerInsideChaseBounds(player.position) ||
                !TrySampleCurrentNavMesh(statue.position, out NavMeshHit spawnHit))
            {
                Debug.LogWarning(
                    $"OBSERVER_CHASE_REJECTED: segment={ownerSegment.SegmentId}, " +
                    "reason=player-or-statue-is-outside-current-floor-navmesh",
                    this);
                return false;
            }

            DisableAgent();
            statue.position = spawnHit.position;
            navMeshAgent.enabled = true;
            if (!navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.enabled = false;
                Debug.LogWarning(
                    $"OBSERVER_CHASE_REJECTED: segment={ownerSegment.SegmentId}, reason=agent-bind-failed",
                    this);
                return false;
            }

            navMeshAgent.stoppingDistance = Mathf.Max(navMeshAgent.stoppingDistance, minimumPlayerDistance);
            navMeshAgent.isStopped = false;
            chaseStarted = true;
            nextRepathTime = 0f;
            IsCurrentlyObserved = false;
            killTrigger?.SetArmed(true);
            Debug.Log($"OBSERVER_CHASE_STARTED: segment={ownerSegment.SegmentId}", this);
            return true;
        }

        protected override void OnActivate()
        {
            ResolveNavigationReferences();
            DisableAgent();
            chaseStarted = false;
            nextRepathTime = 0f;
            IsCurrentlyObserved = false;
            killTrigger?.SetArmed(false);
            chaseTrigger?.SetArmed(true);
        }

        protected override void OnResetAnomaly()
        {
            IsCurrentlyObserved = false;
            chaseStarted = false;
            killTrigger?.SetArmed(false);
            chaseTrigger?.SetArmed(false);
            DisableAgent();
            if (statue != null)
            {
                statue.localPosition = normalLocalPosition;
                statue.localRotation = normalLocalRotation;
            }
        }

        public bool IsPlayerInsideChaseBounds(Vector3 worldPosition)
        {
            if (chaseBounds == null)
            {
                return false;
            }

            Vector3 localPoint = chaseBounds.transform.InverseTransformPoint(worldPosition);
            Vector3 offset = localPoint - chaseBounds.center;
            Vector3 halfSize = chaseBounds.size * 0.5f;
            return Mathf.Abs(offset.x) <= halfSize.x &&
                   Mathf.Abs(offset.y) <= halfSize.y &&
                   Mathf.Abs(offset.z) <= halfSize.z;
        }

        public override void OnPlayerDeathAccepted()
        {
            chaseStarted = false;
            killTrigger?.SetArmed(false);
            DisableAgent();
        }

        private bool HasCurrentSegmentAuthority()
        {
            return loopManager != null && ownerSegment != null &&
                   loopManager.CurrentSegment == ownerSegment;
        }

        private bool TrySampleCurrentNavMesh(Vector3 worldPosition, out NavMeshHit hit)
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
            return NavMesh.SamplePosition(worldPosition, out hit, navMeshSampleDistance, filter);
        }

        private void PauseAgent(bool resetPath)
        {
            if (navMeshAgent == null || !navMeshAgent.enabled || !navMeshAgent.isOnNavMesh)
            {
                return;
            }

            navMeshAgent.isStopped = true;
            navMeshAgent.velocity = Vector3.zero;
            if (resetPath)
            {
                navMeshAgent.ResetPath();
            }
        }

        private bool IsStatueObserved()
        {
            if ((observationRenderers == null || observationRenderers.Length == 0) && statue != null)
            {
                observationRenderers = statue.GetComponentsInChildren<Renderer>(true);
            }

            if (!TryGetObservationBounds(out Bounds bounds))
            {
                return CameraVisibilityUtility.IsVisibleFromCamera(
                    playerCamera,
                    statue,
                    statue.position + Vector3.up * 0.65f,
                    visibilityMask);
            }

            Vector3 center = bounds.center;
            Vector3 verticalExtent = Vector3.up * bounds.extents.y * 0.75f;

            // A single point around the feet was too easy to hide behind furniture
            // while the statue's torso/head was plainly visible. Any visible sample
            // freezes the agent, matching what the player actually sees.
            return CameraVisibilityUtility.IsVisibleFromCamera(
                       playerCamera, statue, center, visibilityMask) ||
                   CameraVisibilityUtility.IsVisibleFromCamera(
                       playerCamera, statue, center + verticalExtent, visibilityMask) ||
                   CameraVisibilityUtility.IsVisibleFromCamera(
                       playerCamera, statue, center - verticalExtent, visibilityMask);
        }

        private bool TryGetObservationBounds(out Bounds combinedBounds)
        {
            combinedBounds = default;
            bool hasBounds = false;
            if (observationRenderers == null)
            {
                return false;
            }

            for (int i = 0; i < observationRenderers.Length; i++)
            {
                Renderer candidate = observationRenderers[i];
                if (candidate == null || !candidate.enabled || !candidate.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    combinedBounds = candidate.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(candidate.bounds);
                }
            }

            return hasBounds;
        }

        private void StopChase(string reason)
        {
            if (!chaseStarted)
            {
                return;
            }

            chaseStarted = false;
            killTrigger?.SetArmed(false);
            DisableAgent();
            Debug.Log(
                $"OBSERVER_CHASE_STOPPED: segment={ownerSegment?.SegmentId ?? "Unbound"}, reason={reason}",
                this);
        }

        private void DisableAgent()
        {
            if (navMeshAgent == null)
            {
                return;
            }

            PauseAgent(true);
            navMeshAgent.enabled = false;
        }

        private void ResolveNavigationReferences()
        {
            if ((observationRenderers == null || observationRenderers.Length == 0) && statue != null)
            {
                observationRenderers = statue.GetComponentsInChildren<Renderer>(true);
            }

            if (navMeshAgent == null && statue != null)
            {
                navMeshAgent = statue.GetComponent<NavMeshAgent>();
            }

            if (ownerSegment == null)
            {
                return;
            }

            deathPresentation ??= GetComponentInChildren<AnomalyDeathPresentation>(true);
            killTrigger ??= GetComponentInChildren<AnomalyKillTrigger>(true);
            if (killTrigger != null)
            {
                killTrigger.Configure(this, deathPresentation);
            }

            if (chaseTrigger == null)
            {
                chaseTrigger = ownerSegment.GetComponentInChildren<ObserverStatueChaseTrigger>(true);
            }

            if (chaseBounds == null)
            {
                BoxCollider[] colliders = ownerSegment.GetComponentsInChildren<BoxCollider>(true);
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i].name == "ObserverChaseBounds")
                    {
                        chaseBounds = colliders[i];
                        break;
                    }
                }
            }
        }
    }
}
