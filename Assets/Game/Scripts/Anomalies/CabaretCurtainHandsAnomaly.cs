using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretCurtainHandsAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform observer;
        [SerializeField] private Transform[] handGroups;
        [SerializeField] private Vector3[] retreatOffsets;
        [SerializeField, Min(.1f)] private float approachDistance=1.8f;
        [SerializeField, Min(.1f)] private float retreatSeconds=.65f;
        private enum Phase { Waiting, Retreating, Hidden }
        private Phase[] phases;
        private Vector3[] starts;
        private float[] timers;

        public void Configure(Transform player,Transform[] groups,Vector3[] offsets)
        {
            observer=player; handGroups=groups; retreatOffsets=offsets;
        }

        public override void CaptureNormalState()
        {
            if (starts!=null || handGroups==null || retreatOffsets==null) return;
            if (handGroups.Length!=retreatOffsets.Length) return;
            starts=new Vector3[handGroups.Length];
            phases=new Phase[handGroups.Length];
            timers=new float[handGroups.Length];
            for (int i=0;i<handGroups.Length;i++)
                if (handGroups[i]!=null) starts[i]=handGroups[i].localPosition;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            SetGroups(true);
        }

        private void Update()
        {
            if (!IsActive || observer==null || starts==null) return;
            for (int i=0;i<handGroups.Length;i++) AdvanceGroup(i);
        }

        private void AdvanceGroup(int i)
        {
            var group=handGroups[i];
            if (group==null || phases[i]==Phase.Hidden) return;
            if (phases[i]==Phase.Waiting)
            {
                var delta=observer.position-group.position; delta.y=0;
                if (delta.sqrMagnitude>approachDistance*approachDistance) return;
                phases[i]=Phase.Retreating;
            }
            timers[i]+=Time.deltaTime;
            float progress=Mathf.Clamp01(timers[i]/Mathf.Max(.1f,retreatSeconds));
            group.localPosition=starts[i]+retreatOffsets[i]*Mathf.SmoothStep(0,1,progress);
            if (progress<1) return;
            phases[i]=Phase.Hidden;
            group.gameObject.SetActive(false);
        }

        private void SetGroups(bool visible)
        {
            if (starts==null) return;
            for (int i=0;i<handGroups.Length;i++)
            {
                phases[i]=Phase.Waiting; timers[i]=0;
                if (handGroups[i]==null) continue;
                handGroups[i].localPosition=starts[i];
                handGroups[i].gameObject.SetActive(visible);
            }
        }

        protected override void OnResetAnomaly() => SetGroups(false);
        private void OnDisable() => ResetAnomaly();
    }
}
