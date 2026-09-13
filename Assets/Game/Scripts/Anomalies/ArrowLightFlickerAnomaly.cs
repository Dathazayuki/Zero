using System.Collections;
using Zero.AnomalyPrototype.Utilities;
using Zero.EnvironmentLoop;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArrowLightFlickerAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Light[] controlledLights;
        [SerializeField] private ArrowLightFlickerTrigger roomEntryTrigger;
        [SerializeField] private Transform arrowTarget;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private LayerMask visibilityMask = ~0;
        [Header("Flicker visual")]
        [SerializeField] private GameObject flickerVisibleObject;
        [SerializeField, Min(0.02f)] private float lightsOnDuration = 0.12f;
        [SerializeField, Min(0.02f)] private float lightsOffDuration = 0.09f;
        [SerializeField, Range(0.02f, 0.5f)] private float focusViewportRadius = 0.18f;
        [SerializeField, Min(0f)] private float requiredLookDuration = 0.2f;

        private bool[] normalEnabledStates;
        private Coroutine flickerRoutine;
        private bool playerEnteredRoom;
        private bool resolvedByArrow;
        private float arrowLookTimer;
        private bool normalFlickerObjectActiveState;
        private bool flickerObjectStateCaptured;

        public bool IsFlickering => flickerRoutine != null;
        public bool WasResolvedByArrow => resolvedByArrow;
        public GameObject FlickerVisibleObject => flickerVisibleObject;

        public void Configure(
            Light[] lights,
            ArrowLightFlickerTrigger entryTrigger,
            Transform arrow,
            Camera camera)
        {
            controlledLights = lights;
            roomEntryTrigger = entryTrigger;
            arrowTarget = arrow;
            playerCamera = camera;
            roomEntryTrigger?.Configure(this);
            CaptureNormalState();
        }

        public void Configure(
            Light[] lights,
            ArrowLightFlickerTrigger entryTrigger,
            Transform arrow,
            Camera camera,
            GameObject visibleWhenLightsOff)
        {
            controlledLights = lights;
            roomEntryTrigger = entryTrigger;
            arrowTarget = arrow;
            playerCamera = camera;
            flickerVisibleObject = visibleWhenLightsOff;
            roomEntryTrigger?.Configure(this);
            CaptureNormalState();
        }

        public void BindRoomContext(Camera camera, CorrectArrowController arrowController)
        {
            playerCamera = camera;
            if (arrowController != null)
            {
                arrowTarget = arrowController.ArrowTransform;
            }

            if (roomEntryTrigger == null)
            {
                roomEntryTrigger = GetComponentInChildren<ArrowLightFlickerTrigger>(true);
            }

            roomEntryTrigger?.Configure(this);
        }

        public override void CaptureNormalState()
        {
            normalFlickerObjectActiveState = flickerVisibleObject != null && flickerVisibleObject.activeSelf;
            flickerObjectStateCaptured = true;

            if (controlledLights == null)
            {
                normalEnabledStates = null;
                return;
            }

            normalEnabledStates = new bool[controlledLights.Length];
            for (int i = 0; i < controlledLights.Length; i++)
            {
                normalEnabledStates[i] = controlledLights[i] != null && controlledLights[i].enabled;
            }
        }

        private void Update()
        {
            if (!IsActive || !playerEnteredRoom || resolvedByArrow || playerCamera == null || arrowTarget == null)
            {
                return;
            }

            Vector3 observationPoint = ResolveArrowObservationPoint();
            Vector3 viewportPoint = playerCamera.WorldToViewportPoint(observationPoint);
            Vector2 viewportOffset = new Vector2(viewportPoint.x - 0.5f, viewportPoint.y - 0.5f);
            bool isFocused = viewportPoint.z > 0f &&
                             viewportOffset.sqrMagnitude <= focusViewportRadius * focusViewportRadius &&
                             CameraVisibilityUtility.IsVisibleFromCamera(
                    playerCamera,
                    arrowTarget,
                    observationPoint,
                    visibilityMask);
            if (!isFocused)
            {
                arrowLookTimer = 0f;
                return;
            }

            arrowLookTimer += Time.deltaTime;
            if (arrowLookTimer < requiredLookDuration)
            {
                return;
            }

            resolvedByArrow = true;
            StopFlickerAndTurnOff();
            roomEntryTrigger?.SetArmed(false);
            Debug.Log($"ARROW_LIGHT_FLICKER_RESOLVED_LIGHTS_OFF: anomaly={name}", this);
        }

        public bool TryBeginFlicker()
        {
            if (!IsActive || playerEnteredRoom || resolvedByArrow || controlledLights == null ||
                controlledLights.Length == 0)
            {
                Debug.LogWarning(
                    $"ARROW_LIGHT_FLICKER_REJECTED: anomaly={name}, active={IsActive}, " +
                    $"entered={playerEnteredRoom}, resolved={resolvedByArrow}, " +
                    $"lights={controlledLights?.Length ?? 0}",
                    this);
                return false;
            }

            playerEnteredRoom = true;
            flickerRoutine = StartCoroutine(FlickerUntilArrowObserved());
            Debug.Log($"ARROW_LIGHT_FLICKER_STARTED: anomaly={name}, lights={controlledLights.Length}", this);
            return true;
        }

        protected override void OnActivate()
        {
            StopFlickerAndRestore();
            playerEnteredRoom = false;
            resolvedByArrow = false;
            arrowLookTimer = 0f;
            roomEntryTrigger?.SetArmed(true);
            if (controlledLights == null || controlledLights.Length == 0 || roomEntryTrigger == null ||
                arrowTarget == null || playerCamera == null || flickerVisibleObject == null)
            {
                Debug.LogWarning(
                    $"ARROW_LIGHT_FLICKER_CONFIGURATION_INVALID: anomaly={name}, " +
                    $"lights={controlledLights?.Length ?? 0}, trigger={roomEntryTrigger != null}, " +
                    $"arrow={arrowTarget != null}, camera={playerCamera != null}, " +
                    $"flickerObject={flickerVisibleObject != null}",
                    this);
            }

            Debug.Log($"ARROW_LIGHT_FLICKER_ARMED: anomaly={name}", this);
        }

        protected override void OnResetAnomaly()
        {
            playerEnteredRoom = false;
            resolvedByArrow = false;
            arrowLookTimer = 0f;
            roomEntryTrigger?.SetArmed(false);
            StopFlickerAndRestore();
        }

        private IEnumerator FlickerUntilArrowObserved()
        {
            bool lightsOn = false;
            while (IsActive && playerEnteredRoom && !resolvedByArrow)
            {
                SetLightsEnabled(lightsOn);
                yield return new WaitForSeconds(lightsOn ? lightsOnDuration : lightsOffDuration);
                lightsOn = !lightsOn;
            }

            flickerRoutine = null;
            RestoreLights();
            RestoreFlickerObject();
        }

        private void StopFlickerAndRestore()
        {
            if (flickerRoutine != null)
            {
                StopCoroutine(flickerRoutine);
                flickerRoutine = null;
            }

            RestoreLights();
            RestoreFlickerObject();
        }

        private void StopFlickerAndTurnOff()
        {
            if (flickerRoutine != null)
            {
                StopCoroutine(flickerRoutine);
                flickerRoutine = null;
            }

            SetLightsEnabled(false);
            Debug.Log(
                $"ARROW_LIGHT_FLICKER_FINAL_STATE: anomaly={name}, lightsOn=false, " +
                $"objectVisible={flickerVisibleObject != null && flickerVisibleObject.activeSelf}",
                this);
        }

        private void RestoreLights()
        {
            if (controlledLights == null || normalEnabledStates == null)
            {
                return;
            }

            int count = Mathf.Min(controlledLights.Length, normalEnabledStates.Length);
            for (int i = 0; i < count; i++)
            {
                if (controlledLights[i] != null)
                {
                    controlledLights[i].enabled = normalEnabledStates[i];
                }
            }
        }

        private void SetLightsEnabled(bool value)
        {
            if (flickerVisibleObject != null)
            {
                flickerVisibleObject.SetActive(!value);
            }

            if (controlledLights == null)
            {
                return;
            }

            for (int i = 0; i < controlledLights.Length; i++)
            {
                if (controlledLights[i] != null)
                {
                    controlledLights[i].enabled = value;
                }
            }
        }

        private void RestoreFlickerObject()
        {
            if (flickerVisibleObject != null)
            {
                flickerVisibleObject.SetActive(
                    flickerObjectStateCaptured && normalFlickerObjectActiveState);
            }
        }

        private Vector3 ResolveArrowObservationPoint()
        {
            Renderer arrowRenderer = arrowTarget.GetComponentInChildren<Renderer>();
            return arrowRenderer != null ? arrowRenderer.bounds.center : arrowTarget.position;
        }
    }
}
