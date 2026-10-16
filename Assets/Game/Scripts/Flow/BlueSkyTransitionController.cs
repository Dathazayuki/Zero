using System.Collections;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class BlueSkyTransitionController : MonoBehaviour
    {
        private SourceSoundPlayback windPlayback;
        [SerializeField] private Room2LightController room2Lights;
        [SerializeField] private Light[] skyLights = new Light[2];
        [SerializeField] private AudioSource fluorescentHum;
        [SerializeField] private AudioSource wind;
        [SerializeField, Min(0.01f)] private float duration = 2.5f;
        private float[] intensities;
        private float humVolume;
        private float windVolume;
        private bool started;
        private void Awake()
        {
            intensities = new float[skyLights.Length];
            for (int i = 0; i < skyLights.Length; i++) if (skyLights[i] != null) { intensities[i] = skyLights[i].intensity; skyLights[i].intensity = 0f; }
            humVolume = fluorescentHum != null ? fluorescentHum.volume : 0f;
            windVolume = wind != null ? wind.volume : 0f;
            if (wind != null) { wind.volume = 0f; wind.loop = true; }
        }
        public void Begin()
        {
            if (started) return;
            started = true;
            StartCoroutine(Transition());
        }
        private IEnumerator Transition()
        {
            if (wind != null) GameAudioBootstrap.BindSource(ref windPlayback, wind, AudioBus.Ambience, false).Play(null, wind.clip, false);
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            { Apply(Mathf.SmoothStep(0f, 1f, elapsed / duration)); yield return null; }
            Apply(1f);
            if (fluorescentHum != null) fluorescentHum.Stop();
        }
        private void Apply(float t)
        {
            if (room2Lights != null) room2Lights.SetMasterIntensity(1f - t);
            for (int i = 0; i < skyLights.Length; i++) if (skyLights[i] != null) skyLights[i].intensity = intensities[i] * t;
            if (fluorescentHum != null) fluorescentHum.volume = humVolume * (1f - t);
            if (wind != null) wind.volume = windVolume * t;
        }
        private void OnDisable() { StopAllCoroutines(); if (wind != null) wind.Stop(); }
    }
}
