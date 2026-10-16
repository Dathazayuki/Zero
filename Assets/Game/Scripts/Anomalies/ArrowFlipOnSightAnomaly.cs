using System.Collections;
using Zero.EnvironmentLoop;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArrowFlipOnSightAnomaly : AnomalyBehaviour
    {
        [SerializeField] private CorrectArrowController arrowController;
        [SerializeField] private RoomAnomalyController roomController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LayerMask obstructionMask = ~0;
        [SerializeField, Min(0.02f)] private float flipDuration = 0.18f;
        [SerializeField, Min(0f)] private float requiredVisibleDuration = 0.08f;
        [SerializeField, Min(0f)] private float targetClearance = 0.08f;

        private Coroutine flipRoutine;
        private float visibleTimer;
        private bool flipStarted;
        private bool flipCompleted;
        private TravelDirection initialDirection;
        private TravelDirection flippedDirection;
        private Renderer arrowRenderer;

        public bool HasFlipStarted => flipStarted;
        public bool HasFlipCompleted => flipCompleted;
        public TravelDirection InitialDirection => initialDirection;
        public TravelDirection FlippedDirection => flippedDirection;
        public override bool KeepsPreparedArrowAsExpected => true;

        public void BindRoomContext(Camera camera, RoomAnomalyController room)
        {
            playerCamera = camera;
            roomController = room;
            arrowController = room != null ? room.CorrectArrow : null;
            CacheArrowRenderer();
        }

        private void Update()
        {
            if (!IsActive || flipStarted || playerCamera == null || roomController == null || arrowController == null ||
                arrowController.ArrowTransform == null)
            {
                return;
            }

            if (!IsArrowVisible())
            {
                visibleTimer = 0f;
                return;
            }

            visibleTimer += Time.deltaTime;
            if (visibleTimer < requiredVisibleDuration)
            {
                return;
            }

            StartFlip();
        }

        protected override void OnActivate()
        {
            CacheArrowRenderer();
            StopFlipRoutine();
            visibleTimer = 0f;
            flipStarted = false;
            flipCompleted = false;

            if (roomController == null || arrowController == null ||
                arrowController.ArrowTransform == null || playerCamera == null)
            {
                Debug.LogWarning(
                    $"ARROW_FLIP_CONFIGURATION_INVALID: anomaly={name}, " +
                    $"room={roomController != null}, " +
                    $"controller={arrowController != null}, " +
                    $"arrow={arrowController != null && arrowController.ArrowTransform != null}, " +
                    $"camera={playerCamera != null}",
                    this);
                return;
            }

            initialDirection = roomController.ArrowDirection;
            flippedDirection = Reverse(initialDirection);
            Debug.Log(
                $"ARROW_FLIP_ARMED: anomaly={name}, initial={initialDirection}, target={flippedDirection}",
                this);
        }

        protected override void OnResetAnomaly()
        {
            StopFlipRoutine();
            visibleTimer = 0f;
            flipStarted = false;
            flipCompleted = false;
        }

        private void StartFlip()
        {
            flipStarted = true;
            flippedDirection = Reverse(initialDirection);

            // Update gameplay immediately. Because this room has an anomaly,
            // ExpectedDirection becomes the reverse of this flipped direction,
            // which is the direction the arrow had before it moved.
            roomController.ApplyAnomalyArrowDirection(flippedDirection);
            flipRoutine = StartCoroutine(FlipRoutine());
            Debug.Log(
                $"ARROW_FLIP_STARTED: anomaly={name}, from={initialDirection}, to={flippedDirection}",
                this);
        }

        private IEnumerator FlipRoutine()
        {
            Transform arrow = arrowController.ArrowTransform;
            Quaternion startRotation = arrow.localRotation;
            Quaternion targetRotation = arrowController.GetLocalRotation(flippedDirection);
            float elapsed = 0f;
            float duration = Mathf.Max(0.02f, flipDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                arrow.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            arrow.localRotation = targetRotation;
            flipRoutine = null;
            flipCompleted = true;
            Debug.Log(
                $"ARROW_FLIP_COMPLETED: anomaly={name}, displayed={flippedDirection}, " +
                $"expected={initialDirection}",
                this);
        }

        private bool IsArrowVisible()
        {
            return AnomalyGazeVisibility.IsVisible(playerCamera, arrowController.ArrowTransform,
                arrowRenderer, obstructionMask, targetClearance);
        }

        private void StopFlipRoutine()
        {
            if (flipRoutine == null)
            {
                return;
            }

            StopCoroutine(flipRoutine);
            flipRoutine = null;
        }

        private void CacheArrowRenderer()
        {
            arrowRenderer = arrowController != null && arrowController.ArrowTransform != null
                ? arrowController.ArrowTransform.GetComponentInChildren<Renderer>()
                : null;
        }

        private static TravelDirection Reverse(TravelDirection direction)
        {
            return direction == TravelDirection.Up ? TravelDirection.Down : TravelDirection.Up;
        }
    }
}
