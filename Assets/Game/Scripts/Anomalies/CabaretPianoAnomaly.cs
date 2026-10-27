using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretPianoAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform observer;
        [SerializeField] private Transform[] whiteKeys;
        [SerializeField] private AudioSource pianoAudio;
        [SerializeField, Min(.1f)] private float activationDistance = 2.8f;
        [SerializeField, Range(1,8)] private float pressAngle = 4f;
        private static readonly int[] Notes = { 14,16,18,17,16,14,13,11,14,16,17,16,13,14,11,10 };
        private Quaternion[] normalRotations;
        private enum Phase { Waiting, Playing, Finished }
        private Phase phase;
        private int pressed = -1;
        public const float NoteInterval = .36f;
        public static int NoteCount => Notes.Length;
        public static int NoteAt(int index) => Notes[index];

        public void Configure(Transform player, Transform[] keys, AudioSource audio)
        {
            observer = player;
            whiteKeys = keys;
            pianoAudio = audio;
        }

        public override void CaptureNormalState()
        {
            if (normalRotations != null || whiteKeys == null) return;
            normalRotations = new Quaternion[whiteKeys.Length];
            for (int i=0;i<whiteKeys.Length;i++)
                if (whiteKeys[i] != null) normalRotations[i] = whiteKeys[i].localRotation;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            phase = Phase.Waiting;
        }

        private void Update()
        {
            if (!IsActive || observer == null || pianoAudio == null || normalRotations == null) return;
            if (phase == Phase.Waiting && (observer.position-transform.position).sqrMagnitude <= activationDistance*activationDistance)
            {
                phase = Phase.Playing;
                pianoAudio.Play();
            }
            if (phase != Phase.Playing) return;
            RestorePressed();
            if (!pianoAudio.isPlaying) { phase=Phase.Finished; return; }
            AnimateKey(pianoAudio.time);
        }

        private void AnimateKey(float time)
        {
            int step = Mathf.FloorToInt(time/NoteInterval);
            if (step >= NoteCount) return;
            int key = NoteAt(step);
            if (key >= whiteKeys.Length || whiteKeys[key] == null) return;
            float fraction = time/NoteInterval-step;
            float amount = Mathf.Clamp01(fraction/.08f)*Mathf.Clamp01((.78f-fraction)/.2f);
            whiteKeys[key].localRotation = normalRotations[key]*Quaternion.Euler(-pressAngle*amount,0,0);
            pressed = key;
        }

        private void RestorePressed()
        {
            if (pressed >= 0 && whiteKeys[pressed] != null)
                whiteKeys[pressed].localRotation = normalRotations[pressed];
            pressed = -1;
        }

        protected override void OnResetAnomaly()
        {
            RestorePressed();
            if (pianoAudio != null) pianoAudio.Stop();
            phase = Phase.Waiting;
        }

        private void OnDisable() => ResetAnomaly();
    }
}
