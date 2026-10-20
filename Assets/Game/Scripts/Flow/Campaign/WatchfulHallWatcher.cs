using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class WatchfulHallWatcher : MonoBehaviour
    {
        [SerializeField] private Transform eye;
        [SerializeField] private Light gazeLight;
        [SerializeField] private Transform neckBone;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform patrolStart;
        [SerializeField] private Transform patrolEnd;
        [SerializeField, Min(1f)] private float viewDistance = 65f;
        [SerializeField, Range(10f, 180f)] private float viewAngle = 100f;
        [SerializeField, Min(0.1f)] private float detectionSeconds = 1.5f;
        [SerializeField, Min(0f)] private float scanDegreesPerSecond = 15f;
        [SerializeField, Range(0f, 90f)] private float scanHalfAngle = 85f;
        [SerializeField, Min(0f)] private float patrolSpeed = 2.4f;
        [SerializeField, Min(0f)] private float turnDegreesPerSecond = 120f;
        [SerializeField, Min(0.01f)] private float walkAnimationSpeed = 0.45f;
        private static readonly int Walk = Animator.StringToHash("Walk");
        private WatchfulHallThreatDirector director;
        private WatchfulHallPatrol responder;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Quaternion initialNeckRotation;
        private Quaternion scanCenterRotation;
        private Quaternion animatedNeckRotation;
        private Transform headBone;
        private Quaternion initialHeadRotation;
        private Quaternion animatedHeadRotation;
        private Color initialLightColor;
        private float observedSeconds;
        private float nextReport;
        private float scanSeconds;
        private bool movingToEnd = true;
        private bool ownsNeckPose;
        private bool hasSpawnPose;
        public bool IsAlerted { get; private set; }
        public bool IsConfigured => eye != null && gazeLight != null && neckBone != null
            && eye.IsChildOf(neckBone) && animator != null && animator.runtimeAnimatorController != null
            && !animator.applyRootMotion && HasPatrolRoute;
        private bool HasPatrolRoute => patrolStart != null && patrolEnd != null
            && !patrolStart.IsChildOf(transform) && !patrolEnd.IsChildOf(transform)
            && (patrolEnd.position - patrolStart.position).sqrMagnitude > 0.01f;

        private void Awake()
        {
            initialPosition = transform.localPosition;
            initialRotation = transform.localRotation;
            if (neckBone != null)
            {
                initialNeckRotation = neckBone.localRotation;
                scanCenterRotation = neckBone.rotation;
            }
            if (animator != null && animator.isHuman)
                headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (headBone != null) initialHeadRotation = headBone.localRotation;
            if (gazeLight != null) initialLightColor = gazeLight.color;
            hasSpawnPose = true;
        }

        public void Bind(WatchfulHallThreatDirector owner)
        {
            director = owner;
            ResetWatcher();
        }

        private void Update()
        {
            if (Time.deltaTime <= 0f) return;
            RestoreNeckPose();
            if (director == null || !director.IsPlaying || !IsConfigured) return;
            scanSeconds += Time.deltaTime;
            animator.speed = walkAnimationSpeed;
            PatrolGallery();
        }

        private void PatrolGallery()
        {
            if (patrolSpeed <= 0f) return;
            Transform destination = movingToEnd ? patrolEnd : patrolStart;
            Vector3 direction = destination.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) { movingToEnd = !movingToEnd; return; }
            Quaternion facing = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing,
                turnDegreesPerSecond * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, facing) > 10f) return;
            transform.position = Vector3.MoveTowards(transform.position, destination.position, patrolSpeed * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (director == null || !director.IsPlaying || !IsConfigured || Time.deltaTime <= 0f) return;
            // Own the neck/head pose after Walk has evaluated, preserving the rest of locomotion.
            animatedNeckRotation = neckBone.localRotation;
            if (headBone != null)
            {
                animatedHeadRotation = headBone.localRotation;
                headBone.localRotation = initialHeadRotation;
            }
            float yaw = WatchfulHallPursuitRules.WatcherScanYaw(scanSeconds, scanHalfAngle, scanDegreesPerSecond);
            // Keep the authored world-space scan area stable while body and Spine animate/turn.
            neckBone.rotation = Quaternion.AngleAxis(yaw, Vector3.up) * scanCenterRotation;
            ownsNeckPose = true;
            ObservePlayer();
        }

        private void ObservePlayer()
        {
            if (!CanSeePlayer()) { ClearNotice(); return; }
            observedSeconds += Time.deltaTime;
            if (observedSeconds >= detectionSeconds && Time.time >= nextReport)
            {
                responder = director.ReportWatcher(director.Player.transform.position, responder);
                IsAlerted = responder != null;
                nextReport = Time.time + 0.35f;
            }
            gazeLight.color = IsAlerted ? Color.red
                : Color.Lerp(initialLightColor, Color.yellow, Mathf.Clamp01(observedSeconds / detectionSeconds));
        }

        private bool CanSeePlayer()
        {
            if (director.Player == null || !gazeLight.isActiveAndEnabled
                || gazeLight.type != LightType.Spot || gazeLight.intensity <= 0f) return false;
            Transform player = director.Player.transform;
            if (director.Hiding.FindCabinet(player.position) != null) return false;
            Vector3 target = player.position + Vector3.up * 0.05f;
            Transform source = gazeLight.transform;
            float range = Mathf.Min(viewDistance, gazeLight.range);
            float angle = Mathf.Min(viewAngle, gazeLight.innerSpotAngle);
            return WatchfulHallPursuitRules.IsInsideViewCone(source.position, source.forward, target, range, angle)
                && director.ClearLine(source.position, target, player);
        }

        public void ClearNotice()
        {
            observedSeconds = 0f;
            nextReport = 0f;
            responder = null;
            IsAlerted = false;
            if (gazeLight != null) gazeLight.color = initialLightColor;
        }

        public void ResetWatcher()
        {
            if (!hasSpawnPose) return;
            StopWatcher();
            transform.localPosition = initialPosition;
            transform.localRotation = initialRotation;
            if (neckBone != null) neckBone.localRotation = initialNeckRotation;
            if (headBone != null) headBone.localRotation = initialHeadRotation;
            scanSeconds = 0f;
            movingToEnd = true;
            if (!IsConfigured || !animator.isActiveAndEnabled) return;
            animator.Play(Walk, 0, 0f);
            animator.speed = walkAnimationSpeed;
        }

        private void RestoreNeckPose()
        {
            if (ownsNeckPose && neckBone != null) neckBone.localRotation = animatedNeckRotation;
            if (ownsNeckPose && headBone != null) headBone.localRotation = animatedHeadRotation;
            ownsNeckPose = false;
        }

        public void StopWatcher()
        {
            if (!hasSpawnPose) return;
            ClearNotice();
            RestoreNeckPose();
            if (animator != null) animator.speed = 0f;
        }

        private void OnDisable()
        {
            ResetWatcher();
            StopWatcher();
        }
    }
}
