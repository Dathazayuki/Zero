using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class LivingRoomArrangementAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform[] targets = Array.Empty<Transform>();
        [SerializeField] private Vector3[] localOffsets = Array.Empty<Vector3>();
        [SerializeField] private Vector3[] rotationOffsets = Array.Empty<Vector3>();
        [SerializeField] private GameObject[] shownObjects = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] hiddenObjects = Array.Empty<GameObject>();
        private Vector3[] positions;
        private Quaternion[] rotations;
        private bool[] shownState;
        private bool[] hiddenState;
        private ParticleSystem[] particles;
        private bool captured;
        public bool IsConfigured => localOffsets != null && rotationOffsets != null && ReferencesPresent(targets) &&
            targets.Length == localOffsets.Length && targets.Length == rotationOffsets.Length &&
            ReferencesPresent(shownObjects) && ReferencesPresent(hiddenObjects);

        private static bool ReferencesPresent(UnityEngine.Object[] references)
        {
            if (references == null) return false;
            for (int i = 0; i < references.Length; i++) if (references[i] == null) return false;
            return true;
        }

        public void Configure(Transform[] moved, Vector3[] offsets, Vector3[] turns, GameObject[] show, GameObject[] hide)
        {
            targets = moved;
            localOffsets = offsets;
            rotationOffsets = turns;
            shownObjects = show;
            hiddenObjects = hide;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            if (captured || !IsConfigured) return;
            positions = new Vector3[targets.Length];
            rotations = new Quaternion[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                positions[i] = targets[i].localPosition;
                rotations[i] = targets[i].localRotation;
            }
            shownState = CaptureActive(shownObjects);
            hiddenState = CaptureActive(hiddenObjects);
            particles = GetComponentsInChildren<ParticleSystem>(true);
            captured = true;
        }

        private static bool[] CaptureActive(GameObject[] objects)
        {
            var state = new bool[objects.Length];
            for (int i = 0; i < objects.Length; i++) state[i] = objects[i].activeSelf;
            return state;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (!captured || !IsConfigured) return;
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].localPosition = positions[i] + localOffsets[i];
                targets[i].localRotation = rotations[i] * Quaternion.Euler(rotationOffsets[i]);
            }
            for (int i = 0; i < shownObjects.Length; i++) shownObjects[i].SetActive(true);
            for (int i = 0; i < hiddenObjects.Length; i++) hiddenObjects[i].SetActive(false);
            for (int i = 0; i < particles.Length; i++) particles[i].Play(true);
        }

        protected override void OnResetAnomaly()
        {
            if (!captured) return;
            for (int i = 0; i < particles.Length; i++)
                if (particles[i] != null) particles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                targets[i].localPosition = positions[i];
                targets[i].localRotation = rotations[i];
            }
            for (int i = 0; i < shownObjects.Length; i++)
                if (shownObjects[i] != null) shownObjects[i].SetActive(shownState[i]);
            for (int i = 0; i < hiddenObjects.Length; i++)
                if (hiddenObjects[i] != null) hiddenObjects[i].SetActive(hiddenState[i]);
        }

        private void OnDisable() => ResetAnomaly();
    }
}
