using System.Collections.Generic;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class AudioPlaybackService : MonoBehaviour
    {
        private sealed class Voice
        {
            internal AudioSource Source;
            internal Transform Owner;
            internal uint Generation;
            internal bool Active;
        }

        private SoundCatalog catalog;
        private SoundCatalogIndex index;
        private Voice[] voices;
        private float nextExhaustionLog;
        private readonly HashSet<string> missingIds = new HashSet<string>();
        public SoundCatalog Catalog => catalog;

        public void Initialize(SoundCatalog config, int capacity = 24)
        {
            if (voices != null) return;
            catalog = config;
            index = new SoundCatalogIndex();
            if (catalog != null)
                foreach (SoundDefinition sound in catalog.Sounds)
                    if (!index.Add(sound)) Debug.LogError($"AUDIO_DUPLICATE_OR_INVALID_ID: id={sound?.Id}", this);
            voices = new Voice[Mathf.Clamp(capacity, 1, 128)];
            for (int i = 0; i < voices.Length; i++)
            {
                var child = new GameObject($"AudioVoice_{i:00}");
                child.transform.SetParent(transform, false);
                AudioSource source = child.AddComponent<AudioSource>();
                source.playOnAwake = false;
                voices[i] = new Voice { Source = source };
            }
        }

        public SoundDefinition Resolve(string id, AudioClip fallback = null) => index?.Resolve(id, fallback);

        public SoundHandle Play(string id, Transform owner)
        {
            SoundDefinition sound = Resolve(id);
            if (sound?.Clip == null)
            {
                string key = string.IsNullOrEmpty(id) ? "<empty>" : id;
                if (missingIds.Add(key)) Debug.LogWarning($"AUDIO_ID_INVALID: id={key}", this);
                return default;
            }
            return PlaySound(sound.Clip, sound, sound.Bus, owner);
        }

        public SoundHandle PlayClip(AudioClip clip, Transform owner, AudioBus bus = AudioBus.Sfx)
        {
            if (clip == null) return default;
            return PlaySound(clip, Resolve(null, clip), bus, owner);
        }

        private SoundHandle PlaySound(AudioClip clip, SoundDefinition sound, AudioBus bus, Transform owner)
        {
            if (!isActiveAndEnabled || owner == null || voices == null) return default;
            int slot = GetFreeSlot();
            if (slot < 0)
            {
                if (Time.unscaledTime >= nextExhaustionLog)
                {
                    Debug.LogWarning($"AUDIO_POOL_EXHAUSTED: capacity={voices.Length}, clip={clip.name}", this);
                    nextExhaustionLog = Time.unscaledTime + 5f;
                }
                return default;
            }
            Voice voice = voices[slot];
            voice.Generation++;
            if (voice.Generation == 0) voice.Generation++;
            voice.Owner = owner;
            voice.Active = true;
            Apply(voice.Source, clip, sound, bus);
            voice.Source.transform.position = owner.position;
            voice.Source.Play();
            return new SoundHandle(slot, voice.Generation);
        }

        public bool IsPlaying(SoundHandle handle)
        {
            return voices != null && handle.IsValid && handle.Slot >= 0 && handle.Slot < voices.Length &&
                voices[handle.Slot].Active && voices[handle.Slot].Generation == handle.Generation;
        }

        public void Stop(SoundHandle handle)
        {
            if (IsPlaying(handle)) Release(voices[handle.Slot]);
        }

        public void StopOwner(Transform owner)
        {
            if (voices == null) return;
            foreach (Voice voice in voices)
                if (voice.Active && voice.Owner == owner) Release(voice);
        }

        public void Route(AudioSource source, AudioBus bus)
        {
            if (source != null && catalog != null) source.outputAudioMixerGroup = catalog.GetGroup(bus);
        }

        private void Apply(AudioSource source, AudioClip clip, SoundDefinition sound, AudioBus bus)
        {
            source.clip = clip;
            source.volume = sound != null ? sound.Volume : 1f;
            source.pitch = sound != null ? sound.Pitch : 1f;
            source.loop = sound != null && sound.Loop;
            source.spatialBlend = sound != null ? sound.SpatialBlend : 1f;
            source.minDistance = sound != null ? sound.MinDistance : 1f;
            source.maxDistance = sound != null ? sound.MaxDistance : 20f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.dopplerLevel = 0f;
            Route(source, bus);
        }

        private int GetFreeSlot()
        {
            for (int i = 0; i < voices.Length; i++)
                if (!voices[i].Active) return i;
            return -1;
        }

        private void LateUpdate()
        {
            if (voices == null) return;
            foreach (Voice voice in voices) TickVoice(voice);
        }

        private static void TickVoice(Voice voice)
        {
            if (!voice.Active) return;
            if (voice.Owner == null || !voice.Owner.gameObject.activeInHierarchy)
            {
                Release(voice);
                return;
            }
            voice.Source.transform.position = voice.Owner.position;
            if (!AudioListener.pause && !voice.Source.isPlaying) Release(voice);
        }

        private static void Release(Voice voice)
        {
            voice.Source.Stop();
            voice.Source.clip = null;
            voice.Owner = null;
            voice.Active = false;
        }

        private void OnDisable()
        {
            if (voices == null) return;
            foreach (Voice voice in voices) Release(voice);
        }
    }
}
