using Zero.AnomalyPrototype.Flow;
using UnityEngine;

namespace Zero.Utilities
{
    /// <summary>
    /// Điều khiển đèn nhấp nháy ngẫu nhiên liên tục (Random Glitch),
    /// mô phỏng ánh sáng chập chờn của đèn huỳnh quang hỏng hoặc nguồn điện không ổn định.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Zero/Lighting/Flickering Light Controller")]
    public sealed class FlickeringLightController : MonoBehaviour
    {
        private SourceSoundPlayback buzzPlayback;
        [Header("Target Light")]
        [Tooltip("Nguồn sáng cần điều khiển. Nếu để trống, script sẽ tự lấy Light trên GameObject này.")]
        [SerializeField] private Light targetLight;

        [Header("Random Glitch Settings")]
        [Tooltip("Có tự động nhấp nháy khi game bắt đầu không?")]
        [SerializeField] private bool playOnAwake = true;

        [Tooltip("Cường độ sáng tối thiểu khi nhấp nháy.")]
        [SerializeField, Min(0f)] private float minIntensity = 0f;

        [Tooltip("Cường độ sáng tối đa khi nhấp nháy.")]
        [SerializeField, Min(0f)] private float maxIntensity = 1.5f;

        [Tooltip("Tần số đổi độ sáng mỗi giây (ví dụ: 25 lần/giây).")]
        [SerializeField, Range(1f, 60f)] private float flickerFrequency = 25f;

        [Tooltip("Độ mượt khi chuyển giữa các mức sáng: 0 = giật tức thì (chuẩn đèn huỳnh quang/neon), > 0 = chuyển mượt (đèn sợi đốt).")]
        [SerializeField, Range(0f, 50f)] private float smoothing = 0f;

        [Tooltip("Tỉ lệ % đèn có thể tắt lịm (cường độ về 0) trong lúc nhấp nháy (0 = không bao giờ tắt hẳn).")]
        [SerializeField, Range(0f, 1f)] private float blackoutProbability = 0.15f;

        [Header("Optional Emissive Mesh Sync")]
        [Tooltip("Renderer của bóng đèn nếu muốn đồng bộ độ phát sáng (Emission) của vật liệu.")]
        [SerializeField] private Renderer emissiveRenderer;

        [Tooltip("Index vật liệu phát sáng trên Renderer.")]
        [SerializeField, Min(0)] private int emissiveMaterialIndex = 0;

        [Tooltip("Tên thuộc tính màu Emission trong Shader (mặc định URP là _EmissionColor).")]
        [SerializeField] private string emissionColorPropertyName = "_EmissionColor";

        [Header("Optional Audio & Effects")]
        [Tooltip("AudioSource phát tiếng rè điện / tiếng nổ lách tách khi đèn nhấp nháy.")]
        [SerializeField] private AudioSource buzzAudio;

        [Tooltip("AudioClip tiếng rè điện / nhấp nháy (tùy chọn, sẽ gán vào buzzAudio).")]
        [SerializeField] private AudioClip buzzClip;

        [Tooltip("Hiệu ứng hạt tia lửa điện khi nhấp nháy (tùy chọn).")]
        [SerializeField] private ParticleSystem sparkParticles;

        private float baseIntensity;
        private float currentIntensity;
        private float targetIntensity;
        private bool isFlickering;
        private float timer;

        private MaterialPropertyBlock propertyBlock;
        private int emissionColorPropertyId;
        private Color baseEmissionColor = Color.white;
        private bool hasEmissiveMaterial;
        private float originalBuzzVolume;

        public bool IsFlickering => isFlickering;
        public Light TargetLight => targetLight;

        private void Awake()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }

            if (targetLight != null)
            {
                baseIntensity = targetLight.intensity;
                if (maxIntensity < baseIntensity)
                {
                    maxIntensity = baseIntensity;
                }
            }

            currentIntensity = baseIntensity;
            targetIntensity = baseIntensity;

            CacheEmission();
            ConfigureBuzzAudio();
        }

        private void CacheEmission()
        {
            // Setup Material Property Block để đổi Emission mà không làm clone Material trong bộ nhớ
            if (emissiveRenderer != null)
            {
                propertyBlock = new MaterialPropertyBlock();
                emissionColorPropertyId = Shader.PropertyToID(emissionColorPropertyName);

                Material[] mats = emissiveRenderer.sharedMaterials;
                if (emissiveMaterialIndex < mats.Length && mats[emissiveMaterialIndex] != null)
                {
                    if (mats[emissiveMaterialIndex].HasProperty(emissionColorPropertyId))
                    {
                        baseEmissionColor = mats[emissiveMaterialIndex].GetColor(emissionColorPropertyId);
                        hasEmissiveMaterial = true;
                    }
                }
            }
        }

        private void ConfigureBuzzAudio()
        {
            buzzClip = GameAudioBootstrap.ResolveDefaultClip(buzzClip, "clip.LightFlicker");
            if (buzzClip != null)
            {
                if (buzzAudio == null)
                {
                    buzzAudio = GetComponent<AudioSource>();
                    if (buzzAudio == null)
                    {
                        buzzAudio = gameObject.AddComponent<AudioSource>();
                        buzzAudio.playOnAwake = false;
                        buzzAudio.loop = true;
                        buzzAudio.spatialBlend = 1f;
                        buzzAudio.rolloffMode = AudioRolloffMode.Linear;
                        buzzAudio.minDistance = 1f;
                        buzzAudio.maxDistance = 15f;
                    }
                }
                if (buzzAudio != null && buzzAudio.clip == null)
                {
                    buzzAudio.clip = buzzClip;
                    buzzAudio.loop = true;
                }
            }

            if (buzzAudio != null)
            {
                originalBuzzVolume = buzzAudio.volume;
            }
        }

        private void OnEnable()
        {
            if (playOnAwake)
            {
                StartFlicker();
            }
        }

        private void OnDisable()
        {
            StopFlicker(false);
        }

        private void Update()
        {
            if (!isFlickering || targetLight == null) return;

            // Đổi target intensity theo tần số flickerFrequency
            timer += Time.deltaTime;
            float interval = flickerFrequency > 0f ? (1f / flickerFrequency) : 0.04f;

            if (timer >= interval)
            {
                timer = 0f;

                if (Random.value < blackoutProbability)
                {
                    targetIntensity = 0f;
                }
                else
                {
                    targetIntensity = Random.Range(minIntensity, maxIntensity);
                }
            }

            // Áp dụng smoothing nếu có
            if (smoothing > 0f)
            {
                currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * smoothing);
            }
            else
            {
                currentIntensity = targetIntensity;
            }

            ApplyIntensity(currentIntensity);
        }

        private void ApplyIntensity(float intensity)
        {
            if (targetLight != null)
            {
                targetLight.intensity = intensity;
                targetLight.enabled = intensity > 0.001f;
            }

            // Đồng bộ Emission của Mesh bóng đèn
            if (hasEmissiveMaterial && emissiveRenderer != null)
            {
                float factor = maxIntensity > 0.001f ? (intensity / maxIntensity) : 0f;
                Color currentEmission = baseEmissionColor * factor;

                emissiveRenderer.GetPropertyBlock(propertyBlock, emissiveMaterialIndex);
                propertyBlock.SetColor(emissionColorPropertyId, currentEmission);
                emissiveRenderer.SetPropertyBlock(propertyBlock, emissiveMaterialIndex);
            }

            // Đồng bộ âm thanh rè / chập
            if (buzzAudio != null)
            {
                float factor = maxIntensity > 0.001f ? (intensity / maxIntensity) : 0f;
                buzzAudio.volume = originalBuzzVolume * (1f - factor * 0.5f);
            }
        }

        /// <summary>
        /// Bắt đầu nhấp nháy đèn.
        /// </summary>
        public void StartFlicker()
        {
            isFlickering = true;
            timer = 0f;

            if (buzzAudio != null && !buzzAudio.isPlaying)
            {
                GameAudioBootstrap.BindSource(ref buzzPlayback, buzzAudio, AudioBus.Ambience).Play(null, buzzAudio.clip, false);
            }

            if (sparkParticles != null && !sparkParticles.isPlaying)
            {
                sparkParticles.Play();
            }
        }

        /// <summary>
        /// Dừng nhấp nháy đèn.
        /// </summary>
        /// <param name="turnOff">Nếu true, tắt hẳn đèn; nếu false, trả về độ sáng ban đầu.</param>
        public void StopFlicker(bool turnOff = false)
        {
            isFlickering = false;

            float finalIntensity = turnOff ? 0f : baseIntensity;
            currentIntensity = finalIntensity;
            targetIntensity = finalIntensity;
            ApplyIntensity(finalIntensity);

            if (buzzAudio != null && buzzAudio.isPlaying)
            {
                buzzAudio.Stop();
            }

            if (sparkParticles != null && sparkParticles.isPlaying)
            {
                sparkParticles.Stop();
            }
        }
    }
}
