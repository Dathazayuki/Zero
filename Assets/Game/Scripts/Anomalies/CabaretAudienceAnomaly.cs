using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretAudienceAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Camera observer;
        [SerializeField] private BoxCollider stageZone;
        [SerializeField] private Transform audienceTarget;
        [SerializeField] private GameObject audience;
        [SerializeField] private GameObject normalDisplays;
        [SerializeField] private Transform[] turnedChairs;
        private readonly CabaretAudienceRules rules = new CabaretAudienceRules();
        private Quaternion[] chairRotations;
        private bool normalActive;
        private bool captured;
        private bool IsConfigured => player != null && observer != null && stageZone != null &&
            audienceTarget != null && audience != null;

        public override void CaptureNormalState()
        {
            if (captured || normalDisplays == null || turnedChairs == null) return;
            normalActive = normalDisplays.activeSelf;
            chairRotations = new Quaternion[turnedChairs.Length];
            for (int i=0;i<turnedChairs.Length;i++) chairRotations[i]=turnedChairs[i].localRotation;
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            rules.Reset();
        }

        private void Update()
        {
            if (!IsActive || !captured || rules.HasRevealed || !IsConfigured) return;
            Vector3 direction = observer.transform.forward;
            bool forward = Vector3.Dot(direction,stageZone.transform.forward) > .35f;
            bool backward = Vector3.Dot(direction,stageZone.transform.forward) < -.35f;
            bool sees = backward && AnomalyGazeVisibility.IsVisible(observer,audienceTarget,null,~0,.1f);
            if (rules.Advance(stageZone.bounds.Contains(player.position),forward,sees)) Reveal();
        }

        private void Reveal()
        {
            normalDisplays.SetActive(false);
            for (int i=0;i<turnedChairs.Length;i++)
                turnedChairs[i].rotation = Quaternion.LookRotation(-stageZone.transform.forward,Vector3.up);
            audience.SetActive(true);
        }

        protected override void OnResetAnomaly()
        {
            rules.Reset();
            if (audience != null) audience.SetActive(false);
            if (!captured) return;
            if (normalDisplays != null) normalDisplays.SetActive(normalActive);
            for (int i=0;i<turnedChairs.Length;i++)
                if (turnedChairs[i] != null) turnedChairs[i].localRotation=chairRotations[i];
        }

        private void OnDisable() => ResetAnomaly();
    }
}
