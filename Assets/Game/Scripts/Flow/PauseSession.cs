using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    // Owns only the global values acquired by this pause, never cinematic input locks.
    public sealed class PauseSession
    {
        private float previousTimeScale;
        private bool previousAudioPause;
        private bool previousCursorVisible;
        private CursorLockMode previousCursorLock;
        public bool IsPaused { get; private set; }

        public bool Begin()
        {
            if (IsPaused) return false;
            previousTimeScale = Time.timeScale;
            previousAudioPause = AudioListener.pause;
            previousCursorVisible = Cursor.visible;
            previousCursorLock = Cursor.lockState;
            IsPaused = true;
            Time.timeScale = 0f;
            AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return true;
        }

        public void End()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }
    }
}
