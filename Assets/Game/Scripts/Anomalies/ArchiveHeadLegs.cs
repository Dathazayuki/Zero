using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArchiveHeadLegs : MonoBehaviour
    {
        [SerializeField] private Transform[] hips;
        [SerializeField, HideInInspector] private Quaternion[] rest;
        public int Count => hips != null ? hips.Length : 0;

        public void Configure(Transform[] joints)
        {
            hips = joints;
            rest = null;
            Capture();
        }

        private void Awake() => Capture();

        private void Capture()
        {
            if (rest != null || hips == null) return;
            rest = new Quaternion[hips.Length];
            for (int i = 0; i < hips.Length; i++) rest[i] = hips[i].localRotation;
        }

        public void Present(float growth, float gait)
        {
            Capture();
            if (rest == null) return;
            for (int i = 0; i < hips.Length; i++) Pose(i, growth, gait);
        }

        private void Pose(int index, float growth, float gait)
        {
            if (hips[index] == null) return;
            float scale = Mathf.Max(0.001f, growth);
            hips[index].localScale = Vector3.one * scale;
            float step = Mathf.Sin(gait + index * Mathf.PI) * Mathf.Clamp01(gait) * 18f;
            hips[index].localRotation = rest[index] * Quaternion.Euler(step, 0f, (1f - growth) * 65f);
        }

        private void OnDisable() => Present(0f, 0f);
    }
}
