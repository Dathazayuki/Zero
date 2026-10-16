using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class TitleMenuController : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject confirmationPanel;
        private GameFlowController flow;

        private void OnEnable()
        {
            if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
            if (continueButton != null) continueButton.onClick.AddListener(Continue);
            if (settingsButton != null) settingsButton.onClick.AddListener(ShowSettings);
            if (exitButton != null) exitButton.onClick.AddListener(ShowExitConfirmation);
            RefreshContinue();
        }

        private void Start()
        {
            flow = GameFlowController.Instance;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ShowMain();
        }

        private void Update()
        {
            if (flow == null || flow.Phase != GameFlowPhase.Title) return;
            bool cancel = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            cancel |= Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            if (cancel && ((settingsPanel != null && settingsPanel.activeSelf) ||
                           (confirmationPanel != null && confirmationPanel.activeSelf))) ShowMain();
        }

        private void OnDisable()
        {
            if (newGameButton != null) newGameButton.onClick.RemoveListener(NewGame);
            if (continueButton != null) continueButton.onClick.RemoveListener(Continue);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(ShowSettings);
            if (exitButton != null) exitButton.onClick.RemoveListener(ShowExitConfirmation);
        }

        public void ShowMain()
        {
            flow?.Settings?.Flush();
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (confirmationPanel != null) confirmationPanel.SetActive(false);
            if (mainPanel != null) mainPanel.SetActive(true);
            RefreshContinue();
            Select(newGameButton);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused && flow != null && flow.Phase == GameFlowPhase.Title) RefreshContinue();
        }

        private void RefreshContinue()
        {
            if (continueButton != null)
                continueButton.interactable = flow != null &&
                    flow.Checkpoints != null && flow.Checkpoints.TryGetCheckpoint(out _);
        }

        private void NewGame()
        {
            if (flow == null || !flow.StartNewGame())
                Debug.LogError("TITLE_NEW_GAME_REJECTED", this);
        }

        private void Continue()
        {
            if (flow == null || !flow.ContinueFromTitle())
                Debug.LogError("TITLE_CONTINUE_REJECTED", this);
        }

        private void ShowSettings()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
            settingsPanel?.GetComponent<TitleSettingsPanel>()?.SelectInitial();
        }

        private void ShowExitConfirmation()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (confirmationPanel != null) confirmationPanel.SetActive(true);
            confirmationPanel?.GetComponent<TitleExitConfirmPanel>()?.SelectInitial();
        }

        private static void Select(Button button)
        {
            if (button != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
}
