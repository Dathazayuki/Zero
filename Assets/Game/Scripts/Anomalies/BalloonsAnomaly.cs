using Zero.AnomalyPrototype.Utilities;
using Zero.EnvironmentLoop;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class BalloonsAnomaly : AnomalyBehaviour
    {
        [Header("Balloons")]
        [SerializeField] private Transform balloonsRoot;
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;

        [Header("Arrow activation")]
        [SerializeField] private CorrectArrowController arrowController;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LayerMask obstructionMask = ~0;
        [SerializeField, Range(0.02f, 0.5f)] private float focusViewportRadius = 0.18f;
        [SerializeField, Min(0f)] private float requiredLookDuration = 0.15f;

        [Header("Floating movement")]
        [SerializeField, Min(0.1f)] private float travelDuration = 8f;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.12f;
        [SerializeField, Min(0f)] private float bobFrequency = 0.45f;
        [SerializeField] private float rotationSpeed = 4f;

        private Vector3 normalLocalPosition;
        private Quaternion normalLocalRotation;
        private bool normalActiveSelf;
        private float arrowLookTimer;
        private float movementElapsed;
        private bool appeared;

        public bool HasAppeared => appeared;
        public float ArrowLookTime => arrowLookTimer;

        public void BindRoomContext(Camera camera, CorrectArrowController controller)
        {
            playerCamera = camera;
            arrowController = controller;
        }

        public override void CaptureNormalState()
        {
            if (balloonsRoot == null)
            {
                return;
            }

            normalLocalPosition = balloonsRoot.localPosition;
            normalLocalRotation = balloonsRoot.localRotation;
            normalActiveSelf = balloonsRoot.gameObject.activeSelf;
        }

        private void Update()
        {
            if (!IsActive)
            {
                return;
            }

            if (!appeared)
            {
                UpdateArrowObservation();
                return;
            }

            UpdateFloatingMovement();
        }

        protected override void OnActivate()
        {
            arrowLookTimer = 0f;
            movementElapsed = 0f;
            appeared = false;

            if (balloonsRoot != null)
            {
                balloonsRoot.gameObject.SetActive(false);
            }

            if (balloonsRoot == null || pointA == null || pointB == null ||
                playerCamera == null || arrowController == null || arrowController.ArrowTransform == null)
            {
                Debug.LogWarning(
                    $"BALLOONS_CONFIGURATION_INVALID: anomaly={name}, balloons={balloonsRoot != null}, " +
                    $"pointA={pointA != null}, pointB={pointB != null}, camera={playerCamera != null}, " +
                    $"arrow={arrowController != null && arrowController.ArrowTransform != null}",
                    this);
                return;
            }

            Debug.Log($"BALLOONS_ARMED: anomaly={name}", this);
        }

        protected override void OnResetAnomaly()
        {
            arrowLookTimer = 0f;
            movementElapsed = 0f;
            appeared = false;

            if (balloonsRoot == null)
            {
                return;
            }

            balloonsRoot.localPosition = normalLocalPosition;
            balloonsRoot.localRotation = normalLocalRotation;
            balloonsRoot.gameObject.SetActive(normalActiveSelf);
        }

        private void UpdateArrowObservation()
        {
            if (!IsArrowFocusedAndVisible())
            {
                if (arrowLookTimer > 0f)
                {
                    Debug.Log(
                        $"BALLOONS_GAZE_RESET: anomaly={name}, elapsed={arrowLookTimer:F2}",
                        this);
                }

                arrowLookTimer = 0f;
                return;
            }

            if (arrowLookTimer <= 0f)
            {
                Debug.Log($"BALLOONS_GAZE_STARTED: anomaly={name}", this);
            }

            arrowLookTimer += Time.deltaTime;
            if (arrowLookTimer >= requiredLookDuration)
            {
                ShowBalloons();
            }
        }

        private bool IsArrowFocusedAndVisible()
        {
            if (playerCamera == null || arrowController == null || arrowController.ArrowTransform == null)
            {
                return false;
            }

            Transform arrow = arrowController.ArrowTransform;
            Renderer arrowRenderer = arrow.GetComponentInChildren<Renderer>();
            Vector3 targetPoint = arrowRenderer != null ? arrowRenderer.bounds.center : arrow.position;
            Vector3 viewportPoint = playerCamera.WorldToViewportPoint(targetPoint);
            Vector2 viewportOffset = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
            return viewportPoint.z > 0f &&
                   viewportOffset.sqrMagnitude <= focusViewportRadius * focusViewportRadius &&
                   CameraVisibilityUtility.IsVisibleFromCamera(
                       playerCamera,
                       arrow,
                       targetPoint,
                       obstructionMask);
        }

        private void ShowBalloons()
        {
            appeared = true;
            arrowLookTimer = requiredLookDuration;
            movementElapsed = 0f;
            balloonsRoot.SetPositionAndRotation(pointA.position, pointA.rotation);
            balloonsRoot.gameObject.SetActive(true);
            Debug.Log(
                $"BALLOONS_APPEARED: anomaly={name}, pointA={pointA.position}, pointB={pointB.position}",
                this);
        }

        private void UpdateFloatingMovement()
        {
            if (balloonsRoot == null || pointA == null || pointB == null)
            {
                return;
            }

            movementElapsed += Time.deltaTime;
            float duration = Mathf.Max(0.1f, travelDuration);
            float pathT = Mathf.PingPong(movementElapsed / duration, 1f);
            float easedT = pathT * pathT * (3f - 2f * pathT);
            Vector3 pathPosition = Vector3.Lerp(pointA.position, pointB.position, easedT);
            float bobOffset = Mathf.Sin(movementElapsed * Mathf.PI * 2f * bobFrequency) * bobAmplitude;
            balloonsRoot.position = pathPosition + Vector3.up * bobOffset;

            if (!Mathf.Approximately(rotationSpeed, 0f))
            {
                balloonsRoot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }
}
