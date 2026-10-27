using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretAudienceAnomalyTests
    {
        [Test]
        public void ResetRestoresDisplaysChairsAndHidesAudienceIdempotently()
        {
            var root=new GameObject("AudienceTest");
            try
            {
                var displays=new GameObject("Displays"); displays.transform.SetParent(root.transform);
                var audience=new GameObject("Audience"); audience.transform.SetParent(root.transform);
                audience.SetActive(false);
                var chair=new GameObject("Chair").transform; chair.SetParent(root.transform);
                var original=Quaternion.Euler(0,90,0); chair.localRotation=original;
                var anomaly=root.AddComponent<CabaretAudienceAnomaly>();
                var so=new SerializedObject(anomaly);
                so.FindProperty("normalDisplays").objectReferenceValue=displays;
                so.FindProperty("audience").objectReferenceValue=audience;
                var chairs=so.FindProperty("turnedChairs"); chairs.arraySize=1;
                chairs.GetArrayElementAtIndex(0).objectReferenceValue=chair;
                so.ApplyModifiedPropertiesWithoutUndo();
                anomaly.CaptureNormalState(); anomaly.Activate();
                displays.SetActive(false); audience.SetActive(true); chair.localRotation=Quaternion.identity;
                anomaly.ResetAnomaly(); anomaly.ResetAnomaly();
                Assert.That(displays.activeSelf,Is.True);
                Assert.That(audience.activeSelf,Is.False);
                Assert.That(Quaternion.Angle(chair.localRotation,original),Is.LessThan(.01f));
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
