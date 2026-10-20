using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.AnomalyPrototype.Demo
{
    public sealed class LivingRoomAnomalyPreview : MonoBehaviour
    {
        [SerializeField] private AnomalyBehaviour[] anomalies;
        private GUIStyle labelStyle;
        private string status = "F3: next anomaly    F2: normal room";
        public int CurrentIndex { get; private set; } = -1;
        public bool IsReady { get; private set; }
        public IReadOnlyList<AnomalyBehaviour> Anomalies => anomalies;
        public AnomalyBehaviour Current => CurrentIndex >= 0 ? anomalies[CurrentIndex] : null;

        private void Start()
        {
            IsReady = anomalies != null && anomalies.Length > 0;
            if (!IsReady) return;
            for (int i = 0; i < anomalies.Length; i++)
            {
                if (anomalies[i] == null || anomalies[i].Data == null) { IsReady = false; break; }
                anomalies[i].CaptureNormalState();
            }
            if (!IsReady) Debug.LogError("LIVING_ROOM_PREVIEW_CONFIGURATION_INVALID: authored anomalies and data required", this);
        }

        private void Update()
        {
            if (!IsReady || Keyboard.current == null || Time.timeScale <= 0f) return;
            if (Keyboard.current.f2Key.wasPressedThisFrame) ResetPreview();
            else if (Keyboard.current.f3Key.wasPressedThisFrame) CycleNext();
        }

        public bool CycleNext()
        {
            if (!IsReady || !isActiveAndEnabled) return false;
            int next = (CurrentIndex + 1) % anomalies.Length;
            Current?.ResetAnomaly();
            CurrentIndex = next;
            Current.Activate();
            status = $"{CurrentIndex + 1}/{anomalies.Length}  {Current.Data.Id} - {Current.Data.DisplayName}\nF3: next    F2: normal room";
            Debug.Log($"LIVING_ROOM_PREVIEW_SELECTED: index={CurrentIndex + 1}, id={Current.Data.Id}", this);
            return true;
        }

        public void ResetPreview()
        {
            Current?.ResetAnomaly();
            CurrentIndex = -1;
            status = "NORMAL ROOM    F3: next anomaly    F2: normal room";
        }

        private void OnGUI()
        {
            if (!IsReady) return;
            if (labelStyle == null) labelStyle = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
            GUI.Box(new Rect(12f, 12f, 680f, 60f), status, labelStyle);
        }

        private void OnDisable() => ResetPreview();
    }
}
