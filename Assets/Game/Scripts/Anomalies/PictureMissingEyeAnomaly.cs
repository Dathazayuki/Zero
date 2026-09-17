using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class PictureMissingEyeAnomaly : AnomalyBehaviour
    {
        [Header("Target Picture Renderers")]
        [Tooltip("Một hoặc nhiều Renderer của các bức tranh cần thay Material.")]
        [SerializeField] private Renderer[] pictureRenderers;

        [Tooltip("Chỉ số slot Material cần thay đổi trên Renderer (mặc định là 0).")]
        [SerializeField, Min(0)] private int materialIndex = 0;

        [Header("Materials")]
        [Tooltip("Material của tranh ở trạng thái bình thường.")]
        [SerializeField] private Material normalMaterial;

        [Tooltip("Material của tranh khi xảy ra Anomaly (mắt bị mất / khoét rỗng).")]
        [SerializeField] private Material missingEyeMaterial;

        private Material cachedOriginalMaterial;
        private bool normalStateCaptured;

        public Renderer[] PictureRenderers => pictureRenderers;
        public Material NormalMaterial => normalMaterial;
        public Material MissingEyeMaterial => missingEyeMaterial;

        public void Configure(Renderer[] renderers, Material normalMat, Material missingMat, int matIndex = 0)
        {
            pictureRenderers = renderers;
            normalMaterial = normalMat;
            missingEyeMaterial = missingMat;
            materialIndex = matIndex;
            CaptureNormalState();
        }

        public override void CaptureNormalState()
        {
            if (pictureRenderers == null || pictureRenderers.Length == 0)
            {
                Renderer r = GetComponentInChildren<Renderer>(true);
                if (r != null)
                {
                    pictureRenderers = new[] { r };
                }
            }

            if (pictureRenderers != null && pictureRenderers.Length > 0 && pictureRenderers[0] != null)
            {
                Material[] mats = pictureRenderers[0].sharedMaterials;
                if (materialIndex >= 0 && materialIndex < mats.Length)
                {
                    cachedOriginalMaterial = mats[materialIndex];
                }
                else
                {
                    cachedOriginalMaterial = pictureRenderers[0].sharedMaterial;
                }

                if (normalMaterial == null)
                {
                    normalMaterial = cachedOriginalMaterial;
                }
            }

            // Đảm bảo trạng thái ban đầu hiển thị normalMaterial
            ApplyMaterial(normalMaterial != null ? normalMaterial : cachedOriginalMaterial);
            normalStateCaptured = true;
        }

        protected override void OnActivate()
        {
            if (!normalStateCaptured)
            {
                CaptureNormalState();
            }

            if (missingEyeMaterial == null)
            {
                Debug.LogWarning($"PICTURE_MISSING_EYE_INVALID: Chưa gán Missing Eye Material trên {name}", this);
                return;
            }

            ApplyMaterial(missingEyeMaterial);
            Debug.Log($"PICTURE_MISSING_EYE_ACTIVATED: anomaly={name}", this);
        }

        protected override void OnResetAnomaly()
        {
            Material targetNormal = normalMaterial != null ? normalMaterial : cachedOriginalMaterial;
            ApplyMaterial(targetNormal);
        }

        private void OnDisable()
        {
            OnResetAnomaly();
        }

        private void ApplyMaterial(Material mat)
        {
            if (mat == null || pictureRenderers == null)
            {
                return;
            }

            for (int i = 0; i < pictureRenderers.Length; i++)
            {
                Renderer r = pictureRenderers[i];
                if (r == null) continue;

                Material[] mats = r.sharedMaterials;
                if (materialIndex >= 0 && materialIndex < mats.Length)
                {
                    mats[materialIndex] = mat;
                    r.sharedMaterials = mats;
                }
                else
                {
                    r.sharedMaterial = mat;
                }
            }
        }
    }
}
