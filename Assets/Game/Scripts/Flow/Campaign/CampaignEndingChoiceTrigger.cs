using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CampaignEndingChoiceTrigger : MonoBehaviour
    {
        [SerializeField] private EndingIdentity ending;
        [SerializeField] private TrueEndingGameplayController trueEnding;
        private bool consumed;

        private void OnTriggerEnter(Collider other)
        {
            if (consumed) return;
            var player = other.GetComponentInParent<FirstPersonController>();
            var flow = GameFlowController.Instance;
            if (player == null || flow == null || flow.Campaign?.IsActive != true || !flow.Campaign.CanPresentEndings) return;
            consumed = ending == EndingIdentity.CurtainCall
                ? trueEnding != null && trueEnding.BeginFinalPresentation(player)
                : flow.BeginCampaignEnding(ending);
        }
    }
}
