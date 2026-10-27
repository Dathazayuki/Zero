using System;
using UnityEngine;
using Zero.AnomalyPrototype.Interaction;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenOvenOpeningAnomaly : AnomalyBehaviour
    {
        private enum Phase { Waiting, Opening, Sliding, Presented }
        [SerializeField] private Transform player;
        [SerializeField] private ApplianceDoor door;
        [SerializeField] private Transform tray;
        [SerializeField] private GameObject presentation;
        [SerializeField, Min(0.1f)] private float activationDistance = 1.8f;
        [SerializeField, Min(0.05f)] private float openingDuration = 0.18f;
        [SerializeField, Min(0.05f)] private float slideDuration = 0.7f;
        [SerializeField] private Vector3 trayLocalOffset = new Vector3(0f, 0f, -0.52f);
        [NonSerialized] private bool captured;
        private bool normalOpen, normalInteraction;
        private Vector3 normalTrayPosition;
        private float elapsed;
        private Phase phase;
        private bool normalPresentationActive;
        private ParticleSystem[] particles = Array.Empty<ParticleSystem>();
        public bool HasTriggered => phase != Phase.Waiting;
        public bool IsConfigured => player != null && door != null && tray != null;

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            normalTrayPosition = tray.localPosition;
            normalOpen = door.IsOpen;
            normalInteraction = door.InteractionEnabled;
            normalPresentationActive = presentation != null && presentation.activeSelf;
            if (presentation != null) particles = presentation.GetComponentsInChildren<ParticleSystem>(true);
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured || !IsConfigured) return;
            door.SetOpen(false, 0f);
            door.SetInteractionEnabled(false);
            if (presentation != null) presentation.SetActive(true);
        }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            if (!IsActive || !captured || !IsConfigured || phase == Phase.Presented) return;
            if (phase == Phase.Waiting)
            {
                Vector3 offset = player.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude > activationDistance * activationDistance) return;
                phase = Phase.Opening;
                door.SetOpen(true, openingDuration);
                return;
            }
            elapsed += Mathf.Max(0f, deltaTime);
            if (elapsed < openingDuration) return;
            if (phase == Phase.Opening)
                for (int i = 0; i < particles.Length; i++) if (particles[i] != null) particles[i].Play(true);
            float t = Mathf.Clamp01((elapsed - openingDuration) / Mathf.Max(0.05f, slideDuration));
            phase = t >= 1f ? Phase.Presented : Phase.Sliding;
            tray.localPosition = normalTrayPosition + trayLocalOffset * Mathf.SmoothStep(0f, 1f, t);
        }

        protected override void OnResetAnomaly()
        {
            elapsed = 0f;
            phase = Phase.Waiting;
            if (!captured) return;
            for (int i = 0; i < particles.Length; i++)
                if (particles[i] != null) particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (presentation != null) presentation.SetActive(normalPresentationActive);
            if (tray != null) tray.localPosition = normalTrayPosition;
            if (door == null) return;
            door.SetOpen(normalOpen, 0f);
            door.SetInteractionEnabled(normalInteraction);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
