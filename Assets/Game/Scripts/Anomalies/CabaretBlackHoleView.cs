using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretBlackHoleView : MonoBehaviour
    {
        [SerializeField] private Renderer[] floorSurfaces;
        [SerializeField] private Shader floorCutoutShader;
        [SerializeField] private Renderer mouth;
        [SerializeField] private Transform shaft;
        [SerializeField] private Collider[] groundSurfaces;
        private Material[][] originals, cutouts;
        private MaterialPropertyBlock[] originalBlocks, holeBlocks;
        private MaterialPropertyBlock mouthBlock;
        private bool opened;
        private static readonly int Hole = Shader.PropertyToID("_Hole");
        private static readonly int HoleOpen = Shader.PropertyToID("_HoleOpen");
        private static readonly int Open = Shader.PropertyToID("_Open");
        private static readonly int Pulse = Shader.PropertyToID("_Pulse");
        public Vector3 Center { get; private set; }
        public bool IsConfigured => floorSurfaces != null && floorSurfaces.Length > 0
            && floorCutoutShader != null && mouth != null && shaft != null && groundSurfaces != null;

        public bool CanOpenAt(RaycastHit hit, float radius)
        {
            foreach (var ground in groundSurfaces)
            {
                if (ground == null || ground != hit.collider) continue;
                var bounds = ground.bounds;
                return hit.point.x >= bounds.min.x + radius && hit.point.x <= bounds.max.x - radius
                    && hit.point.z >= bounds.min.z + radius && hit.point.z <= bounds.max.z - radius;
            }
            return false;
        }

        public void Capture()
        {
            if (originals != null || !IsConfigured) return;
            originals = new Material[floorSurfaces.Length][]; cutouts = new Material[floorSurfaces.Length][];
            originalBlocks = new MaterialPropertyBlock[floorSurfaces.Length];
            holeBlocks = new MaterialPropertyBlock[floorSurfaces.Length];
            mouthBlock = new MaterialPropertyBlock();
            for (int i = 0; i < floorSurfaces.Length; i++) CaptureSurface(i);
        }

        private void CaptureSurface(int index)
        {
            var surface = floorSurfaces[index];
            if (surface == null) return;
            originals[index] = surface.sharedMaterials;
            cutouts[index] = new Material[originals[index].Length];
            originalBlocks[index] = new MaterialPropertyBlock(); holeBlocks[index] = new MaterialPropertyBlock();
            surface.GetPropertyBlock(originalBlocks[index]); surface.GetPropertyBlock(holeBlocks[index]);
            for (int m = 0; m < cutouts[index].Length; m++)
                cutouts[index][m] = new Material(originals[index][m]) { shader = floorCutoutShader, hideFlags = HideFlags.DontSave };
        }

        public void ShowWarning(Vector3 center, float radius)
        {
            Capture(); Center = center;
            transform.position = center; transform.rotation = Quaternion.identity;
            mouth.transform.localScale = Vector3.one * (radius * 2f / .82f);
            shaft.localScale = new Vector3(radius * 2f, 10f, radius * 2f);
            mouth.gameObject.SetActive(true); shaft.gameObject.SetActive(true);
            SetMouth(0f);
        }

        public void PresentWarning(float progress) => SetMouth(progress);

        public void OpenHole(float radius)
        {
            if (originals == null || opened) return;
            opened = true;
            for (int i = 0; i < floorSurfaces.Length; i++)
            {
                if (floorSurfaces[i] == null) continue;
                holeBlocks[i].SetVector(Hole, new Vector4(Center.x, Center.y, Center.z, radius));
                holeBlocks[i].SetFloat(HoleOpen, 1f);
                floorSurfaces[i].sharedMaterials = cutouts[i]; floorSurfaces[i].SetPropertyBlock(holeBlocks[i]);
            }
            SetMouth(1f);
        }

        private void SetMouth(float progress)
        {
            mouthBlock.SetFloat(Open, opened ? 1f : 0f); mouthBlock.SetFloat(Pulse, progress);
            mouth.SetPropertyBlock(mouthBlock);
        }

        public void Restore()
        {
            if (opened && originals != null)
                for (int i = 0; i < floorSurfaces.Length; i++)
                    if (floorSurfaces[i] != null)
                    { floorSurfaces[i].sharedMaterials = originals[i]; floorSurfaces[i].SetPropertyBlock(originalBlocks[i]); }
            opened = false;
            if (mouth != null) mouth.gameObject.SetActive(false);
            if (shaft != null) shaft.gameObject.SetActive(false);
        }

        private void OnDisable() => Restore();
        private void OnDestroy()
        {
            Restore();
            if (cutouts == null) return;
            foreach (var materials in cutouts) DestroyMaterials(materials);
        }

        private void DestroyMaterials(Material[] materials)
        {
            if (materials == null) return;
            foreach (var material in materials)
            {
                if (material == null) continue;
                if (Application.isPlaying) Destroy(material);
                else DestroyImmediate(material);
            }
        }
    }
}
