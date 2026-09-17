using Zero.EnvironmentLoop;
using Zero.AnomalyPrototype.Player;
using Zero.AnomalyPrototype.Utilities;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class WrongReflectionAnomaly : AnomalyBehaviour
    {
        [SerializeField] private PlanarMirrorRenderer planarMirror;
        [SerializeField] private Transform reflectedCharacter;
        [SerializeField] private Animator reflectedAnimator;
        [SerializeField] private Transform player;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private string reflectionOnlyLayerName = "MirrorReflectionOnly";
        [SerializeField] private Vector3 playerSpacePositionOffset;
        [SerializeField] private Vector3 rotationOffset;
        [SerializeField] private bool copyPlayerYaw = true;
        [SerializeField] private string staticPoseStateName = "IdLE";

        [Header("Manifest behind player")]
        [SerializeField] private string worldLayerName = "Default";
        [SerializeField, Min(0.5f)] private float mirrorProximityDistance = 3f;
        [SerializeField, Min(0.1f)] private float mirrorLookDuration = 3f;
        [SerializeField, Range(0.05f, 0.75f)] private float mirrorFocusViewportRadius = 0.35f;
        [SerializeField, Min(0.5f)] private float spawnBehindDistance = 1.5f;
        [SerializeField] private Vector3 worldSpawnOffset;
        [SerializeField] private LayerMask gazeObstructionMask = ~0;
        [SerializeField] private LayerMask spawnCollisionMask = ~0;
        [SerializeField, Min(0f)] private float surfaceClearance = 0.08f;

        [Header("Death")]
        [SerializeField] private AnomalyDeathPresentation deathPresentation;
        [SerializeField] private PlayerDeathController deathController;
        [SerializeField, Min(0.01f)] private float movementBeforeDeath = 0.1f;

        private Transform[] reflectedHierarchy;
        private int[] normalLayers;
        private StairLoopManager loopManager;
        private FloorSegment ownerSegment;
        private bool configurationValid;
        private bool baselineCaptured;
        private string gazeFailure;
        private string lastGazeFailure;
        private Vector3 normalLocalPosition;
        private Quaternion normalLocalRotation;
        private int reflectionOnlyLayer = -1;
        private int worldLayer = -1;
        private float mirrorLookTimer;
        private bool manifestedBehindPlayer;
        private Vector3 playerPositionAtManifest;
        private int manifestationFrame = -1;
        private bool deathRequested;
        private LayerMask originalMirrorReflectionMask;
        private bool mirrorMaskCaptured;
        private bool mirrorMaskModified;

        public Transform ReflectedCharacter => reflectedCharacter;
        public bool IsReflectionVisible => IsActive && reflectedCharacter != null &&
                                           reflectedCharacter.gameObject.activeInHierarchy;
        public bool HasManifestedBehindPlayer => manifestedBehindPlayer;
        public float MirrorLookTime => mirrorLookTimer;

        public void BindFloorContext(StairLoopManager manager, FloorSegment segment)
        {
            loopManager = manager;
            ownerSegment = segment;
        }

        private bool IsCurrentFloor => loopManager != null && ownerSegment != null &&
                                       loopManager.CurrentSegment == ownerSegment;

        public void BindPlayer(Transform playerTransform, Camera camera)
        {
            player = playerTransform;
            playerCamera = camera;
            planarMirror?.BindSourceCamera(camera);
            deathController ??= FindAnyObjectByType<PlayerDeathController>();
        }

        public override void CaptureNormalState()
        {
            if (planarMirror == null)
            {
                planarMirror = GetComponentInChildren<PlanarMirrorRenderer>(true);
            }

            if (planarMirror != null && !mirrorMaskCaptured)
            {
                originalMirrorReflectionMask = planarMirror.ReflectionMask;
                mirrorMaskCaptured = true;
            }

            if (reflectedCharacter == null)
            {
                return;
            }

            if (baselineCaptured) return;
            baselineCaptured = true;
            normalLocalPosition = reflectedCharacter.localPosition;
            normalLocalRotation = reflectedCharacter.localRotation;
            reflectedHierarchy = reflectedCharacter.GetComponentsInChildren<Transform>(true);
            normalLayers = new int[reflectedHierarchy.Length];
            for (int i = 0; i < reflectedHierarchy.Length; i++)
            {
                normalLayers[i] = reflectedHierarchy[i].gameObject.layer;
            }
        }

        private void LateUpdate()
        {
            if (!IsActive || !configurationValid || !IsCurrentFloor)
            {
                HideCharacter();
                mirrorLookTimer = 0f;
                return;
            }

            if (!IsActive || reflectedCharacter == null || player == null)
            {
                return;
            }

            if (manifestedBehindPlayer)
            {
                UpdateManifestedDeath();
                return;
            }

            reflectedCharacter.position = player.TransformPoint(playerSpacePositionOffset);
            if (!reflectedCharacter.gameObject.activeSelf)
            {
                reflectedCharacter.gameObject.SetActive(true);
                ApplyStaticPose();
            }
            if (copyPlayerYaw)
            {
                Vector3 playerEuler = player.eulerAngles;
                reflectedCharacter.rotation = Quaternion.Euler(
                    rotationOffset.x,
                    playerEuler.y + rotationOffset.y,
                    rotationOffset.z);
            }
            else if (planarMirror != null && planarMirror.MirrorPlane != null)
            {
                Vector3 direction = planarMirror.MirrorPlane.position - reflectedCharacter.position;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    reflectedCharacter.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up) *
                                                  Quaternion.Euler(rotationOffset);
                }
            }


            UpdateMirrorGaze();
        }

        protected override void OnActivate()
        {
            configurationValid = false;
            ResolveReferences();
            reflectionOnlyLayer = LayerMask.NameToLayer(reflectionOnlyLayerName);
            worldLayer = LayerMask.NameToLayer(worldLayerName);
            if (planarMirror == null || !planarMirror.IsConfigured || reflectedCharacter == null ||
                player == null || playerCamera == null ||
                reflectionOnlyLayer < 0 || worldLayer < 0 || loopManager == null || ownerSegment == null)
            {
                Debug.LogWarning(
                    $"WRONG_REFLECTION_CONFIGURATION_INVALID: anomaly={name}, " +
                    $"mirror={planarMirror != null && planarMirror.IsConfigured}, " +
                    $"character={reflectedCharacter != null}, player={player != null}, camera={playerCamera != null}, " +
                    $"reflectionLayer={reflectionOnlyLayerName}:{reflectionOnlyLayer}, " +
                    $"worldLayer={worldLayerName}:{worldLayer}, floorBound={loopManager != null && ownerSegment != null}",
                    this);
                return;
            }

            mirrorLookTimer = 0f;
            manifestedBehindPlayer = false;
            manifestationFrame = -1;
            deathRequested = false;
            ApplyLayerRecursively(reflectionOnlyLayer);
            configurationValid = true;
            lastGazeFailure = null;
            reflectedCharacter.gameObject.SetActive(false);

            planarMirror.BindSourceCamera(playerCamera);
            ApplyPlayerReflectionMaskExclusion();
            int reflectionLayerBit = 1 << reflectionOnlyLayer;
            bool mainCameraExcludesCharacter = (playerCamera.cullingMask & reflectionLayerBit) == 0;
            bool mirrorCameraIncludesCharacter = planarMirror.MirrorCamera != null &&
                                                 (planarMirror.MirrorCamera.cullingMask & reflectionLayerBit) != 0;
            if (!mainCameraExcludesCharacter || !mirrorCameraIncludesCharacter)
            {
                Debug.LogWarning(
                    $"WRONG_REFLECTION_LAYER_MASK_INVALID: anomaly={name}, " +
                    $"mainExcludes={mainCameraExcludesCharacter}, mirrorIncludes={mirrorCameraIncludesCharacter}, " +
                    $"layer={reflectionOnlyLayerName}",
                    this);
            }

            Debug.Log(
                $"WRONG_REFLECTION_ACTIVATED: anomaly={name}, character={reflectedCharacter.name}",
                this);
        }

        protected override void OnResetAnomaly()
        {
            configurationValid = false;
            mirrorLookTimer = 0f;
            manifestedBehindPlayer = false;
            manifestationFrame = -1;
            deathRequested = false;
            RestoreMirrorReflectionMask();
            if (reflectedCharacter == null)
            {
                return;
            }

            RestoreLayers();
            reflectedCharacter.localPosition = normalLocalPosition;
            reflectedCharacter.localRotation = normalLocalRotation;
            HideCharacter();
        }

        private void ApplyStaticPose()
        {
            if (reflectedAnimator == null) return;
            reflectedAnimator.enabled = true;
            reflectedAnimator.applyRootMotion = false;
            if (!string.IsNullOrEmpty(staticPoseStateName))
            {
                reflectedAnimator.Play(staticPoseStateName, 0, 0f);
                reflectedAnimator.Update(0f);
            }
            reflectedAnimator.enabled = false;
        }

        private void HideCharacter()
        {
            if (reflectedCharacter != null && reflectedCharacter.gameObject.activeSelf)
            {
                reflectedCharacter.gameObject.SetActive(false);
                planarMirror?.ClearReflectionTexture();
            }
        }

        private void OnDisable()
        {
            RestoreMirrorReflectionMask();
            HideCharacter();
            mirrorLookTimer = 0f;
        }

        private void ApplyPlayerReflectionMaskExclusion()
        {
            if (planarMirror == null)
            {
                return;
            }

            if (!mirrorMaskCaptured)
            {
                originalMirrorReflectionMask = planarMirror.ReflectionMask;
                mirrorMaskCaptured = true;
            }

            LayerMask playerMask = ResolvePlayerLayerMask();
            if (playerMask.value != 0)
            {
                LayerMask anomalyReflectionMask = originalMirrorReflectionMask & ~playerMask;
                planarMirror.SetReflectionMask(anomalyReflectionMask);
                mirrorMaskModified = true;
                Debug.Log(
                    $"WRONG_REFLECTION_EXCLUDE_PLAYER: Excluded player layers from mirror mask (playerMask={playerMask.value:X}, original={originalMirrorReflectionMask.value:X}, current={anomalyReflectionMask.value:X})",
                    this);
            }
            else
            {
                Debug.LogWarning(
                    "WRONG_REFLECTION_EXCLUDE_PLAYER_SKIPPED: 'Player' layer not found in project Tags and Layers.",
                    this);
            }
        }

        private LayerMask ResolvePlayerLayerMask()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                return 1 << playerLayer;
            }

            return 0;
        }

        private void RestoreMirrorReflectionMask()
        {
            if (mirrorMaskModified && planarMirror != null && mirrorMaskCaptured)
            {
                planarMirror.SetReflectionMask(originalMirrorReflectionMask);
                mirrorMaskModified = false;
                Debug.Log("WRONG_REFLECTION_RESTORE_PLAYER: Restored original mirror reflection mask.", this);
            }
        }

        private void UpdateMirrorGaze()
        {
            bool isCloseAndLooking = IsMirrorFocusedAndVisible();
            if (!isCloseAndLooking)
            {
                if (mirrorLookTimer > 0f || lastGazeFailure != gazeFailure)
                {
                    Debug.Log(
                        $"WRONG_REFLECTION_GAZE_RESET: segment={ownerSegment.SegmentId}, elapsed={mirrorLookTimer:F2}, reason={gazeFailure}",
                        this);
                }

                mirrorLookTimer = 0f;
                lastGazeFailure = gazeFailure;
                return;
            }

            lastGazeFailure = null;
            if (mirrorLookTimer <= 0f)
            {
                Debug.Log($"WRONG_REFLECTION_GAZE_STARTED: anomaly={name}", this);
            }

            mirrorLookTimer += Time.deltaTime;
            if (mirrorLookTimer >= mirrorLookDuration)
            {
                ManifestBehindPlayer();
            }
        }

        private bool IsMirrorFocusedAndVisible()
        {
            Renderer surface = planarMirror?.SurfaceRenderer;
            if (surface == null || playerCamera == null)
            {
                gazeFailure = "missing-surface-or-camera";
                return false;
            }

            Vector3 targetPoint = surface.bounds.center;
            // Use the part of the glass under the crosshair, including close-up views.
            MeshFilter filter = surface.GetComponent<MeshFilter>();
            Transform surfaceTransform = surface.transform;
            if (filter != null && filter.sharedMesh != null)
            {
                Ray viewRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
                Ray localRay = new Ray(surfaceTransform.InverseTransformPoint(viewRay.origin),
                    surfaceTransform.InverseTransformVector(viewRay.direction));
                if (filter.sharedMesh.bounds.IntersectRay(localRay, out float entry))
                    targetPoint = surfaceTransform.TransformPoint(localRay.GetPoint(entry));
            }
            if (!surface.enabled || !surface.gameObject.activeInHierarchy ||
                (playerCamera.cullingMask & (1 << surface.gameObject.layer)) == 0)
            {
                gazeFailure = "surface-not-visible";
                return false;
            }
            if (Vector3.Distance(playerCamera.transform.position, targetPoint) > mirrorProximityDistance)
            {
                gazeFailure = "too-far";
                return false;
            }
            Vector3 viewport = playerCamera.WorldToViewportPoint(targetPoint);
            Vector2 offset = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
            if (viewport.z <= 0f ||
                offset.sqrMagnitude > mirrorFocusViewportRadius * mirrorFocusViewportRadius)
            {
                gazeFailure = "not-looking-at-glass";
                return false;
            }

            Vector3 origin = playerCamera.transform.position;
            Vector3 direction = targetPoint - origin;
            float distance = direction.magnitude;
            float rayDistance = Mathf.Max(0f, distance - surfaceClearance);
            if (rayDistance <= Mathf.Epsilon)
                return true;

            if (TryGetObstruction(origin, direction / distance, rayDistance, gazeObstructionMask,
                    surfaceTransform, out RaycastHit obstacle))
            {
                gazeFailure = $"blocked-by:{obstacle.transform.name}";
                return false;
            }
            return true;
        }

        private bool TryGetObstruction(Vector3 origin, Vector3 direction, float distance,
            LayerMask mask, Transform allowedSurface, out RaycastHit nearest)
        {
            nearest = default;
            float nearestDistance = float.PositiveInfinity;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, direction, distance, mask,
                         QueryTriggerInteraction.Ignore))
            {
                Transform target = hit.transform;
                if ((player != null && target.IsChildOf(player)) ||
                    (reflectedCharacter != null && target.IsChildOf(reflectedCharacter)) ||
                    target.gameObject.layer == reflectionOnlyLayer ||
                    (allowedSurface != null && target.IsChildOf(allowedSurface)))
                    continue;
                if (hit.distance < nearestDistance)
                {
                    nearest = hit;
                    nearestDistance = hit.distance;
                }
            }
            return nearestDistance < float.PositiveInfinity;
        }

        private void ManifestBehindPlayer()
        {
            manifestedBehindPlayer = true;
            mirrorLookTimer = mirrorLookDuration;

            Vector3 backward = -player.forward;
            backward.y = 0f;
            if (backward.sqrMagnitude <= 0.001f)
            {
                backward = -playerCamera.transform.forward;
                backward.y = 0f;
            }

            backward.Normalize();
            float resolvedDistance = spawnBehindDistance;
            Vector3 rayOrigin = player.position + Vector3.up * 0.5f;
            if (TryGetObstruction(
                    rayOrigin,
                    backward,
                    spawnBehindDistance,
                    spawnCollisionMask,
                    null, out RaycastHit hit))
            {
                resolvedDistance = Mathf.Max(0f, hit.distance - 0.3f);
            }

            Vector3 spawnPosition = player.position + backward * resolvedDistance + worldSpawnOffset;
            reflectedCharacter.position = spawnPosition;
            Vector3 facePlayer = player.position - spawnPosition;
            facePlayer.y = 0f;
            if (facePlayer.sqrMagnitude > 0.001f)
            {
                reflectedCharacter.rotation = Quaternion.LookRotation(facePlayer.normalized, Vector3.up) *
                                              Quaternion.Euler(rotationOffset);
            }

            ApplyLayerRecursively(worldLayer);
            playerPositionAtManifest = player.position;
            manifestationFrame = Time.frameCount;
            deathRequested = false;
            Debug.Log(
                $"WRONG_REFLECTION_MANIFESTED_BEHIND_PLAYER: anomaly={name}, " +
                $"distance={resolvedDistance:F2}",
                this);
        }

        private void ResolveReferences()
        {
            planarMirror ??= GetComponentInChildren<PlanarMirrorRenderer>(true);
            if (reflectedCharacter != null)
            {
                reflectedAnimator ??= reflectedCharacter.GetComponentInChildren<Animator>(true);
            }

            if (reflectedHierarchy == null || normalLayers == null)
            {
                CaptureNormalState();
            }

            deathPresentation ??= GetComponentInChildren<AnomalyDeathPresentation>(true);
            deathController ??= FindAnyObjectByType<PlayerDeathController>();
        }

        private void UpdateManifestedDeath()
        {
            if (deathRequested || Time.frameCount <= manifestationFrame || reflectedCharacter == null ||
                player == null || playerCamera == null)
            {
                return;
            }

            bool playerMoved = (player.position - playerPositionAtManifest).sqrMagnitude >=
                               movementBeforeDeath * movementBeforeDeath;
            Renderer renderer = reflectedCharacter.GetComponentInChildren<Renderer>(true);
            Vector3 targetPoint = renderer != null ? renderer.bounds.center : reflectedCharacter.position;
            bool characterVisible = CameraVisibilityUtility.IsVisibleFromCamera(
                playerCamera,
                reflectedCharacter,
                targetPoint,
                gazeObstructionMask);
            if (!playerMoved && !characterVisible)
            {
                return;
            }

            deathRequested = true;
            deathController ??= FindAnyObjectByType<PlayerDeathController>();
            if (deathController == null)
            {
                Debug.LogWarning("DEATH_CONFIGURATION_INVALID: Wrong Reflection cannot find PlayerDeathController.", this);
                return;
            }

            deathController.TryBeginDeath(this, deathPresentation);
        }

        private void ApplyLayerRecursively(int layer)
        {
            if (reflectedHierarchy == null)
            {
                reflectedHierarchy = reflectedCharacter.GetComponentsInChildren<Transform>(true);
            }

            for (int i = 0; i < reflectedHierarchy.Length; i++)
            {
                reflectedHierarchy[i].gameObject.layer = layer;
            }
        }

        private void RestoreLayers()
        {
            if (reflectedHierarchy == null || normalLayers == null)
            {
                return;
            }

            int count = Mathf.Min(reflectedHierarchy.Length, normalLayers.Length);
            for (int i = 0; i < count; i++)
            {
                if (reflectedHierarchy[i] != null)
                {
                    reflectedHierarchy[i].gameObject.layer = normalLayers[i];
                }
            }
        }
    }
}
