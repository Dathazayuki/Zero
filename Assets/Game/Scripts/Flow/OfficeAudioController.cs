using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class OfficeAudioController : MonoBehaviour
    {
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource oneShotSource;
        [SerializeField] private AudioClip ambienceClip;
        [SerializeField] private AudioClip typingClip;
        [SerializeField] private AudioClip confirmationClip;
        [SerializeField] private AudioClip chairClip;
        [SerializeField] private AudioClip elevatorErrorClip;
        [SerializeField] private AudioClip transitionClip;

        private const int SampleRate = 22050;
        private SourceSoundPlayback ambiencePlayback;
        private SourceSoundPlayback effectsPlayback;

        private void Awake()
        {
            EnsureSources();
            EnsureFallbackClips();
            AudioPlaybackService service = GameFlowController.Instance != null ? GameFlowController.Instance.Audio : null;
            ambiencePlayback = new SourceSoundPlayback(ambienceSource, service, AudioBus.Ambience);
            effectsPlayback = new SourceSoundPlayback(oneShotSource, service, AudioBus.Sfx);
        }

        public void BeginAmbience()
        {
            EnsureSources();
            EnsureFallbackClips();
            GameAudioBootstrap.BindSource(ref ambiencePlayback, ambienceSource, AudioBus.Ambience, false);
            if (!ambienceSource.isPlaying)
            {
                ambienceSource.loop = true;
                ambiencePlayback.Play("office.ambience", ambienceClip, false);
            }
        }

        public void PlayTyping() => PlayEffect("office.typing", typingClip);
        public void PlayConfirmation() => PlayEffect("office.confirmation", confirmationClip);
        public void PlayChair() => PlayEffect("office.chair", chairClip);
        public void PlayElevatorError() => PlayEffect("office.elevator_error", elevatorErrorClip);
        public void PlayTransition() => PlayEffect("office.transition", transitionClip);

        private void PlayEffect(string id, AudioClip clip)
        {
            GameAudioBootstrap.BindSource(ref effectsPlayback, oneShotSource, AudioBus.Sfx, false).Play(id, clip);
        }

        private void OnDisable()
        {
            ambiencePlayback?.Stop();
            effectsPlayback?.Stop();
        }

        private void EnsureSources()
        {
            if (ambienceSource == null)
            {
                ambienceSource = gameObject.AddComponent<AudioSource>();
                ambienceSource.playOnAwake = false;
                ambienceSource.loop = true;
                ambienceSource.spatialBlend = 0f;
                ambienceSource.volume = 0.35f;
            }

            if (oneShotSource == null)
            {
                oneShotSource = gameObject.AddComponent<AudioSource>();
                oneShotSource.playOnAwake = false;
                oneShotSource.loop = false;
                oneShotSource.spatialBlend = 0f;
                oneShotSource.volume = 0.65f;
            }
        }

        private void EnsureFallbackClips()
        {
            ambienceClip ??= CreateAmbience();
            typingClip ??= CreateTyping();
            confirmationClip ??= CreateTone("Office Confirm", 680f, 0.14f, 0.12f);
            chairClip ??= CreateChairScrape();
            elevatorErrorClip ??= CreateTone("Elevator Error", 185f, 0.42f, 0.18f);
            transitionClip ??= CreateTransition();
        }

        private static AudioClip CreateAmbience()
        {
            const float duration = 4f;
            int count = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[count];
            var random = new System.Random(2001);
            for (int i = 0; i < count; i++)
            {
                float time = i / (float)SampleRate;
                float hum = Mathf.Sin(time * Mathf.PI * 2f * 50f) * 0.035f;
                float ballast = Mathf.Sin(time * Mathf.PI * 2f * 100f) * 0.012f;
                float noise = ((float)random.NextDouble() * 2f - 1f) * 0.006f;
                samples[i] = hum + ballast + noise;
            }

            return MakeClip("Office HVAC Hum", samples);
        }

        private static AudioClip CreateTyping()
        {
            const float duration = 1.35f;
            int count = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[count];
            var random = new System.Random(2357);
            float[] hits = { 0.04f, 0.13f, 0.21f, 0.34f, 0.42f, 0.58f, 0.69f, 0.77f, 0.94f, 1.08f, 1.19f };
            for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
            {
                int start = Mathf.FloorToInt(hits[hitIndex] * SampleRate);
                int length = Mathf.FloorToInt(0.035f * SampleRate);
                for (int i = 0; i < length && start + i < count; i++)
                {
                    float envelope = Mathf.Exp(-10f * i / Mathf.Max(1f, length));
                    float noise = (float)random.NextDouble() * 2f - 1f;
                    samples[start + i] += noise * envelope * 0.22f;
                }
            }

            return MakeClip("Office Keyboard", samples);
        }

        private static AudioClip CreateChairScrape()
        {
            const float duration = 0.72f;
            int count = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[count];
            var random = new System.Random(91);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float envelope = Mathf.Sin(t * Mathf.PI) * (1f - t);
                float noise = (float)random.NextDouble() * 2f - 1f;
                samples[i] = (noise * 0.1f + Mathf.Sin(i * 0.027f) * 0.06f) * envelope;
            }

            return MakeClip("Office Chair", samples);
        }

        private static AudioClip CreateTransition()
        {
            const float duration = 1.6f;
            int count = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float frequency = Mathf.Lerp(48f, 92f, t * t);
                float envelope = Mathf.SmoothStep(0f, 1f, t) * (1f - Mathf.Clamp01((t - 0.82f) / 0.18f));
                samples[i] = (Mathf.Sin(i * Mathf.PI * 2f * frequency / SampleRate) * 0.16f +
                              Mathf.Sin(i * Mathf.PI * 2f * (frequency * 0.51f) / SampleRate) * 0.1f) * envelope;
            }

            return MakeClip("Office Transition Drone", samples);
        }

        private static AudioClip CreateTone(string name, float frequency, float duration, float volume)
        {
            int count = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count;
                float envelope = Mathf.Sin(t * Mathf.PI);
                samples[i] = Mathf.Sin(i * Mathf.PI * 2f * frequency / SampleRate) * envelope * volume;
            }

            return MakeClip(name, samples);
        }

        private static AudioClip MakeClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
