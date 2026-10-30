using UnityEngine;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretRitualRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretRitualHost : MonoBehaviour
    {
        [SerializeField] private Animator actor;
        [SerializeField] private GameObject chair;
        [SerializeField, Range(0f, 70f)] private float defeatedBow = 48f;
        private static readonly int Idle = Animator.StringToHash("IR5_12_HostIdle");
        private static readonly int Conduct = Animator.StringToHash("Host_Conducting");
        private static readonly int Sitting = Animator.StringToHash("Sitting");
        private Transform spine;
        private Vector3 position;
        private Quaternion rotation;
        private bool captured, rootMotion, actorWasEnabled;
        private AnimatorCullingMode cullingMode;
        private float speed, bow;
        private Phase phase;
        public bool IsConfigured => actor != null && actor.runtimeAnimatorController != null && chair != null;

        public void Capture()
        {
            if (captured || !IsConfigured) return;
            captured = true;
            position = actor.transform.position; rotation = actor.transform.rotation;
            rootMotion = actor.applyRootMotion; speed = actor.speed;
            actorWasEnabled = actor.enabled; cullingMode = actor.cullingMode;
            if (actor.isHuman) spine = actor.GetBoneTransform(HumanBodyBones.Spine);
        }

        public bool Begin()
        {
            Capture();
            if (!captured) return false;
            chair.SetActive(true); actor.gameObject.SetActive(true); actor.enabled = true;
            actor.applyRootMotion = true; actor.speed = 1f;
            actor.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            actor.Rebind(); actor.Update(0f);
            if (!actor.HasState(0, Idle) || !actor.HasState(0, Conduct) || !actor.HasState(0, Sitting)) return false;
            phase = Phase.Blackout; bow = 0f;
            actor.Play(Idle, 0, 0f); actor.Update(0f);
            return true;
        }

        public void Enter(Phase value)
        {
            phase = value;
            if (value == Phase.Rising) actor.CrossFadeInFixedTime(Conduct, .25f, 0, 0f);
            if (value == Phase.Releasing) actor.CrossFadeInFixedTime(Sitting, 1.4f, 0, 0f);
        }

        public void PresentRelease(float amount) => bow = defeatedBow * Mathf.SmoothStep(0f, 1f, amount);

        private void LateUpdate()
        {
            if (!captured || spine == null || actor == null || !actor.gameObject.activeInHierarchy) return;
            if (phase != Phase.Releasing && phase != Phase.Safe) return;
            spine.rotation = Quaternion.AngleAxis(bow, actor.transform.right) * spine.rotation;
        }

        public void Restore()
        {
            bow = 0f; phase = Phase.Waiting;
            if (chair != null) chair.SetActive(false);
            if (!captured || actor == null) return;
            actor.applyRootMotion = rootMotion; actor.speed = speed;
            actor.cullingMode = cullingMode; actor.enabled = actorWasEnabled;
            actor.transform.SetPositionAndRotation(position, rotation);
            actor.gameObject.SetActive(false);
        }

        private void OnDisable() => Restore();
    }
}
