using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class WatchfulHallChargePad : MonoBehaviour
    {
        [SerializeField] private BoxCollider zone;
        [SerializeField] private Slider gauge;
        [SerializeField] private Renderer[] signals;
        [SerializeField] private Material chargedMaterial;
        [SerializeField] private TMP_Text statusLabel;
        private Material[] originalMaterials;
        private MaterialPropertyBlock[] originalBlocks;
        private MaterialPropertyBlock pulseBlock;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private enum Feedback { Idle, Paused, Charging, Complete }
        private Feedback feedback;
        private string idleLabel;
        private Color idleLabelColor;
        private float pulseStartedAt;
        public bool IsReady => zone != null && gauge != null && chargedMaterial != null && signals != null && signals.Length > 0;

        private void Awake()
        {
            if (!IsReady) return;
            originalMaterials = new Material[signals.Length];
            originalBlocks = new MaterialPropertyBlock[signals.Length];
            pulseBlock = new MaterialPropertyBlock();
            for (int i = 0; i < signals.Length; i++) CacheSignal(i);
            if (statusLabel == null) return;
            idleLabel = statusLabel.text;
            idleLabelColor = statusLabel.color;
        }

        private void CacheSignal(int index)
        {
            if (signals[index] == null) return;
            originalMaterials[index] = signals[index].sharedMaterial;
            originalBlocks[index] = new MaterialPropertyBlock();
            signals[index].GetPropertyBlock(originalBlocks[index]);
        }

        public bool ContainsFeet(Vector3 feet)
        {
            if (!IsReady || !isActiveAndEnabled || !zone.enabled) return false;
            Vector3 local = zone.transform.InverseTransformPoint(feet) - zone.center;
            Vector3 half = zone.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.z) <= half.z
                && local.y >= -half.y - 0.08f && local.y <= -half.y + 0.3f;
        }

        public void Present(float seconds, float duration, bool complete, bool charging = false)
        {
            if (!IsReady || originalMaterials == null) return;
            gauge.maxValue = duration;
            gauge.SetValueWithoutNotify(seconds);
            Feedback next = complete ? Feedback.Complete : charging ? Feedback.Charging : seconds > 0f ? Feedback.Paused : Feedback.Idle;
            SetFeedback(next);
            if (feedback == Feedback.Charging) PulseSignals();
        }

        private void SetFeedback(Feedback next)
        {
            if (feedback == next) return;
            RestoreSignals(next == Feedback.Complete);
            feedback = next;
            pulseStartedAt = Time.time;
            if (statusLabel == null) return;
            switch (feedback)
            {
                case Feedback.Charging: statusLabel.SetText("CHARGING"); break;
                case Feedback.Paused: statusLabel.SetText("PAUSED"); break;
                case Feedback.Complete: statusLabel.SetText("ACTIVATED"); break;
                default: statusLabel.SetText(idleLabel); break;
            }
            statusLabel.color = feedback == Feedback.Charging ? new Color(1f, 0.75f, 0.2f) : idleLabelColor;
        }

        private void PulseSignals()
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin((Time.time - pulseStartedAt) * Mathf.PI * 2f);
            Color amber = new Color(1f, 0.45f, 0.025f);
            pulseBlock.SetColor(BaseColor, amber * Mathf.Lerp(0.45f, 1f, pulse));
            pulseBlock.SetColor(EmissionColor, amber * Mathf.Lerp(0.5f, 2f, pulse));
            for (int i = 0; i < signals.Length; i++)
                if (signals[i] != null) signals[i].SetPropertyBlock(pulseBlock);
        }

        private void RestoreSignals(bool complete)
        {
            for (int i = 0; i < signals.Length; i++)
            {
                if (signals[i] == null) continue;
                signals[i].SetPropertyBlock(originalBlocks[i]);
                signals[i].sharedMaterial = complete ? chargedMaterial : originalMaterials[i];
            }
        }

        private void OnDisable() => Present(0f, gauge != null ? gauge.maxValue : 15f, false);
    }
}
