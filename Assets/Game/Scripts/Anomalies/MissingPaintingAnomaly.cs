using UnityEngine;
using UnityEngine.Serialization;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class MissingPaintingAnomaly : AnomalyBehaviour
    {
        [SerializeField] private GameObject normalPainting;
        [FormerlySerializedAs("painting")]
        [SerializeField] private GameObject strangePainting;

        private bool normalPaintingWasActive;
        private bool strangePaintingWasActive;
        private bool normalStateCaptured;

        public GameObject NormalPainting => normalPainting;
        public GameObject StrangePainting => strangePainting;

        public void Configure(GameObject paintingObject)
        {
            strangePainting = paintingObject;
        }

        public void Configure(GameObject normalPaintingObject, GameObject strangePaintingObject)
        {
            normalPainting = normalPaintingObject;
            strangePainting = strangePaintingObject;
        }

        public override void CaptureNormalState()
        {
            normalPaintingWasActive = normalPainting != null && normalPainting.activeSelf;
            strangePaintingWasActive = strangePainting != null && strangePainting.activeSelf;
            normalStateCaptured = true;
        }

        protected override void OnActivate()
        {
            if (normalPainting == null || strangePainting == null || normalPainting == strangePainting)
            {
                Debug.LogWarning(
                    $"STRANGE_PAINTING_CONFIGURATION_INVALID: anomaly={name}, " +
                    $"normal={normalPainting != null}, strange={strangePainting != null}, " +
                    $"sameObject={normalPainting != null && normalPainting == strangePainting}",
                    this);
                return;
            }

            normalPainting.SetActive(false);
            strangePainting.SetActive(true);

            Debug.Log(
                $"STRANGE_PAINTING_ACTIVATED: anomaly={name}, " +
                $"normal={normalPainting.name}, strange={strangePainting.name}",
                this);
        }

        protected override void OnResetAnomaly()
        {
            if (normalPainting != null)
            {
                normalPainting.SetActive(normalStateCaptured ? normalPaintingWasActive : true);
            }

            if (strangePainting != null)
            {
                strangePainting.SetActive(normalStateCaptured ? strangePaintingWasActive : false);
            }
        }
    }
}
