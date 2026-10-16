using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    [Serializable]
    public sealed class SoundDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private AudioClip clip;
        [SerializeField, HideInInspector] private AudioClip legacyClip;
        [SerializeField] private AudioBus bus = AudioBus.Sfx;
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField, Range(0.1f, 3f)] private float pitch = 1f;
        [SerializeField] private bool loop;
        [SerializeField, Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField, Min(0.01f)] private float minDistance = 1f;
        [SerializeField, Min(0.01f)] private float maxDistance = 20f;

        public string Id => id;
        public AudioClip Clip => clip;
        public AudioClip LegacyClip => legacyClip;
        public AudioBus Bus => bus;
        public float Volume => Mathf.Clamp01(volume);
        public float Pitch => Mathf.Clamp(pitch, 0.1f, 3f);
        public bool Loop => loop;
        public float SpatialBlend => spatialBlend;
        public float MinDistance => Mathf.Max(0.01f, minDistance);
        public float MaxDistance => Mathf.Max(MinDistance, maxDistance);

        public SoundDefinition(string soundId, AudioClip soundClip, AudioBus soundBus)
        {
            id = soundId;
            clip = soundClip;
            legacyClip = soundClip;
            bus = soundBus;
        }
    }
}
