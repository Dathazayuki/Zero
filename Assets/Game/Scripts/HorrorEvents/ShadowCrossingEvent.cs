using System.Collections;
using UnityEngine;

namespace Zero.AnomalyPrototype.HorrorEvents
{
    public sealed class ShadowCrossingEvent : HorrorEventBehaviour
    {
        [SerializeField] private Transform silhouette;
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform endPoint;
        [SerializeField, Min(0.1f)] private float crossingDuration = 0.65f;
        private Coroutine routine;

        public void Configure(Transform shadow, Transform start, Transform end, float duration = 0.65f)
        {
            silhouette = shadow;
            startPoint = start;
            endPoint = end;
            crossingDuration = Mathf.Max(0.1f, duration);
            ResetSilhouette();
        }

        protected override void OnPlay()
        {
            if (silhouette == null || startPoint == null || endPoint == null)
            {
                return;
            }

            if (routine != null)
            {
                StopCoroutine(routine);
            }

            routine = StartCoroutine(CrossingRoutine());
        }

        protected override void OnResetEvent()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            ResetSilhouette();
        }

        private IEnumerator CrossingRoutine()
        {
            silhouette.position = startPoint.position;
            silhouette.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < crossingDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / crossingDuration);
                silhouette.position = Vector3.Lerp(startPoint.position, endPoint.position, t);
                yield return null;
            }

            silhouette.gameObject.SetActive(false);
            routine = null;
        }

        private void ResetSilhouette()
        {
            if (silhouette == null)
            {
                return;
            }

            if (startPoint != null)
            {
                silhouette.position = startPoint.position;
            }

            silhouette.gameObject.SetActive(false);
        }
    }
}
