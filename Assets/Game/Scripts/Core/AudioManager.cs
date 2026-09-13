using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Core
{
    public sealed class AudioManager : MonoBehaviour
    {
        private AudioClip fallbackFootsteps;

        public AudioClip GetFallbackFootsteps()
        {
            if (fallbackFootsteps != null)
            {
                return fallbackFootsteps;
            }

            const int sampleRate = 22050;
            const float duration = 1.65f;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];
            float[] stepTimes = { 0.08f, 0.44f, 0.81f, 1.19f };
            var random = new System.Random(1979);

            for (int stepIndex = 0; stepIndex < stepTimes.Length; stepIndex++)
            {
                int start = Mathf.FloorToInt(stepTimes[stepIndex] * sampleRate);
                int length = Mathf.FloorToInt(0.16f * sampleRate);
                for (int i = 0; i < length && start + i < samples.Length; i++)
                {
                    float normalized = i / (float)length;
                    float envelope = Mathf.Exp(-7f * normalized);
                    float tone = Mathf.Sin(2f * Mathf.PI * 72f * i / sampleRate);
                    float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                    samples[start + i] += (tone * 0.28f + noise * 0.12f) * envelope;
                }
            }

            fallbackFootsteps = AudioClip.Create("Procedural Footsteps", sampleCount, 1, sampleRate, false);
            fallbackFootsteps.SetData(samples, 0);
            return fallbackFootsteps;
        }
    }
}
