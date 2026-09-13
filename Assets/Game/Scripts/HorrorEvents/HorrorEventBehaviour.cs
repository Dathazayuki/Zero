using UnityEngine;

namespace Zero.AnomalyPrototype.HorrorEvents
{
    public abstract class HorrorEventBehaviour : MonoBehaviour
    {
        [SerializeField] private string displayName = "Unnamed Event";
        [SerializeField, Min(0.01f)] private float selectionWeight = 1f;

        public string DisplayName => displayName;
        public float SelectionWeight => selectionWeight;
        public bool IsArmed { get; private set; }
        public bool HasPlayed { get; private set; }

        public void Initialize(string eventDisplayName, float weight = 1f)
        {
            displayName = eventDisplayName;
            selectionWeight = Mathf.Max(0.01f, weight);
        }

        public virtual void CaptureNormalState()
        {
        }

        public void Arm()
        {
            ResetEvent();
            IsArmed = true;
            OnArmed();
        }

        public void Play()
        {
            if (!IsArmed || HasPlayed)
            {
                return;
            }

            HasPlayed = true;
            OnPlay();
        }

        public void ResetEvent()
        {
            OnResetEvent();
            IsArmed = false;
            HasPlayed = false;
        }

        protected virtual void OnArmed()
        {
        }

        protected abstract void OnPlay();
        protected abstract void OnResetEvent();
    }
}
