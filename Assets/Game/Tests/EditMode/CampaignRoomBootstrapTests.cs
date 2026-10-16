using NUnit.Framework;
using UnityEngine;
using Zero.EnvironmentLoop;

namespace Zero.Tests.EditMode
{
    public sealed class CampaignRoomBootstrapTests
    {
        [Test]
        public void MissingSerializedManagerIsResolvedBeforeRoomInitializationAndCached()
        {
            var root = new GameObject("Campaign manager binding test");
            root.SetActive(false);
            try
            {
                var building = root.AddComponent<InfiniteBuildingManager>();
                var existing = root.AddComponent<DirectionalAnomalyGameManager>();
                Assert.AreSame(existing, building.ResolveAnomalyManager());
                Assert.AreSame(existing, building.ResolveAnomalyManager());
                Assert.AreEqual(1, root.GetComponents<DirectionalAnomalyGameManager>().Length);
                Assert.AreSame(existing, building.AnomalyGameManager);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
