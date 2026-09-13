using System.Collections;
using UnityEngine;

namespace Zero.AnomalyPrototype.HorrorEvents
{
    public sealed class LightFlickerEvent : HorrorEventBehaviour
    {
        [SerializeField] private Light[] lights;
        [SerializeField, Min(1)] private int flickerCount = 4;
        [SerializeField, Min(0.02f)] private float interval = 0.11f;
        private bool[] normalStates;
        private Coroutine routine;

        public void Configure(Light[] controlledLights, int count = 4, float flickerInterval = 0.11f)
        {
            lights = controlledLights;
            flickerCount = Mathf.Max(1, count);
            interval = Mathf.Max(0.02f, flickerInterval);
            CaptureStates();
        }

        public override void CaptureNormalState()
        {
            CaptureStates();
        }

        protected override void OnPlay()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(FlickerRoutine());
        }

        protected override void OnResetEvent()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            RestoreStates();
        }

        private IEnumerator FlickerRoutine()
        {
            for (int i = 0; i < flickerCount * 2; i++)
            {
                SetLightsEnabled(i % 2 != 0);
                yield return new WaitForSeconds(interval);
            }

            RestoreStates();
            routine = null;
        }

        private void CaptureStates()
        {
            if (lights == null)
            {
                normalStates = null;
                return;
            }

            normalStates = new bool[lights.Length];
            for (int i = 0; i < lights.Length; i++)
            {
                normalStates[i] = lights[i] != null && lights[i].enabled;
            }
        }

        private void RestoreStates()
        {
            if (lights == null || normalStates == null)
            {
                return;
            }

            for (int i = 0; i < lights.Length && i < normalStates.Length; i++)
            {
                if (lights[i] != null) lights[i].enabled = normalStates[i];
            }
        }

        private void SetLightsEnabled(bool enabled)
        {
            if (lights == null)
            {
                return;
            }

            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null) lights[i].enabled = enabled;
            }
        }
    }
}
