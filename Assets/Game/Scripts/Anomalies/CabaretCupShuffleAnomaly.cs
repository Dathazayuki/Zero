using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretCupShuffleAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform observer;
        [SerializeField] private Transform[] cups;
        [SerializeField, Min(.1f)] private float approachDistance=1.7f;
        [SerializeField, Min(.1f)] private float firstSwapSeconds=.85f;
        [SerializeField, Min(.1f)] private float lastSwapSeconds=.45f;
        [SerializeField, Min(1)] private int swapCount=7;
        [SerializeField, Min(.055f)] private float laneWidth=.06f;
        private enum Phase { Waiting, Shuffling, Finished }
        private Phase phase;
        private Vector3[] starts;
        private readonly int[] occupants={0,1,2};
        private int step;
        private float elapsed;
        private int Left => step%2;
        private float Duration => Mathf.Lerp(firstSwapSeconds,lastSwapSeconds,step/(float)Mathf.Max(1,swapCount-1));

        public void Configure(Transform player,Transform[] glasses)
        {
            observer=player; cups=glasses;
        }

        public override void CaptureNormalState()
        {
            if (starts!=null || cups==null || cups.Length!=3) return;
            for (int i=0;i<3;i++) if (cups[i]==null) return;
            starts=new Vector3[3];
            for (int i=0;i<3;i++) starts[i]=cups[i].localPosition;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            phase=Phase.Waiting;
        }

        private void Update()
        {
            if (!IsActive || starts==null || observer==null || phase==Phase.Finished) return;
            if (phase==Phase.Waiting)
            {
                Vector3 center=cups[1].position;
                Vector3 offset=observer.position-center; offset.y=0;
                if (offset.sqrMagnitude>approachDistance*approachDistance) return;
                phase=Phase.Shuffling;
            }
            AdvanceShuffle(Time.deltaTime);
        }

        private void AdvanceShuffle(float seconds)
        {
            elapsed+=seconds;
            float t=Mathf.Clamp01(elapsed/Mathf.Max(.1f,Duration));
            int a=Left, b=a+1;
            cups[occupants[a]].localPosition=SwapPosition(starts[a],starts[b],t,laneWidth);
            cups[occupants[b]].localPosition=SwapPosition(starts[b],starts[a],t,laneWidth);
            if (t<1) return;
            int previous=occupants[a]; occupants[a]=occupants[b]; occupants[b]=previous;
            step++; elapsed=0;
            if (step>=swapCount) phase=Phase.Finished;
        }

        public static Vector3 SwapPosition(Vector3 from,Vector3 to,float progress,float width)
        {
            float t=Mathf.SmoothStep(0,1,progress);
            Vector3 side=Vector3.Cross(Vector3.up,(to-from).normalized);
            return Vector3.Lerp(from,to,t)+side*(Mathf.Sin(t*Mathf.PI)*width);
        }

        protected override void OnResetAnomaly()
        {
            phase=Phase.Waiting; step=0; elapsed=0;
            for (int i=0;i<3;i++) occupants[i]=i;
            if (starts==null) return;
            for (int i=0;i<3;i++) if (cups[i]!=null) cups[i].localPosition=starts[i];
        }

        private void OnDisable() => ResetAnomaly();
    }
}
