using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class WatchfulHallExitGate : MonoBehaviour
    {
        [SerializeField] private Transform leftHinge;
        [SerializeField] private Transform rightHinge;
        [SerializeField] private Transform[] lockGlyphs;
        [SerializeField] private Material chargedMaterial;
        [SerializeField, Min(1f)] private float openDegreesPerSecond = 90f;
        private Renderer[][] glyphRenderers;
        private Material[][] originalMaterials;
        private readonly bool[] shown = new bool[4];
        private Quaternion leftClosed;
        private Quaternion rightClosed;
        private bool unlocked;
        public bool IsReady => leftHinge != null && rightHinge != null && chargedMaterial != null
            && lockGlyphs != null && lockGlyphs.Length == 4;

        private void Awake()
        {
            if (!IsReady) return;
            leftClosed = leftHinge.localRotation;
            rightClosed = rightHinge.localRotation;
            glyphRenderers = new Renderer[4][];
            originalMaterials = new Material[4][];
            for (int i = 0; i < 4; i++) CacheGlyph(i);
        }

        private void CacheGlyph(int index)
        {
            glyphRenderers[index] = lockGlyphs[index] != null
                ? lockGlyphs[index].GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            originalMaterials[index] = new Material[glyphRenderers[index].Length];
            for (int i = 0; i < glyphRenderers[index].Length; i++)
                originalMaterials[index][i] = glyphRenderers[index][i].sharedMaterial;
        }

        public void Present(WatchfulHallChargeRules rules)
        {
            if (!IsReady || glyphRenderers == null) return;
            unlocked = rules.IsUnlocked;
            for (int i = 0; i < 4; i++) SetLock(i, rules.IsCharged(i));
        }

        private void SetLock(int index, bool complete)
        {
            if (shown[index] == complete) return;
            shown[index] = complete;
            for (int i = 0; i < glyphRenderers[index].Length; i++)
                glyphRenderers[index][i].sharedMaterial = complete ? chargedMaterial : originalMaterials[index][i];
        }

        private void Update()
        {
            if (!IsReady || !unlocked) return;
            float step = openDegreesPerSecond * Time.deltaTime;
            leftHinge.localRotation = Quaternion.RotateTowards(leftHinge.localRotation, leftClosed * Quaternion.Euler(0f, -90f, 0f), step);
            rightHinge.localRotation = Quaternion.RotateTowards(rightHinge.localRotation, rightClosed * Quaternion.Euler(0f, 90f, 0f), step);
        }

        public bool HasPassed(Vector3 position)
        {
            if (!unlocked) return false;
            Vector3 local = transform.InverseTransformPoint(position);
            return local.z > 1.4f && local.z < 5f && Mathf.Abs(local.x) < 1.5f;
        }

        public void ResetPresentation()
        {
            if (!IsReady || glyphRenderers == null) return;
            unlocked = false;
            leftHinge.localRotation = leftClosed;
            rightHinge.localRotation = rightClosed;
            for (int i = 0; i < 4; i++) SetLock(i, false);
        }

        private void OnDisable() => ResetPresentation();
    }
}
