using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Flow
{
    public static class PauseMenuLayout
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            Image image = Rect(name, parent).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static TMP_Text Label(Transform parent, string text, float size)
        {
            var label = Rect("Label", parent).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = new Color(0.91f, 0.88f, 0.81f);
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.margin = new Vector4(24f, 0f, 24f, 0f);
            return label;
        }

        public static Button Button(Transform parent, string text, float y, UnityAction action)
        {
            Image image = Panel(text, parent, new Color(0.15f, 0.15f, 0.15f));
            Place(image.rectTransform, new Vector2(0f, y), new Vector2(560f, 64f));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(2.2f, 1.35f, 1.2f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(1.6f, 0.75f, 0.65f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            Label(button.transform, text, 24f);
            button.onClick.AddListener(action);
            return button;
        }

        public static Slider Volume(Transform parent, string name, float y)
        {
            RectTransform row = Rect(name, parent);
            Place(row, new Vector2(0f, y), new Vector2(560f, 65f));
            TMP_Text label = Label(row, name, 20f);
            label.rectTransform.anchorMax = new Vector2(0.4f, 1f);
            Image track = Panel("Track", row, new Color(0.21f, 0.21f, 0.21f));
            Place(track.rectTransform, new Vector2(100f, 0f), new Vector2(285f, 12f));
            Image fill = Panel("Fill", track.transform, new Color(0.65f, 0.25f, 0.2f));
            Image handle = Panel("Handle", track.transform, new Color(0.92f, 0.88f, 0.8f));
            handle.rectTransform.sizeDelta = new Vector2(18f, 26f);
            Slider slider = track.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }
    }
}
