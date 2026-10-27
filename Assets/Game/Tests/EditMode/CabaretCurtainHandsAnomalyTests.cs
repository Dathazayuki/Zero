using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretCurtainHandsAnomalyTests
    {
        [Test]
        public void ResetDuringRetreatRestoresBothGroupsAndReactivationShowsThem()
        {
            var root=new GameObject("CurtainHandsTest");
            try
            {
                var left=new GameObject("Left").transform; left.SetParent(root.transform);
                var right=new GameObject("Right").transform; right.SetParent(root.transform);
                left.localPosition=Vector3.left; right.localPosition=Vector3.right;
                var anomaly=root.AddComponent<CabaretCurtainHandsAnomaly>();
                anomaly.Configure(root.transform,new[]{left,right},new[]{Vector3.forward,Vector3.forward});
                anomaly.CaptureNormalState(); anomaly.Activate();
                left.localPosition+=Vector3.forward*.4f; right.gameObject.SetActive(false);
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(left.localPosition,Is.EqualTo(Vector3.left));
                Assert.That(right.localPosition,Is.EqualTo(Vector3.right));
                Assert.That(left.gameObject.activeSelf,Is.False);
                Assert.That(right.gameObject.activeSelf,Is.False);
                anomaly.Activate();
                Assert.That(left.gameObject.activeSelf && right.gameObject.activeSelf,Is.True);
                root.SetActive(false);
                Assert.That(left.gameObject.activeSelf,Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
