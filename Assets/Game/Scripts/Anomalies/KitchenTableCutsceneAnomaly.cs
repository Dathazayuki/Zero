using UnityEngine;
using Zero.AnomalyPrototype.Player;
using Phase = Zero.AnomalyPrototype.Anomalies.KitchenTableCutsceneRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenTableCutsceneAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Animator actor;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private PlayerDeathController death;
        [SerializeField] private Transform cameraAnchor;
        [SerializeField] private RuntimeAnimatorController cutsceneController;
        [SerializeField] private GameObject[] hiddenProps;
        [SerializeField] private CanvasGroup captureFade;
        [SerializeField, Min(0.1f)] private float triggerDistance = 1.6f;
        [SerializeField, Min(0.05f)] private float turnSeconds = 1.2f;
        [SerializeField, Min(0.01f)] private float captureFadeSeconds = 1f;
        [SerializeField, Min(0.01f), Tooltip("Seconds into Chopping when the knife strikes. Blackout is immediate at this clip time.")]
        private float impactClipSeconds = 0.75f;
        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int Chopping = Animator.StringToHash("Chopping");
        private KitchenTableCutsceneRules rules;
        private RuntimeAnimatorController normalController;
        private Camera observer;
        private PlayerInteraction interaction;
        private Transform[] bones;
        private Vector3[] bonePositions;
        private Quaternion[] boneRotations;
        private bool[] propStates;
        private bool normalEnabled, normalRootMotion, ownsCamera, normalInteraction, normalActorActive;
        private float normalSpeed;
        private AnimatorCullingMode normalCulling;
        private Vector3 actorPosition, cameraLocalPosition;
        private Quaternion actorRotation, cameraLocalRotation, facing;
        public Phase State => rules != null ? rules.State : Phase.Waiting;
        public bool IsConfigured => actor != null && player != null && player.PlayerCamera != null
            && death != null && cameraAnchor != null && cutsceneController != null && captureFade != null;

        public override void CaptureNormalState()
        {
            if (rules != null || !IsConfigured) return;
            rules = new KitchenTableCutsceneRules(turnSeconds, impactClipSeconds, captureFadeSeconds);
            observer = player.PlayerCamera;
            interaction = player.GetComponent<PlayerInteraction>();
            normalController = actor.runtimeAnimatorController;
            normalActorActive = actor.gameObject.activeSelf;
            normalEnabled = actor.enabled; normalRootMotion = actor.applyRootMotion;
            normalSpeed = actor.speed; normalCulling = actor.cullingMode;
            actorPosition = actor.transform.position; actorRotation = actor.transform.rotation;
            bones = actor.GetComponentsInChildren<Transform>(true);
            bonePositions = new Vector3[bones.Length]; boneRotations = new Quaternion[bones.Length];
            for (int i = 0; i < bones.Length; i++)
            { bonePositions[i] = bones[i].localPosition; boneRotations[i] = bones[i].localRotation; }
            propStates = new bool[hiddenProps != null ? hiddenProps.Length : 0];
            for (int i = 0; i < propStates.Length; i++) propStates[i] = hiddenProps[i] != null && hiddenProps[i].activeSelf;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (rules == null) { Debug.LogError("KITCHEN_TABLE_CONFIGURATION_INVALID", this); return; }
            rules.Reset();
            for (int i = 0; i < propStates.Length; i++) if (hiddenProps[i] != null) hiddenProps[i].SetActive(false);
            actor.gameObject.SetActive(true);
            actor.enabled = true; actor.applyRootMotion = false; actor.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            actor.runtimeAnimatorController = cutsceneController;
            actor.speed = 1f; actor.Play(Idle, 0, 0f); actor.Update(0f);
        }

        private void LateUpdate()
        {
            if (!IsActive || rules == null || !IsConfigured || Time.timeScale <= 0f) return;
            if (State == Phase.Caught)
            {
                if (death.IsScreenBlack || !death.IsDeathInProgress) ResetAnomaly();
                return;
            }
            if (death.IsDeathInProgress) { ReleaseCamera(); return; }
            AdvanceCutscene();
        }

        private void AdvanceCutscene()
        {
            Vector3 offset = player.transform.position - transform.position;
            bool nearby = Mathf.Abs(offset.y) < 1.5f && new Vector2(offset.x, offset.z).sqrMagnitude <= triggerDistance * triggerDistance;
            var info = actor.GetCurrentAnimatorStateInfo(0);
            float clipTime = State == Phase.Chopping ? info.normalizedTime * info.length : 0f;
            Phase previous = State;
            rules.Tick(Time.deltaTime, nearby, clipTime);
            if (previous == Phase.Waiting && State == Phase.Fading) BeginCutscene();
            PresentState();
            if (previous == Phase.Turning && State == Phase.Chopping) BeginChopping();
            if (previous != Phase.Caught && State == Phase.Caught) Strike();
        }

        private void PresentState()
        {
            if (ownsCamera) captureFade.alpha = rules.FadeAlpha;
            if (ownsCamera) PresentCamera();
            if (State == Phase.Turning) actor.transform.rotation = Quaternion.Slerp(actorRotation, facing, Mathf.SmoothStep(0f, 1f, rules.TurnProgress));
        }

        private void BeginCutscene()
        {
            cameraLocalPosition = observer.transform.localPosition; cameraLocalRotation = observer.transform.localRotation;
            normalInteraction = interaction != null && interaction.enabled;
            if (interaction != null) interaction.enabled = false;
            player.SetCinematicLock(true); ownsCamera = true;
            Vector3 direction = cameraAnchor.position - actor.transform.position; direction.y = 0f;
            facing = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : actorRotation;
        }

        private void PresentCamera()
        {
            if (ownsCamera) observer.transform.SetPositionAndRotation(cameraAnchor.position, cameraAnchor.rotation);
        }

        private void BeginChopping()
        {
            actor.transform.rotation = facing;
            actor.speed = 1f; actor.Play(Chopping, 0, 0f); actor.Update(0f);
        }

        private void Strike()
        {
            PresentCamera();
            captureFade.alpha = 0f;
            // Restore interaction before handing ownership to death, so respawn restores the original value.
            if (interaction != null) interaction.enabled = normalInteraction;
            bool accepted = death.TryBeginFreezeDeath(this, 0f);
            if (accepted) ownsCamera = false;
            else ResetAnomaly();
        }

        private void ReleaseCamera()
        {
            if (!ownsCamera) return;
            ownsCamera = false;
            if (captureFade != null) captureFade.alpha = 0f;
            if (death != null && death.IsDeathInProgress) return;
            if (observer != null) observer.transform.SetLocalPositionAndRotation(cameraLocalPosition, cameraLocalRotation);
            if (interaction != null) interaction.enabled = normalInteraction;
            if (player != null) player.SetCinematicLock(false);
        }

        protected override void OnResetAnomaly()
        {
            ReleaseCamera();
            if (rules == null) return;
            rules.Reset();
            for (int i = 0; i < propStates.Length; i++) if (hiddenProps[i] != null) hiddenProps[i].SetActive(propStates[i]);
            if (actor == null) return;
            actor.runtimeAnimatorController = normalController;
            actor.applyRootMotion = normalRootMotion; actor.speed = normalSpeed; actor.cullingMode = normalCulling;
            if (Application.isPlaying && actor.enabled) { actor.Rebind(); actor.Update(0f); }
            for (int i = 0; i < bones.Length; i++)
                if (bones[i] != null) bones[i].SetLocalPositionAndRotation(bonePositions[i], boneRotations[i]);
            actor.transform.SetPositionAndRotation(actorPosition, actorRotation); actor.enabled = normalEnabled;
            actor.gameObject.SetActive(normalActorActive);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
