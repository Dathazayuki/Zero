using UnityEngine;
using UnityEngine.Video;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class TVCartoonAnomaly : AnomalyBehaviour
    {
        [Header("TV Screen Settings")]
        [Tooltip("Renderer bề mặt màn hình TV.")]
        [SerializeField] private Renderer screenRenderer;

        [Tooltip("Chỉ số slot Material của màn hình trên Renderer (mặc định là 0).")]
        [SerializeField, Min(0)] private int screenMaterialIndex = 0;

        [Tooltip("Tên thuộc tính Texture trên Shader màn hình (_BaseMap cho URP, _MainTex cho Built-in).")]
        [SerializeField] private string targetMaterialProperty = "_BaseMap";

        [Tooltip("Material màn hình ở trạng thái bình thường (màn hình đen / tắt).")]
        [SerializeField] private Material normalMaterial;

        [Tooltip("Material màn hình khi chiếu video (nếu dùng Material có RenderTexture) - tùy chọn.")]
        [SerializeField] private Material videoMaterial;

        [Header("Video Settings")]
        [Tooltip("Component VideoPlayer điều khiển chiếu video (nếu để trống script sẽ tự tìm).")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Tooltip("Video clip cartoon cần chiếu (kéo file video đã import vào đây).")]
        [SerializeField] private VideoClip cartoonVideoClip;

        [Tooltip("Tự động lặp lại video liên tục.")]
        [SerializeField] private bool loopVideo = true;

        [Tooltip("Âm lượng video (0 = im lặng, 1 = tối đa).")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;

        [Tooltip("AudioSource phát âm thanh của video (tùy chọn).")]
        [SerializeField] private AudioSource audioSource;

        private Material cachedOriginalMaterial;
        private bool normalStateCaptured;

        public Renderer ScreenRenderer => screenRenderer;
        public VideoPlayer VideoPlayer => videoPlayer;
        public VideoClip CartoonVideoClip => cartoonVideoClip;

        public void Configure(Renderer renderer, VideoPlayer player, VideoClip clip, Material normalMat = null, Material videoMat = null)
        {
            screenRenderer = renderer;
            videoPlayer = player;
            cartoonVideoClip = clip;
            normalMaterial = normalMat;
            videoMaterial = videoMat;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            ResolveReferences();

            if (screenRenderer != null)
            {
                Material[] mats = screenRenderer.sharedMaterials;
                if (screenMaterialIndex >= 0 && screenMaterialIndex < mats.Length)
                {
                    cachedOriginalMaterial = mats[screenMaterialIndex];
                }
                else
                {
                    cachedOriginalMaterial = screenRenderer.sharedMaterial;
                }

                if (normalMaterial == null)
                {
                    normalMaterial = cachedOriginalMaterial;
                }
            }

            // Đảm bảo trạng thái bình thường: tắt video, hiển thị normal material
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            ApplyScreenMaterial(normalMaterial != null ? normalMaterial : cachedOriginalMaterial);
            normalStateCaptured = true;
        }

        protected override void OnActivate()
        {
            ResolveReferences();
            if (!normalStateCaptured)
            {
                CaptureNormalState();
            }

            if (videoPlayer == null && screenRenderer == null)
            {
                Debug.LogWarning($"TV_CARTOON_INVALID: Chưa gán VideoPlayer hoặc ScreenRenderer trên {name}", this);
                return;
            }

            // Gán material chiếu video nếu có cấu hình
            if (videoMaterial != null)
            {
                ApplyScreenMaterial(videoMaterial);
            }

            // Cấu hình và phát VideoPlayer
            if (videoPlayer != null)
            {
                if (cartoonVideoClip != null)
                {
                    videoPlayer.clip = cartoonVideoClip;
                }

                videoPlayer.isLooping = loopVideo;

                // Nếu chưa dùng RenderTexture mà có screenRenderer, chiếu thẳng lên Renderer
                if (videoPlayer.renderMode == VideoRenderMode.MaterialOverride && screenRenderer != null)
                {
                    videoPlayer.targetMaterialRenderer = screenRenderer;
                    if (!string.IsNullOrEmpty(targetMaterialProperty))
                    {
                        videoPlayer.targetMaterialProperty = targetMaterialProperty;
                    }
                }

                // Cấu hình âm thanh
                if (audioSource != null)
                {
                    videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
                    videoPlayer.SetTargetAudioSource(0, audioSource);
                    audioSource.volume = volume;
                }
                else
                {
                    videoPlayer.SetDirectAudioVolume(0, volume);
                }

                videoPlayer.Play();
            }

            Debug.Log($"TV_CARTOON_ACTIVATED: anomaly={name}", this);
        }

        protected override void OnResetAnomaly()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            Material targetNormal = normalMaterial != null ? normalMaterial : cachedOriginalMaterial;
            ApplyScreenMaterial(targetNormal);
        }

        private void OnDisable()
        {
            OnResetAnomaly();
        }

        private void ResolveReferences()
        {
            if (videoPlayer == null)
            {
                videoPlayer = GetComponentInChildren<VideoPlayer>(true);
            }

            if (screenRenderer == null)
            {
                screenRenderer = GetComponentInChildren<Renderer>(true);
            }

            if (audioSource == null)
            {
                audioSource = GetComponentInChildren<AudioSource>(true);
            }

            // Tự động thiết lập chế độ MaterialOverride nếu VideoPlayer chưa cấu hình
            if (videoPlayer != null && screenRenderer != null && videoPlayer.renderMode != VideoRenderMode.RenderTexture)
            {
                videoPlayer.renderMode = VideoRenderMode.MaterialOverride;
                videoPlayer.targetMaterialRenderer = screenRenderer;
                if (!string.IsNullOrEmpty(targetMaterialProperty))
                {
                    videoPlayer.targetMaterialProperty = targetMaterialProperty;
                }
            }
        }

        private void ApplyScreenMaterial(Material mat)
        {
            if (mat == null || screenRenderer == null)
            {
                return;
            }

            Material[] mats = screenRenderer.sharedMaterials;
            if (screenMaterialIndex >= 0 && screenMaterialIndex < mats.Length)
            {
                mats[screenMaterialIndex] = mat;
                screenRenderer.sharedMaterials = mats;
            }
            else
            {
                screenRenderer.sharedMaterial = mat;
            }
        }
    }
}
