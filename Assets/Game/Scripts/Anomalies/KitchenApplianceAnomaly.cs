using System;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;
using Zero.AnomalyPrototype.Interaction;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenApplianceAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform appliance;
        [SerializeField] private Transform player;
        [SerializeField] private GameObject effects;
        [SerializeField] private AudioSource[] sounds = Array.Empty<AudioSource>();
        [SerializeField] private ApplianceDoor[] doors = Array.Empty<ApplianceDoor>();
        [SerializeField, Min(0.1f)] private float activationDistance = 2.5f;
        [SerializeField] private bool stayOnAfterApproach;
        [SerializeField, Min(0f)] private float shakeDistance;
        [SerializeField, Min(0f)] private float shakeDegrees;
        private Vector3 normalPosition;
        private Quaternion normalRotation;
        private bool[] doorStates;
        private SourceSoundPlayback[] playback;
        private Light[] lights;
        private float[] intensities;
        private ParticleSystem[] particles;
        private bool captured, normalEffectsActive;
        private float elapsed;

        public bool IsPresenting { get; private set; }
        public bool IsConfigured => appliance != null && player != null && effects != null;

        public void Configure(Transform target, Transform observer, GameObject presentation)
        {
            appliance = target;
            player = observer;
            effects = presentation;
        }

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            normalPosition = appliance.localPosition;
            normalRotation = appliance.localRotation;
            normalEffectsActive = effects.activeSelf;
            particles = effects.GetComponentsInChildren<ParticleSystem>(true);
            lights = effects.GetComponentsInChildren<Light>(true);
            intensities = new float[lights.Length];
            for (int i = 0; i < lights.Length; i++) intensities[i] = lights[i].intensity;
            playback = new SourceSoundPlayback[sounds.Length];
            for (int i = 0; i < sounds.Length; i++)
                if (sounds[i] != null) GameAudioBootstrap.BindSource(ref playback[i], sounds[i]);
            doorStates = new bool[doors.Length];
            for (int i = 0; i < doors.Length; i++)
                if (doors[i] != null) doorStates[i] = doors[i].enabled;
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured) return;
            effects.SetActive(false);
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] == null) continue;
                doors[i].ResetDoor();
                doors[i].enabled = false;
            }
        }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (!IsActive || !captured || !IsConfigured) return;
            Vector3 offset = player.position - appliance.position;
            offset.y = 0f;
            bool near = offset.sqrMagnitude <= activationDistance * activationDistance;
            SetPresentation(near || (stayOnAfterApproach && IsPresenting));
            if (!IsPresenting) return;
            elapsed += Mathf.Max(0f, deltaTime);
            float pulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(elapsed * 2.7f)), 2f);
            float jerk = Mathf.Sin(elapsed * 37f) * pulse;
            appliance.localPosition = normalPosition + new Vector3(jerk, Mathf.Abs(jerk) * 0.2f, -jerk * 0.6f) * shakeDistance;
            appliance.localRotation = normalRotation * Quaternion.Euler(jerk * shakeDegrees, 0f, jerk * shakeDegrees * 0.7f);
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) lights[i].intensity = intensities[i] * (0.85f + Mathf.PerlinNoise(elapsed * 8f, i) * 0.3f);
        }

        private void SetPresentation(bool active)
        {
            if (IsPresenting == active) return;
            IsPresenting = active;
            effects.SetActive(active);
            for (int i = 0; i < sounds.Length; i++)
            {
                if (sounds[i] == null) continue;
                if (active) playback[i].Play(null, sounds[i].clip, false);
                else playback[i].Stop();
            }
            if (active)
            {
                for (int i = 0; i < particles.Length; i++) particles[i].Play(true);
            }
            else RestorePose();
        }

        private void RestorePose()
        {
            if (appliance != null) appliance.SetLocalPositionAndRotation(normalPosition, normalRotation);
        }

        protected override void OnResetAnomaly()
        {
            if (!captured) return;
            IsPresenting = false;
            elapsed = 0f;
            for (int i = 0; i < playback.Length; i++) playback[i]?.Stop();
            for (int i = 0; i < particles.Length; i++)
                if (particles[i] != null) particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) lights[i].intensity = intensities[i];
            if (effects != null) effects.SetActive(normalEffectsActive);
            for (int i = 0; i < doors.Length; i++)
                if (doors[i] != null) doors[i].enabled = doorStates[i];
            RestorePose();
        }

        private void OnDisable() => ResetAnomaly();
    }
}
