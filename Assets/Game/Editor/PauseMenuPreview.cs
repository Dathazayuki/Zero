using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Editor
{
    public static class PauseMenuPreview
    {
        [MenuItem("Tools/Zero/Pause Menu/Preview In Play Mode")]
        private static void Preview()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("PAUSE_PREVIEW_REQUIRES_PLAY_MODE");
                return;
            }
            GameFlowController flow = GameFlowController.Instance;
            if (flow == null) return;
            GameplayPauseController pause = flow.GetComponent<GameplayPauseController>();
            if (pause != null && !pause.TryPause())
                Debug.LogWarning("PAUSE_PREVIEW_REJECTED: wait until Player has control", flow);
        }
    }
}
