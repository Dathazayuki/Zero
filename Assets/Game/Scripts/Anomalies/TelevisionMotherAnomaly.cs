using UnityEngine;
using UnityEngine.AI;
using Zero.AnomalyPrototype.Player;
using TelevisionMotherPhase = Zero.AnomalyPrototype.Anomalies.TelevisionMotherRules.PhaseState;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class TelevisionMotherAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Animator actor;
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Transform observer;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private AnomalyDeathPresentation presentation;
        [SerializeField] private TelevisionFeed feed;
        [SerializeField] private BoxCollider roomBounds;
        [SerializeField] private Vector3 landingPosition;
        [SerializeField, Min(0.1f)] private float warningSeconds = 3f;
        [SerializeField, Min(0.1f)] private float emergeSeconds = 1.4f;
        [SerializeField, Min(1f)] private float chaseSeconds = 20f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 2.7f;
        [SerializeField, HideInInspector] private float lungeSpeed = 5.5f;
        private static readonly int Run = Animator.StringToHash("RunNormal");
        private static readonly int Lunge = Animator.StringToHash("RunLunge");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private TelevisionMotherRules rules;
        private Transform head;
        private Vector3 startPosition, startScale;
        private Quaternion startRotation;
        private Transform[] bones;
        private Vector3[] bonePositions;
        private Quaternion[] boneRotations;
        private float nextRepath;
        [System.NonSerialized] private bool captured;
        public bool IsConfigured => actor != null && agent != null && observer != null && death != null
            && presentation != null && feed != null && roomBounds != null;

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            startPosition = actor.transform.position;
            startRotation = actor.transform.rotation;
            startScale = actor.transform.localScale;
            head = actor.GetBoneTransform(HumanBodyBones.Head);
            rules = new TelevisionMotherRules(warningSeconds, emergeSeconds, chaseSeconds);
            bones = actor.GetComponentsInChildren<Transform>(true);
            bonePositions = new Vector3[bones.Length];
            boneRotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                bonePositions[i] = bones[i].localPosition;
                boneRotations[i] = bones[i].localRotation;
            }
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured) return;
            rules.Reset();
            feed.Begin(false);
        }

        private void Update()
        {
            if (!IsActive || !captured || !IsConfigured) return;
            if (rules.Phase == TelevisionMotherPhase.Caught)
            {
                if (!death.IsDeathInProgress) ResetAnomaly();
                return;
            }
            if (death.IsDeathInProgress) return;
            Advance();
        }

        private void Advance()
        {
            Vector3 offset = observer.root.position - actor.transform.position;
            offset.y = 0f;
            var previous = rules.Phase;
            Vector3 local = roomBounds.transform.InverseTransformPoint(observer.root.position);
            bool inside = new Bounds(roomBounds.center, roomBounds.size).Contains(local);
            rules.Tick(Time.deltaTime, inside, offset.sqrMagnitude < 1.8f * 1.8f);
            if (rules.Phase != previous) EnterPhase();
            if (rules.Phase == TelevisionMotherPhase.Warning) feed.SetTension(rules.WarningProgress);
            else if (rules.Phase == TelevisionMotherPhase.Emerging) Emerge();
            else if (rules.Phase == TelevisionMotherPhase.Chasing) Pursue();
        }

        private void EnterPhase()
        {
            switch (rules.Phase)
            {
                case TelevisionMotherPhase.Emerging:
                    actor.gameObject.SetActive(true);
                    actor.Rebind();
                    actor.Play(Lunge, 0, 0f);
                    break;
                case TelevisionMotherPhase.Chasing:
                    BeginChase();
                    break;
                case TelevisionMotherPhase.Finished:
                    ResetAnomaly();
                    break;
            }
        }

        private void Emerge()
        {
            float t = Mathf.SmoothStep(0f, 1f, rules.EmergenceProgress);
            actor.transform.position = Vector3.Lerp(startPosition, landingPosition, t);
            actor.transform.localScale = startScale * Mathf.Lerp(0.25f, 1f, t);
            if (t > 0.65f) feed.Stop();
        }

        private void BeginChase()
        {
            feed.Stop();
            if (!agent.enabled)
            {
                if (!NavMesh.SamplePosition(landingPosition, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                {
                    Debug.LogWarning("IR2_TV_NAVIGATION_UNAVAILABLE", this);
                    ResetAnomaly();
                    return;
                }
                actor.transform.position = hit.position;
                actor.transform.localScale = startScale;
                agent.enabled = true;
                agent.Warp(hit.position);
            }
            actor.CrossFadeInFixedTime(Run, 0.12f);
            agent.speed = chaseSpeed;
            nextRepath = 0f;
        }

        private void Pursue()
        {
            if (!agent.enabled || !agent.isOnNavMesh) return;
            if (Time.time >= nextRepath)
            {
                nextRepath = Time.time + 0.2f;
                agent.SetDestination(observer.root.position);
            }
            Vector3 offset = observer.root.position - actor.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > 0.7f * 0.7f) return;
            if (Physics.Linecast(head.position, observer.position, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(observer.root)) return;
            death.TryBeginDeath(this, presentation);
        }

        public override void OnPlayerDeathAccepted()
        {
            if (rules == null || rules.Phase == TelevisionMotherPhase.Caught) return;
            rules.Catch();
            if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
            Vector3 direction = observer.position - actor.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f) actor.transform.rotation = Quaternion.LookRotation(direction);
            actor.CrossFadeInFixedTime(Attack, 0.05f, 0, 0f);
        }

        protected override void OnResetAnomaly()
        {
            feed?.Stop();
            rules?.Reset();
            if (!captured || actor == null) return;
            if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
            agent.enabled = false;
            actor.gameObject.SetActive(false);
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].localPosition = bonePositions[i];
                bones[i].localRotation = boneRotations[i];
            }
            actor.transform.SetPositionAndRotation(startPosition, startRotation);
            actor.transform.localScale = startScale;
        }

        private void OnDisable() => ResetAnomaly();
    }
}
