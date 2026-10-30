using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Phase = Zero.AnomalyPrototype.Anomalies.CabaretRitualRules.Phase;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretRitualLighting : MonoBehaviour
    {
        [SerializeField] private Light[] roomLights;
        [SerializeField] private Light playerSpot;
        [SerializeField] private Light hostSpot;
        [SerializeField] private Renderer[] targetEyes;
        [SerializeField] private Transform safeDisc;
        [SerializeField] private CabaretRitualDoor door;
        [SerializeField] private Renderer[] lampEmitters;
        [SerializeField, Min(.5f)] private float safeRadius = 1.35f;
        [FormerlySerializedAs("routeSpeed"), SerializeField, Min(.1f)] private float followSpeed = 1.8f;
        private float[] intensities;
        private bool[] enabledLights;
        private CabaretRitualEye[] targets;
        private AmbientMode ambientMode;
        private Color ambientColour;
        private float ambientIntensity, reflectionIntensity;
        private bool captured, ownsLighting;
        private MaterialPropertyBlock colours;
        private MaterialPropertyBlock[] emitterBlocks;
        private Color[] emitterColours;
        private int[] emitterLightIndices;
        private Renderer discRenderer, coneRenderer;
        private static readonly int BaseColour = Shader.PropertyToID("_BaseColor");
        public Vector3 SafeCentre { get; private set; }
        public bool IsLocked => door != null && door.IsLocked;
        public bool IsConfigured => roomLights != null && roomLights.Length > 0 && playerSpot != null && hostSpot != null
            && targetEyes != null && targetEyes.Length == 3 && safeDisc != null && door != null && door.IsConfigured;

        public void Capture()
        {
            if (captured || !IsConfigured) return;
            captured = true;
            intensities = new float[roomLights.Length]; enabledLights = new bool[roomLights.Length];
            for (int i = 0; i < roomLights.Length; i++)
            { intensities[i] = roomLights[i].intensity; enabledLights[i] = roomLights[i].enabled; }
            targets = new CabaretRitualEye[3];
            for (int i = 0; i < targets.Length; i++)
            { targets[i] = targetEyes[i].GetComponent<CabaretRitualEye>(); targets[i]?.Capture(); }
            colours = new MaterialPropertyBlock();
            discRenderer = safeDisc.GetComponent<Renderer>();
            coneRenderer = playerSpot.GetComponentInChildren<Renderer>(true);
            CaptureEmitters();
            door.Capture();
        }

        private void CaptureEmitters()
        {
            int count = lampEmitters != null ? lampEmitters.Length : 0;
            emitterBlocks = new MaterialPropertyBlock[count]; emitterColours = new Color[count]; emitterLightIndices = new int[count];
            for (int i = 0; i < count; i++)
            {
                emitterBlocks[i] = new MaterialPropertyBlock(); lampEmitters[i].GetPropertyBlock(emitterBlocks[i]);
                Material material = lampEmitters[i].sharedMaterial;
                emitterColours[i] = material != null && material.HasProperty(BaseColour) ? material.GetColor(BaseColour) : Color.white;
                float nearest = float.MaxValue;
                for (int j = 0; j < roomLights.Length; j++)
                {
                    float distance = (roomLights[j].transform.position - lampEmitters[i].bounds.center).sqrMagnitude;
                    if (distance >= nearest) continue;
                    nearest = distance; emitterLightIndices[i] = j;
                }
            }
        }

        public void Begin()
        {
            Capture();
            if (!captured || ownsLighting) return;
            ownsLighting = true;
            ambientMode = RenderSettings.ambientMode; ambientColour = RenderSettings.ambientLight;
            ambientIntensity = RenderSettings.ambientIntensity; reflectionIntensity = RenderSettings.reflectionIntensity;
            playerSpot.gameObject.SetActive(false); hostSpot.gameObject.SetActive(false);
            safeDisc.gameObject.SetActive(false);
            for (int i = 0; i < targetEyes.Length; i++) targetEyes[i].gameObject.SetActive(false);
            door.SetClosed(true);
        }

        public void Blackout(float progress)
        {
            if (!ownsLighting) return;
            float count = roomLights.Length;
            for (int i = 0; i < roomLights.Length; i++)
            {
                float amount = 1f - Mathf.Clamp01(progress * count - i);
                roomLights[i].intensity = intensities[i] * amount;
                roomLights[i].enabled = enabledLights[i] && amount > 0f;
            }
            for (int i = 0; i < emitterBlocks.Length; i++)
            {
                float amount = 1f - Mathf.Clamp01(progress * count - emitterLightIndices[i]);
                colours.SetColor(BaseColour, emitterColours[i] * amount);
                colours.SetColor("_Color", emitterColours[i] * amount);
                colours.SetColor("_EmissionColor", Color.black);
                lampEmitters[i].SetPropertyBlock(colours);
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(ambientColour * ambientIntensity, Color.black, progress);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.reflectionIntensity = Mathf.Lerp(reflectionIntensity, 0f, progress);
        }

        public void Reveal(Vector3 feet)
        {
            Blackout(1f);
            SafeCentre = feet;
            RenderSettings.ambientLight = new Color(.018f, .02f, .025f);
            playerSpot.gameObject.SetActive(true); hostSpot.gameObject.SetActive(true);
            safeDisc.gameObject.SetActive(true);
            for (int i = 0; i < targetEyes.Length; i++) targetEyes[i].gameObject.SetActive(true);
            Present(Phase.Active, 0f);
        }

        public void Track(float delta, Vector3 feet, Phase phase, float difficulty)
        {
            if (!ownsLighting || phase != Phase.Active) return;
            feet.y = SafeCentre.y;
            SafeCentre = Vector3.MoveTowards(SafeCentre, feet, followSpeed * difficulty * delta);
        }

        public void Present(Phase phase, float progress)
        {
            playerSpot.transform.SetPositionAndRotation(SafeCentre + Vector3.up * 3.25f, Quaternion.Euler(90f, 0f, 0f));
            playerSpot.spotAngle = Mathf.Atan(safeRadius / 3.25f) * 2f * Mathf.Rad2Deg;
            playerSpot.innerSpotAngle = playerSpot.spotAngle * .92f;
            Color colour = new Color(1f, .93f, .75f);
            if (phase == Phase.Warning) colour = Color.Lerp(Color.yellow, new Color(1f, .3f, 0f), progress);
            if (phase == Phase.Attacking) colour = Color.red;
            playerSpot.color = colour;
            colours.Clear(); colours.SetColor(BaseColour, colour);
            if (discRenderer != null) discRenderer.SetPropertyBlock(colours);
            if (coneRenderer != null) coneRenderer.SetPropertyBlock(colours);
            safeDisc.SetPositionAndRotation(SafeCentre + Vector3.up * .012f, Quaternion.Euler(90f, 0f, 0f));
            safeDisc.localScale = Vector3.one * safeRadius * 2f;
        }

        public void PresentTargets(float delta, float time, int mask, Phase phase, Vector3 playerPosition)
        {
            if (targets == null) return;
            bool canFlee = CabaretRitualRules.CanEyesFlee(phase, mask);
            for (int i = 0; i < targets.Length; i++)
                targets[i]?.Present(delta, time + i * 1.7f, (mask & (1 << i)) != 0,
                    canFlee ? playerPosition : Vector3.positiveInfinity, CabaretRitualRules.EyeSpeedMultiplier(mask));
        }

        public bool ContainsStrike(Vector3 feet, float playerRadius)
        {
            Vector3 offset = feet - SafeCentre;
            float radius = safeRadius + playerRadius;
            return Mathf.Abs(offset.y) < .8f && offset.x * offset.x + offset.z * offset.z <= radius * radius;
        }

        public int HitTargets()
        {
            int mask = 0;
            if (targets == null) return mask;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null) continue;
                Vector3 offset = targets[i].transform.position - SafeCentre; offset.y = 0f;
                if (offset.sqrMagnitude <= safeRadius * safeRadius) mask |= 1 << i;
            }
            return mask;
        }

        public void Unlock()
        {
            door.SetClosed(false);
            if (ownsLighting) RestoreRoomLights();
            playerSpot.gameObject.SetActive(false); safeDisc.gameObject.SetActive(false);
        }

        private void RestoreRoomLights()
        {
            for (int i = 0; i < roomLights.Length; i++)
                if (roomLights[i] != null) { roomLights[i].intensity = intensities[i]; roomLights[i].enabled = enabledLights[i]; }
            for (int i = 0; i < emitterBlocks.Length; i++)
                if (lampEmitters[i] != null) lampEmitters[i].SetPropertyBlock(emitterBlocks[i]);
            RenderSettings.ambientMode = ambientMode; RenderSettings.ambientLight = ambientColour;
            RenderSettings.ambientIntensity = ambientIntensity; RenderSettings.reflectionIntensity = reflectionIntensity;
            ownsLighting = false;
        }

        public void Restore()
        {
            if (ownsLighting) RestoreRoomLights();
            if (playerSpot != null) playerSpot.gameObject.SetActive(false);
            if (hostSpot != null) hostSpot.gameObject.SetActive(false);
            if (safeDisc != null) safeDisc.gameObject.SetActive(false);
            if (targetEyes != null)
                for (int i = 0; i < targetEyes.Length; i++)
                    if (targetEyes[i] != null)
                    { targets?[i]?.Restore(); targetEyes[i].SetPropertyBlock(null); targetEyes[i].gameObject.SetActive(false); }
            door?.Restore();
        }

        private void OnDisable() => Restore();
    }
}
