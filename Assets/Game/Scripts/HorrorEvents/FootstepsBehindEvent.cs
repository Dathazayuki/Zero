using Zero.AnomalyPrototype.Core;
using UnityEngine;

namespace Zero.AnomalyPrototype.HorrorEvents
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class FootstepsBehindEvent : HorrorEventBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private AudioClip footstepClip;
        [SerializeField, Min(0.5f)] private float distanceBehindPlayer = 2.2f;
        [SerializeField] private AudioManager audioManager;
        private AudioSource source;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            ConfigureSource();
        }

        public void Configure(Transform playerTransform, AudioManager manager, AudioClip optionalClip = null)
        {
            player = playerTransform;
            audioManager = manager;
            footstepClip = optionalClip;
            source = GetComponent<AudioSource>();
            ConfigureSource();
        }

        protected override void OnPlay()
        {
            if (source == null || player == null)
            {
                return;
            }

            source.transform.position = player.position - player.forward * distanceBehindPlayer + Vector3.up * 0.2f;
            AudioClip clip = footstepClip != null ? footstepClip : audioManager?.GetFallbackFootsteps();
            if (clip != null)
            {
                source.PlayOneShot(clip);
            }
        }

        protected override void OnResetEvent()
        {
            if (source != null)
            {
                source.Stop();
            }
        }

        private void ConfigureSource()
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
            source.minDistance = 0.5f;
            source.maxDistance = 14f;
            source.rolloffMode = AudioRolloffMode.Linear;
        }
    }
}
