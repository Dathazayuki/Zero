using Zero.AnomalyPrototype.Core;
using Zero.AnomalyPrototype.Loop;
using Zero.AnomalyPrototype.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Zero.AnomalyPrototype.UI
{
    public sealed class RuntimeHUD : MonoBehaviour
    {
        private GameManager gameManager;
        private LoopManager loopManager;
        private ProgressManager progressManager;
        private PlayerInteraction playerInteraction;
        private bool showDebug;
        private string notification = string.Empty;
        private float notificationUntil;

        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle centerStyle;
        private GUIStyle escapedStyle;
        private Texture2D panelTexture;

        public void Initialize(
            GameManager game,
            LoopManager loops,
            ProgressManager progress,
            PlayerInteraction interaction)
        {
            gameManager = game;
            loopManager = loops;
            progressManager = progress;
            playerInteraction = interaction;

            if (loopManager != null)
            {
                loopManager.ChoiceResolved += HandleChoiceResolved;
                loopManager.LoopPrepared += HandleLoopPrepared;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                showDebug = !showDebug;
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                loopManager?.ForceNormalNextLoop();
                Notify("NEXT LOOP FORCED: NO ANOMALY — GO UP");
            }

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                loopManager?.ForceAnomalyNextLoop();
                Notify("NEXT LOOP FORCED: ANOMALY — GO DOWN");
            }
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawInteractionPrompt();
            DrawStatus();

            if (showDebug)
            {
                DrawDebugPanel();
            }

            if (!string.IsNullOrEmpty(notification) && Time.unscaledTime <= notificationUntil)
            {
                GUI.Label(new Rect(0f, Screen.height * 0.72f, Screen.width, 42f), notification, centerStyle);
            }

            if (gameManager != null && gameManager.IsEscaped)
            {
                GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, bodyStyle);
                GUI.Label(new Rect(0f, Screen.height * 0.38f, Screen.width, 120f), "YOU ESCAPED", escapedStyle);
                GUI.Label(new Rect(0f, Screen.height * 0.55f, Screen.width, 42f), "The corridor is behind you. For now.", centerStyle);
            }
        }

        private void DrawInteractionPrompt()
        {
            if (playerInteraction != null && !string.IsNullOrEmpty(playerInteraction.CurrentPrompt))
            {
                GUI.Label(
                    new Rect(0f, Screen.height * 0.56f, Screen.width, 34f),
                    $"[E] {playerInteraction.CurrentPrompt}",
                    centerStyle);
            }
        }

        private void DrawStatus()
        {
            int current = progressManager != null ? progressManager.CurrentProgress : 0;
            int required = progressManager != null ? progressManager.RequiredCorrectLoops : 5;
            GUI.Label(new Rect(20f, 16f, 560f, 32f), $"NORMAL: GO UP    ANOMALY: GO DOWN    {current} / {required}", titleStyle);
            GUI.Label(
                new Rect(20f, Screen.height - 38f, 780f, 28f),
                "WASD Move   Mouse Look   Esc Cursor   F1 Debug   F2 Normal   F3 Anomaly   F4 Observer",
                bodyStyle);
        }

        private void DrawDebugPanel()
        {
            LoopState state = loopManager?.CurrentState;
            string stateText = state == null
                ? "Waiting for loop..."
                : $"Loop: {state.LoopNumber}\n" +
                  $"Progress: {progressManager.CurrentProgress} / {progressManager.RequiredCorrectLoops}\n" +
                  $"Has Anomaly: {state.HasAnomaly}\n" +
                  $"Anomaly: {state.ActiveAnomalyName}\n" +
                  $"Horror Event: {state.HorrorEventName}\n" +
                  $"Difficulty: {state.Difficulty}\n" +
                  $"Last Result: {loopManager.LastResult}";

            Rect panel = new Rect(20f, 58f, 310f, 205f);
            GUI.Box(panel, GUIContent.none, bodyStyle);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 12f, panel.width - 28f, panel.height - 24f), stateText, bodyStyle);
        }

        private void HandleChoiceResolved(LoopChoice choice, bool correct)
        {
            string direction = choice == LoopChoice.Normal ? "UP" : "DOWN";
            Notify(correct ? $"{direction}: CORRECT" : $"{direction}: WRONG — PROGRESS RESET");
        }

        private void HandleLoopPrepared(LoopState state)
        {
            if (state != null && state.IsExitLoop)
            {
                Notify("THE EXIT IS OPEN");
            }
        }

        private void Notify(string message)
        {
            notification = message;
            notificationUntil = Time.unscaledTime + 2.2f;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null)
            {
                return;
            }

            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, new Color(0.015f, 0.018f, 0.022f, 0.86f));
            panelTexture.Apply();

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.9f, 0.93f, 0.86f) }
            };
            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                padding = new RectOffset(10, 10, 8, 8),
                normal =
                {
                    textColor = new Color(0.87f, 0.89f, 0.84f),
                    background = panelTexture
                }
            };
            centerStyle = new GUIStyle(titleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17
            };
            escapedStyle = new GUIStyle(centerStyle)
            {
                fontSize = 46,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.72f, 0.95f, 0.74f) }
            };
        }

        private void OnDestroy()
        {
            if (loopManager != null)
            {
                loopManager.ChoiceResolved -= HandleChoiceResolved;
                loopManager.LoopPrepared -= HandleLoopPrepared;
            }

            if (panelTexture != null)
            {
                Destroy(panelTexture);
            }
        }
    }
}
