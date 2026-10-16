using Zero.AnomalyPrototype.Flow;
using UnityEngine;
using UnityEngine.UI;
using Zero.AnomalyPrototype.Player;
using Zero.EnvironmentLoop;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class SeaOfBloodAnomaly : AnomalyBehaviour
    {
        private SourceSoundPlayback soundPlayback;
        public enum FloodState { Dormant, WaitingForEntry, Rising, Escaped, DeathRequested }

        [SerializeField] private Transform bloodSurface;
        [SerializeField] private BoxCollider bloodCollider;
        [SerializeField] private Transform bloodStartPoint;
        [SerializeField] private Transform bloodEndPoint;
        [SerializeField] private Renderer bloodRenderer;
        [SerializeField] private SeaOfBloodStartTrigger entryTrigger;
        [SerializeField] private ParticleSystem riseParticles;
        [SerializeField] private AudioSource riseAudio;
        [SerializeField] private AnomalyDeathPresentation deathPresentation;
        [SerializeField, Min(0.1f)] private float escapeDuration = 12f;
        [SerializeField, Min(0f)] private float dangerDuration = 3f;
        [SerializeField, Range(0f, 1f)] private float startVolume = 0.25f;
        [SerializeField, Range(0f, 1f)] private float dangerVolume = 1f;

        [Header("Underwater Camera Presentation")]
        [SerializeField] private Color underwaterColor = new Color(0.48f, 0.015f, 0.035f, 1f);
        [SerializeField, Range(0.1f, 1f)] private float underwaterMaxAlpha = 0.85f;

        [Header("3D Volume Mode (Cube)")]
        [SerializeField] private bool scaleVolumeHeight = true;

        private Transform player;
        private Camera playerCamera;
        private FirstPersonController movement;
        private PlayerDeathController death;
        private StairLoopManager manager;
        private FloorSegment owner;
        private MaterialPropertyBlock properties;
        private static readonly int FloodProgress = Shader.PropertyToID("_FloodProgress");
        private float elapsed;
        private bool ownsPlayerEffects;
        private bool dangerLogged;

        private Vector3 baseSurfaceLocalPosition;
        private Vector3 baseSurfaceLocalScale = Vector3.one;
        private bool hasCapturedBaseTransform;

        private CanvasGroup underwaterCanvasGroup;

        public FloodState State { get; private set; }

        public void BindPlayer(Transform playerTransform, Camera cam)
        {
            player = playerTransform;
            playerCamera = cam;
            movement = player != null ? player.GetComponentInParent<FirstPersonController>() : null;
            death = movement != null ? movement.GetComponent<PlayerDeathController>() : null;
            if (playerCamera == null && movement != null)
                playerCamera = movement.GetComponentInChildren<Camera>(true);
            if (playerCamera == null)
                playerCamera = Camera.main;
        }

        public void BindFloorContext(StairLoopManager loopManager, FloorSegment segment)
        {
            manager = loopManager;
            owner = segment;
        }

        public override void CaptureNormalState()
        {
            if (bloodSurface == null)
            {
                var t = transform.Find("BloodSurface");
                if (t != null) bloodSurface = t;
            }
            if (bloodStartPoint == null)
            {
                var t = transform.Find("BloodStartPoint");
                if (t != null) bloodStartPoint = t;
            }
            if (bloodEndPoint == null)
            {
                var t = transform.Find("BloodEndPoint");
                if (t != null) bloodEndPoint = t;
            }

            if (bloodSurface != null && !hasCapturedBaseTransform)
            {
                baseSurfaceLocalPosition = bloodSurface.localPosition;
                baseSurfaceLocalScale = bloodSurface.localScale;
                hasCapturedBaseTransform = true;
            }
            if (bloodRenderer == null && bloodSurface != null)
                bloodRenderer = bloodSurface.GetComponent<Renderer>();
            if (bloodCollider == null && bloodSurface != null)
                bloodCollider = bloodSurface.GetComponent<BoxCollider>();
            if (bloodCollider == null && bloodSurface != null)
                bloodCollider = bloodSurface.gameObject.AddComponent<BoxCollider>();
            if (bloodCollider != null)
                bloodCollider.isTrigger = true;

            if (entryTrigger == null) entryTrigger = GetComponentInChildren<SeaOfBloodStartTrigger>(true);
            if (deathPresentation == null) deathPresentation = GetComponentInChildren<AnomalyDeathPresentation>(true);
            if (deathPresentation != null && deathPresentation.MotionStyle != DeathMotionStyle.Suffocation && deathPresentation.DeathStyle != AnomalyDeathStyle.Style2_FreezeBlackout && deathPresentation.MotionStyle != DeathMotionStyle.FreezeBlackout)
            {
                deathPresentation.SetMotionStyle(DeathMotionStyle.Suffocation);
            }
            if (properties == null) properties = new MaterialPropertyBlock();
            if (entryTrigger != null) entryTrigger.Configure(this);
            EnsureUnderwaterOverlay();
        }

        private void EnsureUnderwaterOverlay()
        {
            if (underwaterCanvasGroup != null) return;

            var canvasObj = new GameObject("SeaOfBloodOverlay");
            canvasObj.transform.SetParent(transform, false);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            underwaterCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
            underwaterCanvasGroup.alpha = 0f;
            underwaterCanvasGroup.blocksRaycasts = false;

            var imgObj = new GameObject("Tint");
            imgObj.transform.SetParent(canvasObj.transform, false);
            var img = imgObj.AddComponent<Image>();
            img.color = underwaterColor;
            img.raycastTarget = false;

            var rect = img.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }

        protected override void OnActivate()
        {
            CaptureNormalState();

            if (player == null || movement == null || death == null)
            {
                var fp = FindFirstObjectByType<FirstPersonController>();
                if (fp != null)
                {
                    BindPlayer(fp.transform, fp.PlayerCamera);
                }
            }

            if (manager == null || owner == null)
            {
                var lm = FindFirstObjectByType<StairLoopManager>();
                var seg = GetComponentInParent<FloorSegment>();
                if (lm != null && seg != null)
                {
                    BindFloorContext(lm, seg);
                }
            }

            bool validDeathStyle = deathPresentation != null && (deathPresentation.MotionStyle == DeathMotionStyle.Suffocation || deathPresentation.DeathStyle == AnomalyDeathStyle.Style2_FreezeBlackout || deathPresentation.MotionStyle == DeathMotionStyle.FreezeBlackout);

            if (bloodSurface == null || bloodStartPoint == null || bloodEndPoint == null ||
                bloodRenderer == null || entryTrigger == null || player == null || movement == null ||
                death == null || manager == null || owner == null || deathPresentation == null ||
                !validDeathStyle ||
                bloodStartPoint.IsChildOf(bloodSurface) || bloodEndPoint.IsChildOf(bloodSurface))
            {
                Debug.LogError($"SEA_OF_BLOOD_CONFIGURATION_INVALID: surface={bloodSurface != null}, startPoint={bloodStartPoint != null}, endPoint={bloodEndPoint != null}, renderer={bloodRenderer != null}, trigger={entryTrigger != null}, player={player != null}, movement={movement != null}, death={death != null}, manager={manager != null}, owner={owner != null}, deathPresentation={deathPresentation != null}, motionSuffocation={validDeathStyle}, startChild={bloodStartPoint != null && bloodStartPoint.IsChildOf(bloodSurface)}, endChild={bloodEndPoint != null && bloodEndPoint.IsChildOf(bloodSurface)}", this);
                return;
            }
            State = FloodState.WaitingForEntry;
            entryTrigger.SetArmed(true);
            Debug.Log("SEA_OF_BLOOD_ARMED", this);
        }

        public bool TryStartRising(Collider other)
        {
            if (!IsActive || !isActiveAndEnabled || State != FloodState.WaitingForEntry ||
                manager == null || owner == null || manager.CurrentSegment != owner ||
                other == null || movement == null || other.GetComponentInParent<FirstPersonController>() != movement ||
                death == null || death.IsDeathInProgress)
                return false;

            State = FloodState.Rising;
            elapsed = 0f;
            entryTrigger.SetArmed(false);
            if (bloodSurface != null) bloodSurface.gameObject.SetActive(true);
            if (bloodRenderer != null) bloodRenderer.enabled = true;
            SetSurfaceProgress(0f);
            if (riseAudio != null)
            {
                riseAudio.volume = startVolume;
                GameAudioBootstrap.BindSource(ref soundPlayback, riseAudio).Play(null, riseAudio.clip, false);
            }
            if (riseParticles != null) riseParticles.Play(true);
            Debug.Log("SEA_OF_BLOOD_RISING_STARTED", this);
            return true;
        }

        private void LateUpdate()
        {
            if (!IsActive) return;

            // Trong lúc death sequence: giữ màu đỏ suốt lúc camera rung lắc và fade đen.
            // Chỉ khi màn hình ĐÃ ĐEN HOÀN TOÀN mới dọn màu đỏ về 0.
            if (death != null && death.IsDeathInProgress)
            {
                if (underwaterCanvasGroup != null)
                    underwaterCanvasGroup.alpha = death.IsScreenBlack ? 0f : underwaterMaxAlpha;
                return;
            }

            if (State == FloodState.DeathRequested)
            {
                if (underwaterCanvasGroup != null) underwaterCanvasGroup.alpha = underwaterMaxAlpha;
                return;
            }

            // Chuyển tầng khác -> dọn dẹp mặt máu
            if ((State == FloodState.Rising || State == FloodState.Escaped) && manager != null && owner != null && manager.CurrentSegment != owner)
            {
                Cleanup();
                State = FloodState.Escaped;
                Debug.Log("SEA_OF_BLOOD_ESCAPED", this);
                return;
            }

            // Đồng bộ trạng thái player trong / ngoài vùng máu
            bool inHazard = IsPlayerInHazardZone();
            if (inHazard && State == FloodState.Escaped)
            {
                OnPlayerReentered();
            }
            else if (!inHazard && State == FloodState.Rising && elapsed >= 0.2f)
            {
                SetPlayerEscaped();
            }

            if (State != FloodState.Rising && State != FloodState.Escaped) return;

            // Máu dâng liên tục đến trần (dù ở trong hay ngoài phòng)
            float duration = Mathf.Max(0.1f, escapeDuration);
            if (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetSurfaceProgress(Mathf.Clamp01(elapsed / duration));
            }
            else
            {
                SetSurfaceProgress(1f);
            }

            // Kiểm tra ngập nước
            float surfaceY = (bloodRenderer != null) ? bloodRenderer.bounds.max.y : bloodSurface.position.y;
            float camY = playerCamera != null ? playerCamera.transform.position.y : (player != null ? player.position.y + 1.6f : 0f);
            bool isUnderwater = inHazard && (camY <= surfaceY + 0.08f);

            // Cập nhật màu camera
            if (underwaterCanvasGroup != null)
            {
                underwaterCanvasGroup.alpha = isUnderwater
                    ? underwaterMaxAlpha
                    : Mathf.MoveTowards(underwaterCanvasGroup.alpha, 0f, Time.deltaTime * 25f);
            }

            // Xử lý nguy hiểm và chết ngạt khi ở trong vùng máu
            if (State == FloodState.Rising)
            {
                float danger = dangerDuration <= 0f ? 0f : Mathf.Clamp01((elapsed - duration + dangerDuration) / dangerDuration);
                if (danger > 0f || isUnderwater || elapsed >= duration)
                {
                    if (!dangerLogged)
                    {
                        dangerLogged = true;
                        Debug.Log("SEA_OF_BLOOD_DANGER", this);
                    }
                    ownsPlayerEffects = true;
                    if (movement != null)
                    {
                        movement.SetForcedBreathing(true);
                        movement.SetStumbleIntensity(Mathf.Max(danger * 0.35f, isUnderwater ? 0.35f : 0.2f));
                    }
                }
                if (riseAudio != null) riseAudio.volume = Mathf.Lerp(startVolume, dangerVolume, Mathf.Max(danger, isUnderwater ? 1f : 0f));

                if (elapsed >= duration)
                {
                    TriggerSuffocationDeath();
                }
            }
        }

        private void TriggerSuffocationDeath()
        {
            State = FloodState.DeathRequested;
            if (underwaterCanvasGroup != null) underwaterCanvasGroup.alpha = underwaterMaxAlpha;
            Debug.Log("SEA_OF_BLOOD_DEATH_REQUESTED", this);

            if (death == null || !death.TryBeginDeath(this, deathPresentation))
            {
                Cleanup();
                Debug.LogWarning("SEA_OF_BLOOD_CONFIGURATION_INVALID: death rejected; countdown stopped.", this);
            }
        }

        private void OnPlayerReentered()
        {
            if (State != FloodState.Escaped) return;
            State = FloodState.Rising;
            Debug.Log("SEA_OF_BLOOD_REENTERED", this);

            float duration = Mathf.Max(0.1f, escapeDuration);
            if (elapsed >= duration)
            {
                // Cho player 1.5s thở dốc ngạt thở trước khi chết nếu quay lại phòng đã ngập đầy
                elapsed = duration - 1.5f;
            }
        }

        private void SetPlayerEscaped()
        {
            if (State != FloodState.Rising) return;
            State = FloodState.Escaped;
            ClearPlayerEffects();
            if (underwaterCanvasGroup != null) underwaterCanvasGroup.alpha = 0f;
            Debug.Log("SEA_OF_BLOOD_ESCAPED", this);
        }

        private bool IsPlayerInHazardZone()
        {
            if (player == null || bloodSurface == null || !bloodSurface.gameObject.activeInHierarchy) return false;
            Vector3 localPos = bloodSurface.InverseTransformPoint(player.position);
            return Mathf.Abs(localPos.x) <= 0.52f && Mathf.Abs(localPos.z) <= 0.52f;
        }

        private void SetSurfaceProgress(float progress)
        {
            if (bloodSurface == null) return;

            if (!hasCapturedBaseTransform)
            {
                baseSurfaceLocalPosition = bloodSurface.localPosition;
                baseSurfaceLocalScale = bloodSurface.localScale;
                hasCapturedBaseTransform = true;
            }

            float startY = GetLocalYForMarker(bloodStartPoint, baseSurfaceLocalPosition.y - 0.15f);
            float endY = GetLocalYForMarker(bloodEndPoint, baseSurfaceLocalPosition.y + 2.5f);
            float currentTopY = Mathf.Lerp(startY, endY, Mathf.Pow(progress, 1.25f));

            if (scaleVolumeHeight)
            {
                float height = Mathf.Max(0.001f, currentTopY - startY);
                bloodSurface.localPosition = new Vector3(baseSurfaceLocalPosition.x, startY + height * 0.5f, baseSurfaceLocalPosition.z);
                bloodSurface.localScale = new Vector3(baseSurfaceLocalScale.x, height, baseSurfaceLocalScale.z);
            }
            else
            {
                bloodSurface.localPosition = new Vector3(baseSurfaceLocalPosition.x, currentTopY, baseSurfaceLocalPosition.z);
            }

            if (bloodRenderer != null)
            {
                bloodRenderer.GetPropertyBlock(properties);
                properties.SetFloat(FloodProgress, progress);
                bloodRenderer.SetPropertyBlock(properties);
            }
        }

        private float GetLocalYForMarker(Transform marker, float fallbackLocalY)
        {
            if (marker == null) return fallbackLocalY;
            if (bloodSurface.parent == null) return marker.position.y;
            if (marker.parent == bloodSurface.parent) return marker.localPosition.y;
            return bloodSurface.parent.InverseTransformPoint(marker.position).y;
        }

        public override void OnPlayerDeathAccepted()
        {
            State = FloodState.DeathRequested;
            ClearPlayerEffects();
            if (riseAudio != null) riseAudio.Stop();
            if (riseParticles != null) riseParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (underwaterCanvasGroup != null) underwaterCanvasGroup.alpha = underwaterMaxAlpha;
        }

        private void ClearPlayerEffects()
        {
            if (!ownsPlayerEffects) return;
            ownsPlayerEffects = false;
            if (movement == null) return;
            movement.SetStumbleIntensity(0f);
            movement.SetForcedBreathing(false);
        }

        private void Cleanup()
        {
            ClearPlayerEffects();
            if (underwaterCanvasGroup != null) underwaterCanvasGroup.alpha = 0f;
            if (entryTrigger != null) entryTrigger.SetArmed(false);
            if (riseAudio != null) riseAudio.Stop();
            if (riseParticles != null) riseParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (bloodRenderer != null) bloodRenderer.enabled = false;
            if (bloodSurface != null) bloodSurface.gameObject.SetActive(false);
            SetSurfaceProgress(0f);
            if (hasCapturedBaseTransform && bloodSurface != null)
                bloodSurface.localScale = baseSurfaceLocalScale;
            elapsed = 0f;
            dangerLogged = false;
        }

        protected override void OnResetAnomaly()
        {
            bool wasRunning = State != FloodState.Dormant;
            Cleanup();
            State = FloodState.Dormant;
            if (wasRunning) Debug.Log("SEA_OF_BLOOD_RESET", this);
        }

        private void Awake() { CaptureNormalState(); Cleanup(); }
        private void OnDisable() { ResetAnomaly(); }
    }
}
