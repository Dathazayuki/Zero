using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    // Migration adapter: preserve authored clips and AudioSource ownership.
    public sealed class SourceSoundPlayback
    {
        private readonly AudioSource source;
        private readonly AudioPlaybackService service;
        private readonly AudioBus fallbackBus;
        private readonly bool useCatalogSpatial;
        private float baseVolume;
        private float basePitch;
        private float appliedVolume;
        private float appliedPitch;
        private bool missingClipLogged;
        private string lastMissingId;
        private string lastMismatchId;
        private bool disabledSourceLogged;

        public SourceSoundPlayback(AudioSource target, AudioPlaybackService playback, AudioBus bus,
            bool applySpatial = false)
        {
            source = target;
            service = playback;
            fallbackBus = bus;
            useCatalogSpatial = applySpatial;
            if (source == null) return;
            baseVolume = appliedVolume = source.volume;
            basePitch = appliedPitch = source.pitch;
        }

        public bool Matches(AudioSource target, AudioPlaybackService playback) =>
            source == target && service == playback;

        public void Play(string id, AudioClip fallback, bool oneShot = true, float volumeScale = 1f)
        {
            if (!IsReady()) return;
            SoundDefinition sound = service != null ? service.Resolve(id, fallback) : null;
            AudioClip clip = ResolveClip(id, fallback, sound);
            if (clip == null) return;
            if (source.outputAudioMixerGroup == null || (!string.IsNullOrEmpty(id) && sound != null))
                service?.Route(source, ResolveBus(id, sound));
            if (useCatalogSpatial) ApplySpatial(sound);
            Rebase();
            appliedPitch = basePitch * (sound != null ? sound.Pitch : 1f);
            source.pitch = appliedPitch;
            float gain = (sound != null ? sound.Volume : 1f) * Mathf.Clamp01(volumeScale);
            StartPlayback(clip, oneShot, gain);
        }

        private bool IsReady()
        {
            if (source == null) return false;
            if (source.isActiveAndEnabled) { disabledSourceLogged = false; return true; }
            if (!disabledSourceLogged) Debug.LogWarning($"AUDIO_SOURCE_DISABLED: source={source.name}", source);
            disabledSourceLogged = true;
            return false;
        }

        private AudioClip ResolveClip(string id, AudioClip fallback, SoundDefinition sound)
        {
            if (service != null && sound == null && !string.IsNullOrEmpty(id) && id != lastMissingId)
            {
                Debug.LogWarning($"AUDIO_ID_INVALID: id={id}, source={source.name}", source);
                lastMissingId = id;
            }
            if (sound?.Clip != null && fallback != null && sound.Clip != fallback && id != lastMismatchId)
            {
                Debug.LogWarning($"AUDIO_CLIP_MISMATCH: id={id}, catalog={sound.Clip.name}, authored={fallback.name}", source);
                lastMismatchId = id;
            }
            // Existing Inspector references remain the clip authority during migration.
            AudioClip clip = fallback != null ? fallback : sound?.Clip;
            if (clip == null)
            {
                if (!missingClipLogged)
                {
                    Debug.LogWarning($"AUDIO_CLIP_MISSING: id={id ?? "<legacy>"}, source={source.name}", source);
                    missingClipLogged = true;
                }
                return null;
            }
            missingClipLogged = false;
            return clip;
        }

        private AudioBus ResolveBus(string id, SoundDefinition sound)
        {
            if (!string.IsNullOrEmpty(id) && sound != null) return sound.Bus;
            if (fallbackBus != AudioBus.Sfx) return fallbackBus;
            return sound != null ? sound.Bus : fallbackBus;
        }

        private void StartPlayback(AudioClip clip, bool oneShot, float gain)
        {
            if (oneShot)
            {
                appliedVolume = baseVolume;
                source.volume = baseVolume;
                source.PlayOneShot(clip, gain);
            }
            else
            {
                appliedVolume = baseVolume * gain;
                source.volume = appliedVolume;
                source.clip = clip;
                source.Play();
            }
        }

        private void ApplySpatial(SoundDefinition sound)
        {
            source.spatialBlend = sound != null ? sound.SpatialBlend : 1f;
            source.minDistance = sound != null ? sound.MinDistance : 1f;
            source.maxDistance = sound != null ? sound.MaxDistance : 20f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f;
        }

        private void Rebase()
        {
            if (!Mathf.Approximately(source.volume, appliedVolume)) baseVolume = source.volume;
            if (!Mathf.Approximately(source.pitch, appliedPitch)) basePitch = source.pitch;
        }

        public void Stop()
        {
            if (source == null) return;
            source.Stop();
            Rebase();
            source.volume = appliedVolume = baseVolume;
            source.pitch = appliedPitch = basePitch;
        }
    }
}
