using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    // Per-object ownership. Bind at composition; no scene lookup, singleton or per-frame work.
    public sealed class AudioEmitter : MonoBehaviour
    {
        [SerializeField] private string soundId;
        private AudioPlaybackService playback;
        private SoundHandle loopHandle;
        private bool invalidIdLogged;

        private void OnEnable()
        {
            if (playback != null) return;
            GameFlowController flow = GameFlowController.Instance;
            if (flow != null && flow.Audio != null) Bind(flow.Audio);
        }

        public void Bind(AudioPlaybackService service)
        {
            ResetAudio();
            playback = service;
        }

        public bool Play() => Play(soundId);

        public bool Play(string id)
        {
            if (!isActiveAndEnabled || playback == null) return false;
            SoundDefinition sound = playback.Resolve(id);
            if (sound == null || sound.Clip == null)
            {
                if (!invalidIdLogged)
                {
                    Debug.LogWarning($"AUDIO_ID_INVALID: id={id}", this);
                    invalidIdLogged = true;
                }
                return false;
            }
            invalidIdLogged = false;
            if (sound.Loop && playback.IsPlaying(loopHandle)) return true;
            SoundHandle handle = playback.Play(id, transform);
            if (sound.Loop) loopHandle = handle;
            return handle.IsValid;
        }

        public void ResetAudio()
        {
            if (playback != null) playback.StopOwner(transform);
            loopHandle = default;
        }

        private void OnDisable() => ResetAudio();
    }
}
