using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class SceneFadeController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image overlayImage;
        [SerializeField, Min(0)] private int sortingOrder = 32000;

        public float Alpha => canvasGroup != null ? canvasGroup.alpha : 0f;
        public Color Color => overlayImage != null ? overlayImage.color : UnityEngine.Color.black;

        private void Awake()
        {
            EnsureOverlay();
        }

        public void SetImmediate(float alpha)
        {
            EnsureOverlay();
            canvasGroup.alpha = Mathf.Clamp01(alpha);
            canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.001f;
            canvasGroup.interactable = false;
        }

        public void SetColor(Color color)
        {
            EnsureOverlay();
            overlayImage.color = color;
        }

        public IEnumerator FadeToColor(Color color, float targetAlpha, float duration)
        {
            SetColor(color);
            yield return FadeTo(targetAlpha, duration);
        }

        public IEnumerator TransitionOpaqueColor(Color from, Color to, float duration)
        {
            EnsureOverlay();
            SetImmediate(1f);
            duration = Mathf.Max(0f, duration);
            if (duration <= 0f)
            {
                SetColor(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                SetColor(UnityEngine.Color.Lerp(from, to, t));
                yield return null;
            }

            SetColor(to);
        }

        public IEnumerator FadeFromBlackGradual(float duration)
        {
            EnsureOverlay();
            SetColor(Color.black);
            SetImmediate(1f);
            duration = Mathf.Max(0f, duration);

            if (duration <= 0f)
            {
                SetImmediate(0f);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                // Perceptual brightness curve: transmitted light increases quadratically
                // with smoothT, so perceived brightness increases linearly/gradually.
                float transmittedLight = smoothT * smoothT;
                SetImmediate(1f - transmittedLight);
                yield return null;
            }

            SetImmediate(0f);
        }

        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            EnsureOverlay();
            float startAlpha = canvasGroup.alpha;
            targetAlpha = Mathf.Clamp01(targetAlpha);
            duration = Mathf.Max(0f, duration);

            if (duration <= 0f)
            {
                SetImmediate(targetAlpha);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                SetImmediate(Mathf.Lerp(startAlpha, targetAlpha, t));
                yield return null;
            }

            SetImmediate(targetAlpha);
        }

        private void EnsureOverlay()
        {
            if (canvasGroup != null && overlayImage != null)
            {
                return;
            }

            if (canvasGroup != null)
            {
                overlayImage = canvasGroup.GetComponentInChildren<Image>(true);
                if (overlayImage != null)
                {
                    return;
                }
            }

            GameObject canvasObject = new GameObject("PersistentSceneFadeCanvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvasObject.AddComponent<CanvasScaler>();

            canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = false;

            GameObject imageObject = new GameObject("BlackoutImage");
            imageObject.transform.SetParent(canvasObject.transform, false);
            overlayImage = imageObject.AddComponent<Image>();
            overlayImage.color = UnityEngine.Color.black;
            overlayImage.raycastTarget = false;

            RectTransform rect = overlayImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
