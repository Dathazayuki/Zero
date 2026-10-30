using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretCurtainHandsAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform observer;
        [SerializeField] private BoxCollider stageZone;
        [SerializeField] private Transform[] handGroups;
        [SerializeField] private Vector3[] retreatOffsets;
        [SerializeField, Min(.1f)] private float retreatSeconds=.65f;
        private Vector3[] starts;
        private float retreatTimer;
        private bool retreatStarted;
        private bool CanAdvance => observer!=null && stageZone!=null && starts!=null;

        public void Configure(Transform player,BoxCollider stage,Transform[] groups,Vector3[] offsets)
        {
            observer=player; stageZone=stage; handGroups=groups; retreatOffsets=offsets;
        }

        public override void CaptureNormalState()
        {
            if (starts!=null || handGroups==null || retreatOffsets==null) return;
            if (handGroups.Length!=retreatOffsets.Length) return;
            starts=new Vector3[handGroups.Length];
            for (int i=0;i<handGroups.Length;i++)
                if (handGroups[i]!=null) starts[i]=handGroups[i].localPosition;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            SetGroups(true);
        }

        private void Update() => Advance(Time.deltaTime);

        public void Advance(float delta)
        {
            if (!IsActive || !CanAdvance || delta<=0f) return;
            if (!retreatStarted)
            {
                if (!stageZone.bounds.Contains(observer.position+Vector3.up)) return;
                retreatStarted=true;
            }
            if (retreatTimer>=retreatSeconds) return;
            retreatTimer+=delta;
            float progress=Mathf.Clamp01(retreatTimer/Mathf.Max(.1f,retreatSeconds));
            for (int i=0;i<handGroups.Length;i++)
            {
                var group=handGroups[i];
                if (group==null) continue;
                group.localPosition=starts[i]+retreatOffsets[i]*Mathf.SmoothStep(0,1,progress);
                if (progress>=1f) group.gameObject.SetActive(false);
            }
        }

        private void SetGroups(bool visible)
        {
            if (starts==null) return;
            retreatStarted=false; retreatTimer=0f;
            for (int i=0;i<handGroups.Length;i++)
            {
                if (handGroups[i]==null) continue;
                handGroups[i].localPosition=starts[i];
                handGroups[i].gameObject.SetActive(visible);
            }
        }

        protected override void OnResetAnomaly() => SetGroups(false);
        private void OnDisable() => ResetAnomaly();
    }
}
