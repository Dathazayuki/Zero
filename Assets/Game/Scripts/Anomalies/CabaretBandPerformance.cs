using UnityEngine;
using Zero.AnomalyPrototype.Flow;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBandPerformance : MonoBehaviour
    {
        [SerializeField] private AudioSource music;
        [SerializeField] private Transform bow;
        [SerializeField] private Transform[] drumsticks;
        [SerializeField] private Transform[] pianoKeys;
        [SerializeField] private Transform bowPlayingPose;
        [SerializeField] private Transform[] drumStrikePoses;
        [SerializeField, Min(.1f)] private float beatsPerMinute = 100f;
        private SourceSoundPlayback playback;
        private Vector3 bowPosition;
        private Quaternion bowRotation;
        private Quaternion[] stickRotations, keyRotations;
        private Vector3[] stickPositions;
        private bool captured;
        public bool IsPlaying { get; private set; }
        public bool IsConfigured => music != null && music.clip != null;

        public void CaptureNormalState()
        {
            if (captured) return;
            captured = true;
            if (bow != null) { bowPosition = bow.localPosition; bowRotation = bow.localRotation; }
            stickRotations = CaptureRotations(drumsticks);
            if (drumsticks != null)
            {
                stickPositions = new Vector3[drumsticks.Length];
                for (int i = 0; i < drumsticks.Length; i++)
                    if (drumsticks[i] != null) stickPositions[i] = drumsticks[i].localPosition;
            }
            keyRotations = CaptureRotations(pianoKeys);
        }

        private static Quaternion[] CaptureRotations(Transform[] parts)
        {
            if (parts == null) return null;
            var rotations = new Quaternion[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                if (parts[i] != null) rotations[i] = parts[i].localRotation;
            return rotations;
        }

        public void Begin()
        {
            if (IsPlaying || !IsConfigured) return;
            CaptureNormalState();
            music.loop = true;
            GameAudioBootstrap.BindSource(ref playback, music, AudioBus.Music, false).Play(null, music.clip, false);
            IsPlaying = true;
        }

        private void Update()
        {
            if (!IsPlaying || music == null || Time.timeScale <= 0f) return;
            float beat = music.time * beatsPerMinute / 60f;
            if (bow != null && bowPlayingPose != null)
            {
                bow.SetPositionAndRotation(bowPlayingPose.position
                    + bowPlayingPose.up * (.18f * Mathf.Sin(beat * Mathf.PI)), bowPlayingPose.rotation);
            }
            AnimateSticks(beat);
            AnimateKeys(beat);
        }

        private void AnimateSticks(float beat)
        {
            if (drumsticks == null || drumStrikePoses == null) return;
            for (int i = 0; i < drumsticks.Length; i++)
                AnimateStick(i, beat);
        }

        private void AnimateStick(int index, float beat)
        {
            if (index >= drumStrikePoses.Length || drumsticks[index] == null || drumStrikePoses[index] == null) return;
            Transform pose = drumStrikePoses[index];
            float lift = 35f * (.5f - .5f * Mathf.Cos((beat + index) * Mathf.PI));
            float halfLength = drumsticks[index].lossyScale.y;
            Pose result = EvaluateStrike(new Pose(pose.position, pose.rotation), halfLength, lift);
            drumsticks[index].SetPositionAndRotation(result.position, result.rotation);
        }

        public static Pose EvaluateStrike(Pose contact, float halfLength, float lift)
        {
            Quaternion rotation = contact.rotation * Quaternion.Euler(-lift, 0f, 0f);
            Vector3 grip = contact.position - contact.rotation * Vector3.up * (halfLength * 2f);
            return new Pose(grip + rotation * Vector3.up * halfLength, rotation);
        }

        private void AnimateKeys(float beat)
        {
            if (pianoKeys == null || keyRotations == null) return;
            int step = Mathf.FloorToInt(beat * 2f);
            int note = CabaretPianoAnomaly.NoteAt(step % CabaretPianoAnomaly.NoteCount);
            for (int i = 0; i < pianoKeys.Length; i++)
                if (pianoKeys[i] != null)
                    pianoKeys[i].localRotation = keyRotations[i] * Quaternion.Euler(i == note ? -4f : 0f, 0f, 0f);
        }

        public void StopAndRestore()
        {
            IsPlaying = false;
            if (music != null) music.Stop();
            if (!captured) return;
            if (bow != null) { bow.localPosition = bowPosition; bow.localRotation = bowRotation; }
            Restore(drumsticks, stickRotations);
            if (drumsticks != null && stickPositions != null)
                for (int i = 0; i < drumsticks.Length; i++)
                    if (drumsticks[i] != null) drumsticks[i].localPosition = stickPositions[i];
            Restore(pianoKeys, keyRotations);
        }

        private static void Restore(Transform[] parts, Quaternion[] rotations)
        {
            if (parts == null || rotations == null) return;
            for (int i = 0; i < parts.Length; i++)
                if (parts[i] != null) parts[i].localRotation = rotations[i];
        }

        private void OnDisable() => StopAndRestore();
    }
}
