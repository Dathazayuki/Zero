using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretCurtainHandsAnomalyTests
    {
        [Test]
        public void StageEntryRetreatsBothGroupsTogetherAndResetRestoresThem()
        {
            var root=new GameObject("CurtainHandsTest");
            try
            {
                var left=new GameObject("Left").transform; left.SetParent(root.transform);
                var right=new GameObject("Right").transform; right.SetParent(root.transform);
                var player=new GameObject("Player").transform; player.SetParent(root.transform);
                left.localPosition=Vector3.left; right.localPosition=Vector3.right;
                var stage=root.AddComponent<BoxCollider>();
                stage.center=new Vector3(0f,1f,5f); stage.size=new Vector3(4f,3f,2f);
                var anomaly=root.AddComponent<CabaretCurtainHandsAnomaly>();
                anomaly.Configure(player,stage,new[]{left,right},new[]{Vector3.forward,Vector3.forward});
                anomaly.CaptureNormalState(); anomaly.Activate();
                anomaly.Advance(.2f);
                Assert.That(left.localPosition.z,Is.Zero);
                Assert.That(right.localPosition.z,Is.Zero);
                player.localPosition=Vector3.forward*5f;
                anomaly.Advance(.2f);
                Assert.That(left.localPosition.z,Is.GreaterThan(0f));
                Assert.That(right.localPosition.z,Is.EqualTo(left.localPosition.z).Within(.0001f));
                anomaly.Advance(.5f);
                Assert.That(left.gameObject.activeSelf || right.gameObject.activeSelf,Is.False);
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
