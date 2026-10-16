using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class PauseMenuView : MonoBehaviour
    {
        private enum Page { Main, Settings, Confirmation }
        private Page currentPage;
        private GameObject mainPage;
        private PauseSettingsView settingsPage;
        private GameObject confirmationPage;
        private Button resumeButton;
        private Button cancelButton;
        public event Action ResumeRequested;
        public event Action TitleRequested;

        public static PauseMenuView Create(Transform parent, GameSettingsService settings)
        {
            RectTransform root = PauseMenuLayout.Rect("GameplayPauseCanvas", parent);
            root.gameObject.SetActive(false);
            Canvas canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25000;
            CanvasScaler scaler = root.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            root.gameObject.AddComponent<GraphicRaycaster>();
            var view = root.gameObject.AddComponent<PauseMenuView>();
            view.Build(settings);
            return view;
        }

        private void Build(GameSettingsService settings)
        {
            PauseMenuLayout.Panel("DimmedWorld", transform, new Color(0.015f, 0.018f, 0.02f, 0.88f));
            Image card = PauseMenuLayout.Panel("MenuCard", transform, new Color(0.055f, 0.06f, 0.065f, 0.98f));
            PauseMenuLayout.Place(card.rectTransform, Vector2.zero, new Vector2(680f, 960f));
            Image accent = PauseMenuLayout.Panel("Accent", card.transform, new Color(0.62f, 0.19f, 0.15f));
            PauseMenuLayout.Place(accent.rectTransform, new Vector2(-279f, 400f), new Vector2(3f, 54f));
            TMP_Text title = PauseMenuLayout.Label(card.transform, "PAUSED", 48f);
            PauseMenuLayout.Place(title.rectTransform, new Vector2(0f, 400f), new Vector2(550f, 70f));
            TMP_Text subtitle = PauseMenuLayout.Label(card.transform, "TAKE A BREATH. THE SHIFT CAN WAIT.", 16f);
            PauseMenuLayout.Place(subtitle.rectTransform, new Vector2(0f, 342f), new Vector2(550f, 35f));
            BuildMain(card.transform);
            settingsPage = PauseMenuLayout.Rect("Settings", card.transform).gameObject.AddComponent<PauseSettingsView>();
            settingsPage.Build(settings);
            PauseMenuLayout.Button(settingsPage.transform, "<   BACK", -360f, ShowMain);
            BuildConfirmation(card.transform);
            TMP_Text hint = PauseMenuLayout.Label(card.transform, "ESC / START   BACK     |     ENTER / A   SELECT", 15f);
            PauseMenuLayout.Place(hint.rectTransform, new Vector2(0f, -438f), new Vector2(580f, 30f));
        }

        private void BuildMain(Transform parent)
        {
            mainPage = PauseMenuLayout.Rect("Main", parent).gameObject;
            resumeButton = PauseMenuLayout.Button(mainPage.transform, "01     RESUME", 130f, () => ResumeRequested?.Invoke());
            PauseMenuLayout.Button(mainPage.transform, "02     SETTINGS", 40f, ShowSettings);
            PauseMenuLayout.Button(mainPage.transform, "03     RETURN TO TITLE", -50f, ShowConfirmation);
            TMP_Text note = PauseMenuLayout.Label(mainPage.transform,
                "Progress is saved at checkpoints.\nReturning to Title keeps your latest checkpoint.", 19f);
            PauseMenuLayout.Place(note.rectTransform, new Vector2(0f, -220f), new Vector2(560f, 100f));
        }

        private void BuildConfirmation(Transform parent)
        {
            confirmationPage = PauseMenuLayout.Rect("ConfirmReturn", parent).gameObject;
            TMP_Text note = PauseMenuLayout.Label(confirmationPage.transform,
                "RETURN TO TITLE?\n\nContinue will resume from your last checkpoint,\nnot your exact position here.", 23f);
            PauseMenuLayout.Place(note.rectTransform, new Vector2(0f, 140f), new Vector2(580f, 180f));
            cancelButton = PauseMenuLayout.Button(confirmationPage.transform, "STAY HERE", -40f, ShowMain);
            PauseMenuLayout.Button(confirmationPage.transform, "RETURN TO TITLE", -130f, () => TitleRequested?.Invoke());
        }

        public void Show() { gameObject.SetActive(true); ShowMain(); }
        public void Hide() => gameObject.SetActive(false);
        public void Back()
        {
            if (currentPage == Page.Main) ResumeRequested?.Invoke();
            else ShowMain();
        }

        private void ShowMain() => SelectPage(Page.Main, resumeButton);
        private void ShowConfirmation() => SelectPage(Page.Confirmation, cancelButton);
        private void ShowSettings()
        {
            settingsPage.Refresh();
            SelectPage(Page.Settings, settingsPage.FirstButton);
        }

        private void SelectPage(Page page, Button focus)
        {
            currentPage = page;
            mainPage.SetActive(page == Page.Main);
            settingsPage.gameObject.SetActive(page == Page.Settings);
            confirmationPage.SetActive(page == Page.Confirmation);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(focus.gameObject);
        }
    }
}
