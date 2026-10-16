using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class EndingTitlePresenter : MonoBehaviour
    {
        private const float FadeDuration = 0.45f;
        private const float HoldDuration = 3.25f;

        private CanvasGroup group;
        private TextMeshProUGUI label;
        private Coroutine sequence;

        public bool IsShowing => sequence != null;

        public bool Show(EndingIdentity ending, GameFlowController flow)
        {
            if (sequence != null || flow == null) return false;
            EnsureCanvas();
            label.text = EndingTitleCatalog.GetTitle(ending);
            group.alpha = 0f;
            group.gameObject.SetActive(true);
            sequence = StartCoroutine(ShowThenReturnToTitle(flow));
            Debug.Log($"ENDING_TITLE_SHOWN: ending={ending}", this);
            return true;
        }

        private IEnumerator ShowThenReturnToTitle(GameFlowController flow)
        {
            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / FadeDuration));
                yield return null;
            }

            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(HoldDuration);
            while (flow != null && flow.IsTransitioning) yield return null;
            if (flow == null) yield break;

            SceneFadeController fade = flow.FadeController;
            if (fade != null)
            {
                fade.SetColor(Color.black);
                fade.SetImmediate(1f);
            }

            if (!flow.ReturnToTitleAfterEnding())
            {
                Debug.LogError("ENDING_TITLE_RETURN_FAILED: TitleScene is unavailable", this);
                sequence = null;
                yield break;
            }

            group.gameObject.SetActive(false);
            sequence = null;
            Debug.Log("ENDING_TITLE_RETURN_TO_TITLE_STARTED", this);
        }

        private void EnsureCanvas()
        {
            if (group != null) return;
            GameObject canvasObject = new GameObject("EndingTitleCanvas", typeof(RectTransform),
                typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32766;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            group = canvasObject.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
            backdrop.transform.SetParent(canvasObject.transform, false);
            Image image = backdrop.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            Stretch(image.rectTransform);

            GameObject textObject = new GameObject("EndingTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(canvasObject.transform, false);
            label = textObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) label.font = TMP_Settings.defaultFontAsset;
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 64f;
            label.color = Color.white;
            label.raycastTarget = false;
            Stretch(label.rectTransform);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDisable()
        {
            if (sequence != null) StopCoroutine(sequence);
            sequence = null;
            if (group != null) group.gameObject.SetActive(false);
        }
    }
}
