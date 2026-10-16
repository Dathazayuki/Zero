using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class DarumaSignalLamp : MonoBehaviour
    {
        [SerializeField] private DarumaSession session;
        [SerializeField] private Light bulb;
        [SerializeField] private Renderer diffuser;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private Color normalColor;
        private MaterialPropertyBlock normalBlock;
        private MaterialPropertyBlock warningBlock;

        private void Awake()
        {
            if (bulb != null) normalColor = bulb.color;
            if (diffuser == null) return;
            normalBlock = new MaterialPropertyBlock();
            warningBlock = new MaterialPropertyBlock();
            diffuser.GetPropertyBlock(normalBlock);
            diffuser.GetPropertyBlock(warningBlock);
            warningBlock.SetColor(BaseColor, Color.red);
            warningBlock.SetColor(EmissionColor, Color.red * 3f);
        }

        private void OnEnable()
        {
            if (session == null || bulb == null || diffuser == null)
            {
                Debug.LogError("DARUMA_LAMP_CONFIGURATION_INVALID: missing reference", this);
                return;
            }
            session.StateChanged += Present;
            Present(session.State);
        }

        private void Present(DarumaRules.Phase phase)
        {
            if (bulb == null || diffuser == null) return;
            bool watching = phase == DarumaRules.Phase.TurningToWatch ||
                phase == DarumaRules.Phase.Watching || phase == DarumaRules.Phase.Caught;
            bulb.color = watching ? Color.red : normalColor;
            diffuser.SetPropertyBlock(watching ? warningBlock : normalBlock);
        }

        private void OnDisable()
        {
            if (session != null) session.StateChanged -= Present;
            if (bulb != null) bulb.color = normalColor;
            if (diffuser != null) diffuser.SetPropertyBlock(normalBlock);
        }
    }
}
