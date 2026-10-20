using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class SeatedFatherAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Animator actor;
        [SerializeField] private Transform observer;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private AnomalyDeathPresentation presentation;
        [SerializeField] private Transform seatAnchor;
        [SerializeField, Tooltip("Drag Father's head bone here. If empty, uses the Humanoid Head bone.")] private Transform headBone;
        [SerializeField, Min(0.1f)] private float warningSeconds = 2.5f;
        [SerializeField, Min(0.1f)] private float triggerDistance = 1.8f;
        [SerializeField, Min(0.1f)] private float lungeSpeed = 4f;
        [SerializeField] private LayerMask obstacles = ~0;
        private static readonly int Sit = Animator.StringToHash("Sit");
        private static readonly int Rise = Animator.StringToHash("SitToStand");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private SeatedFatherRules rules;
        private Vector3 position, lungeTarget, seatedPosition;
        private Quaternion rotation, headRotation;
        private Transform[] bones;
        private Vector3[] bonePositions;
        private Quaternion[] boneRotations;
        [System.NonSerialized] private bool captured;
        public FatherState State => rules != null ? rules.State : FatherState.Sitting;
        public Animator Actor => actor;
        public Transform HeadBone => headBone;
        public bool IsConfigured => actor != null && observer != null && death != null && presentation != null && seatAnchor != null;

        public void Configure(Animator model, Transform camera, PlayerDeathController controller, AnomalyDeathPresentation view, Transform seat)
        {
            actor = model;
            observer = camera;
            death = controller;
            presentation = view;
            seatAnchor = seat;
            if (headBone == null && actor != null) headBone = actor.GetBoneTransform(HumanBodyBones.Head);
        }

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            position = actor.transform.position;
            rotation = actor.transform.rotation;
            if (headBone == null) headBone = actor.GetBoneTransform(HumanBodyBones.Head);
            rules = new SeatedFatherRules(warningSeconds);
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
            if (!captured || !IsConfigured) return;
            rules.Reset();
            actor.transform.SetPositionAndRotation(position, rotation);
            actor.gameObject.SetActive(true);
            actor.Rebind();
            actor.Play(Sit, 0, 0.45f);
            actor.Update(0f);
            Transform hips = actor.GetBoneTransform(HumanBodyBones.Hips);
            actor.transform.position += seatAnchor.position - hips.position;
            seatedPosition = actor.transform.position;
            if (headBone != null) headRotation = headBone.localRotation;
        }

        private void Update()
        {
            if (!IsActive || !captured || !IsConfigured) return;
            if (rules.State == FatherState.Caught)
            {
                if (!death.IsDeathInProgress) ResetAnomaly();
                return;
            }
            if (death.IsDeathInProgress) return;
            AdvanceThreat();
        }

        private void AdvanceThreat()
        {
            Vector3 offset = observer.position - actor.transform.position;
            offset.y = 0f;
            bool visible = ClearSight();
            FatherState previous = rules.State;
            rules.Tick(Time.deltaTime, offset.sqrMagnitude <= triggerDistance * triggerDistance && visible,
                offset.sqrMagnitude <= 4.84f && visible);
            if (previous != rules.State) PresentState();
            if (rules.State == FatherState.Lunging) Lunge();
            if (rules.State == FatherState.Returning) ReturnToSeat();
        }

        private void PresentState()
        {
            if (rules.State == FatherState.Rising) actor.CrossFadeInFixedTime(Rise, 0.12f);
            if (rules.State != FatherState.Lunging) return;
            actor.CrossFadeInFixedTime(Attack, 0.08f);
            lungeTarget = observer.position;
            lungeTarget.y = actor.transform.position.y;
            Vector3 direction = lungeTarget - actor.transform.position;
            if (direction.sqrMagnitude > 0.01f) actor.transform.rotation = Quaternion.LookRotation(direction);
        }

        private void LateUpdate()
        {
            if (!IsActive || headBone == null || observer == null || rules == null) return;
            if (rules.State != FatherState.Warning && rules.State != FatherState.Sitting) return;
            Quaternion rest = headBone.parent.rotation * headRotation;
            Vector3 direction = observer.position - headBone.position;
            if (direction.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(direction, Vector3.up)
                * Quaternion.Inverse(Quaternion.LookRotation(actor.transform.forward, Vector3.up)) * rest;
            float blend = rules.State == FatherState.Warning ? Mathf.SmoothStep(0f, 1f, rules.Elapsed / rules.WarningSeconds) : 0f;
            headBone.rotation = Quaternion.Slerp(rest, target, blend);
        }

        private bool ClearSight()
        {
            Vector3 origin = headBone != null ? headBone.position : actor.transform.position + Vector3.up * 1.4f;
            if (!Physics.Linecast(origin, observer.position, out RaycastHit hit, obstacles, QueryTriggerInteraction.Ignore)) return true;
            return observer.IsChildOf(hit.transform) || hit.transform.IsChildOf(observer.root);
        }

        private void Lunge()
        {
            Vector3 offset = lungeTarget - actor.transform.position;
            float step = Mathf.Min(lungeSpeed * Time.deltaTime, offset.magnitude);
            Vector3 direction = offset.sqrMagnitude > 0.001f ? offset.normalized : Vector3.zero;
            Vector3 origin = actor.transform.position + Vector3.up * 0.9f;
            if (Physics.SphereCast(origin, 0.22f, direction, out RaycastHit hit, step, obstacles, QueryTriggerInteraction.Ignore))
                step = Mathf.Max(0f, hit.distance - 0.025f);
            actor.transform.position += direction * step;
            Vector3 playerOffset = observer.position - actor.transform.position;
            playerOffset.y = 0f;
            if (playerOffset.sqrMagnitude <= 0.65f * 0.65f && ClearSight()) death.TryBeginDeath(this, presentation);
        }

        private void ReturnToSeat()
        {
            actor.transform.position = Vector3.MoveTowards(actor.transform.position, seatedPosition, 2.5f * Time.deltaTime);
            actor.transform.rotation = Quaternion.RotateTowards(actor.transform.rotation, rotation, 240f * Time.deltaTime);
            if (Vector3.Distance(actor.transform.position, seatedPosition) > 0.01f) return;
            actor.Play(Sit, 0, 0.45f);
            rules.Returned();
        }

        public override void OnPlayerDeathAccepted() => rules?.Catch();

        protected override void OnResetAnomaly()
        {
            rules?.Reset();
            if (!captured || actor == null) return;
            actor.transform.SetPositionAndRotation(position, rotation);
            actor.gameObject.SetActive(false);
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                bones[i].localPosition = bonePositions[i];
                bones[i].localRotation = boneRotations[i];
            }
        }

        private void OnDisable() => ResetAnomaly();
    }
}
