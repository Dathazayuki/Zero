using Zero.AnomalyPrototype.Flow;
using System.Collections;
using UnityEngine;

namespace Zero.AnomalyPrototype.HorrorEvents
{
    public sealed class LightFlickerEvent : HorrorEventBehaviour
    {
        private SourceSoundPlayback soundPlayback;
        [SerializeField] private Light[] lights;
        [SerializeField, Min(1)] private int flickerCount = 4;
        [SerializeField, Min(0.02f)] private float interval = 0.11f;
        [SerializeField] private AudioClip flickerClip;
        private AudioSource audioSource;
        private bool[] normalStates;
        private Coroutine routine;

        private void Awake()
        {
            flickerClip = GameAudioBootstrap.ResolveDefaultClip(flickerClip, "clip.LightFlicker");
        }

        public void Configure(Light[] controlledLights, int count = 4, float flickerInterval = 0.11f)
        {
            lights = controlledLights;
            flickerCount = Mathf.Max(1, count);
            interval = Mathf.Max(0.02f, flickerInterval);
            CaptureStates();
        }

        public override void CaptureNormalState()
        {
            CaptureStates();
        }

        protected override void OnPlay()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1f;
                }
            }

            if (audioSource != null && flickerClip != null)
            {
                GameAudioBootstrap.BindSource(ref soundPlayback, audioSource).Play(null, flickerClip);
            }

            routine = StartCoroutine(FlickerRoutine());
        }

        protected override void OnResetEvent()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            RestoreStates();
        }

        private IEnumerator FlickerRoutine()
        {
            for (int i = 0; i < flickerCount * 2; i++)
            {
                SetLightsEnabled(i % 2 != 0);
                yield return new WaitForSeconds(interval);
            }

            RestoreStates();
            routine = null;
        }

        private void CaptureStates()
        {
            if (lights == null)
            {
                normalStates = null;
                return;
            }

            normalStates = new bool[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                normalStates[i] = lights[i] != null && lights[i].enabled;
            }
        }

        private void RestoreStates()
        {
            if (lights == null || normalStates == null)
            {
                return;
            }

            for (int i = 0; i < lights.Length && i < normalStates.Length; i++)
            {
                if (lights[i] != null) lights[i].enabled = normalStates[i];
            }
        }

        private void SetLightsEnabled(bool enabled)
        {
            if (lights == null)
            {
                return;
            }

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null) lights[i].enabled = enabled;
            }
        }
    }
}
