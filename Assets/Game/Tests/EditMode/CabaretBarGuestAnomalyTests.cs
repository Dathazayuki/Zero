using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretBarGuestAnomalyTests
    {
        [Test]
        public void ResetAndDisableAreSafeBeforeReferencesAreBound()
        {
            var root = new GameObject("GuestResetTest");
            try
            {
                var guest = root.AddComponent<CabaretBarGuestAnomaly>();
                Assert.That(guest.IsConfigured, Is.False);
                guest.ResetAnomaly(); guest.ResetAnomaly(); root.SetActive(false);
                Assert.That(guest.IsActive, Is.False);
                Assert.That(guest.State, Is.EqualTo(CabaretBarGuestRules.Phase.Unseen));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
