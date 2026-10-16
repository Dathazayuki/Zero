using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public abstract class AnomalyBehaviour : MonoBehaviour
    {
        [SerializeField] private AnomalyData data;

        public AnomalyData Data => data;
        public bool IsActive { get; private set; }
        public virtual bool KeepsPreparedArrowAsExpected => false;

        public void Initialize(AnomalyData anomalyData)
        {
            data = anomalyData;
        }

        public virtual void CaptureNormalState()
        {
        }

        public void Activate()
        {
            ResetAnomaly();
            IsActive = true;
            OnActivate();
        }

        public void ResetAnomaly()
        {
            OnResetAnomaly();
            IsActive = false;
        }

        public virtual void OnPlayerDeathAccepted()
        {
        }

        protected abstract void OnActivate();
        protected abstract void OnResetAnomaly();
    }
}
