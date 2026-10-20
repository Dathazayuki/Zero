using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class FamilyTelevisionAnomaly : AnomalyBehaviour
    {
        [SerializeField] private TelevisionFeed feed;
        public TelevisionFeed Feed => feed;
        public void Configure(TelevisionFeed television) => feed = television;
        protected override void OnActivate() => feed?.Begin(true);
        protected override void OnResetAnomaly() => feed?.Stop();
        private void OnDisable() => ResetAnomaly();
    }
}
