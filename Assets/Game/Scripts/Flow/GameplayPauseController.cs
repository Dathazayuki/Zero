using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [DefaultExecutionOrder(-1500)]
    public sealed class GameplayPauseController : MonoBehaviour
    {
        private readonly PauseSession session = new PauseSession();
        private GameFlowController flow;
        private FirstPersonController player;
        private PauseMenuView view;
        private EventSystem menuEvents;
        private EventSystem previousEvents;
        private GameObject previousSelection;
        private Coroutine binding;

        public bool IsPaused => session.IsPaused;

        public void Initialize(GameFlowController owner)
        {
            flow = owner;
            view = PauseMenuView.Create(transform, owner.Settings);
            view.ResumeRequested += Resume;
            view.TitleRequested += ReturnToTitle;
            var events = new GameObject("PauseEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
            menuEvents = events.GetComponent<EventSystem>();
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            events.SetActive(false);
            SceneManager.sceneLoaded += OnSceneLoaded;
            flow.PhaseChanged += OnPhaseChanged;
            binding = StartCoroutine(BindPlayer());
        }

        private void Update()
        {
            if (flow == null || view == null) return;
            bool pressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            pressed |= Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;
            pressed |= IsPaused && Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            if (!pressed) return;
            if (IsPaused) view.Back();
            else TryPause();
        }

        public bool TryPause()
        {
            if (flow == null || flow.IsTransitioning || player == null || !player.InputEnabled ||
                flow.Phase == GameFlowPhase.Title || flow.Phase == GameFlowPhase.Ending ||
                flow.Phase == GameFlowPhase.Finished || !session.Begin()) return false;
            previousEvents = EventSystem.current;
            if (previousEvents != null)
            {
                previousSelection = previousEvents.currentSelectedGameObject;
                previousEvents.enabled = false;
            }
            menuEvents.gameObject.SetActive(true);
            view.Show();
            Debug.Log("GAME_PAUSED", this);
            return true;
        }

        public void Resume()
        {
            if (!IsPaused) return;
            view.Hide();
            menuEvents.gameObject.SetActive(false);
            if (previousEvents != null)
            {
                previousEvents.enabled = true;
                previousEvents.SetSelectedGameObject(previousSelection);
            }
            previousEvents = null;
            session.End();
            flow.Settings.Flush();
            Debug.Log("GAME_RESUMED", this);
        }

        private void ReturnToTitle()
        {
            if (!flow.ReturnToTitleFromPause()) return;
            Resume();
            if (player != null) player.SetCinematicLock(true);
        }

        private void OnPhaseChanged(GameFlowPhase phase) => Resume();

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Resume();
            if (binding != null) StopCoroutine(binding);
            binding = StartCoroutine(BindPlayer());
        }

        private IEnumerator BindPlayer()
        {
            yield return null;
            // Scene composition only; never searched in a frame loop.
            player = FindFirstObjectByType<FirstPersonController>();
            binding = null;
        }

        private void OnDisable()
        {
            Resume();
            if (binding != null) StopCoroutine(binding);
            binding = null;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (flow != null) flow.PhaseChanged -= OnPhaseChanged;
            if (view == null) return;
            view.ResumeRequested -= Resume;
            view.TitleRequested -= ReturnToTitle;
        }
    }
}
