using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.AnomalyPrototype.Player
{
    public static class PlayerDeathReferenceBinder
    {
        public static bool IsFreeze(AnomalyBehaviour source, AnomalyDeathPresentation presentation)
        {
            if (source != null && source.Data != null && source.Data.DeathStyle == AnomalyDeathStyle.Style2_FreezeBlackout)
                return true;
            return presentation != null && (presentation.DeathStyle == AnomalyDeathStyle.Style2_FreezeBlackout ||
                presentation.MotionStyle == DeathMotionStyle.FreezeBlackout);
        }

        public static AudioSource ResolveAudio(GameObject owner, AudioSource authored)
        {
            if (authored != null) return authored;
            AudioSource source = owner.GetComponent<AudioSource>();
            if (source != null) return source;
            source = owner.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        public static void BindBlackout(CanvasGroup group)
        {
            if (group == null) return;
            Canvas canvas = group.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.sortingOrder = 1000;
        }
    }
}
