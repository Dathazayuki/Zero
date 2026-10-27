using System;
using UnityEngine;
using Zero.AnomalyPrototype.Interaction;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenFridgeOpeningAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private ApplianceDoor[] doors = Array.Empty<ApplianceDoor>();
        [SerializeField, Min(0.1f)] private float activationDistance = 1.8f;
        [SerializeField, Min(0.05f)] private float openingDuration = 0.16f;
        [NonSerialized] private bool captured;
        private bool[] normalOpen, normalInteraction;
        public bool HasOpened { get; private set; }
        public bool IsConfigured => player != null && doors != null && doors.Length == 2 &&
            doors[0] != null && doors[1] != null;

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            normalOpen = new bool[doors.Length];
            normalInteraction = new bool[doors.Length];
            for (int i = 0; i < doors.Length; i++)
            {
                normalOpen[i] = doors[i].IsOpen;
                normalInteraction[i] = doors[i].InteractionEnabled;
            }
            captured = true;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured || !IsConfigured) return;
            for (int i = 0; i < doors.Length; i++)
            {
                doors[i].SetOpen(false, 0f);
                doors[i].SetInteractionEnabled(false);
            }
        }

        private void Update() => CheckApproach();

        public void CheckApproach()
        {
            if (!IsActive || !captured || !IsConfigured || HasOpened) return;
            Vector3 offset = player.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > activationDistance * activationDistance) return;
            HasOpened = true;
            for (int i = 0; i < doors.Length; i++) doors[i].SetOpen(true, openingDuration);
        }

        protected override void OnResetAnomaly()
        {
            HasOpened = false;
            if (!captured) return;
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] == null) continue;
                doors[i].SetOpen(normalOpen[i], 0f);
                doors[i].SetInteractionEnabled(normalInteraction[i]);
            }
        }

        private void OnDisable() => ResetAnomaly();
    }
}
