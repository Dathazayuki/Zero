using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretCupShuffleAnomalyTests
    {
        [Test]
        public void PassingCupsRemainLevelAndClearEachOther()
        {
            var a=new Vector3(-1.54f,1.075f,-1.9f);
            var b=new Vector3(-1.62f,1.075f,-1.48f);
            for (int i=0;i<=100;i++)
            {
                float t=i/100f;
                Vector3 left=CabaretCupShuffleAnomaly.SwapPosition(a,b,t,.06f);
                Vector3 right=CabaretCupShuffleAnomaly.SwapPosition(b,a,t,.06f);
                Assert.That(left.y,Is.EqualTo(a.y).Within(.0001f));
                Assert.That(Vector3.Distance(left,right),Is.GreaterThan(.11f));
            }
            Assert.That(Vector3.Distance(CabaretCupShuffleAnomaly.SwapPosition(a,b,1,.06f),b),Is.LessThan(.0001f));
        }

        [Test]
        public void ResetAndDisableRestoreEveryCupWithoutChangingItsChildren()
        {
            var root=new GameObject("ShuffleTest");
            try
            {
                var cups=new Transform[3];
                for (int i=0;i<3;i++)
                {
                    cups[i]=new GameObject("Cup"+i).transform; cups[i].SetParent(root.transform);
                    cups[i].localPosition=Vector3.forward*i;
                }
                var anomaly=root.AddComponent<CabaretCupShuffleAnomaly>();
                anomaly.Configure(root.transform,cups); anomaly.Activate();
                cups[0].localPosition=Vector3.one;
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                for (int i=0;i<3;i++) Assert.That(cups[i].localPosition,Is.EqualTo(Vector3.forward*i));
                anomaly.Activate(); cups[2].localPosition=Vector3.zero; root.SetActive(false);
                Assert.That(cups[2].localPosition,Is.EqualTo(Vector3.forward*2));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
