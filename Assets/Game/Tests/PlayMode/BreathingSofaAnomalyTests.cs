#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Anomalies;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.PlayMode
{
    public sealed class BreathingSofaAnomalyTests : LivingRoomFamilyAnomalyTests
    {
        [UnityTest]
        public IEnumerator LongSofaBreathesAndRestoresItsNormalMesh()
        {
            var sofa = (BreathingSofaAnomaly)Select("IR2-10");
            Assert.That(sofa.OccupiedSeat.sharedMesh.blendShapeCount, Is.EqualTo(1));
            var deltas = new Vector3[sofa.OccupiedSeat.sharedMesh.vertexCount];
            sofa.OccupiedSeat.sharedMesh.GetBlendShapeFrameVertices(0, 0, deltas, null, null);
            float deepest = 0f;
            foreach (Vector3 delta in deltas) deepest = Mathf.Min(deepest, delta.y);
            Assert.That(deepest, Is.LessThan(-0.08f), "The actual cushion mesh must have an occupied depression.");
            float before = sofa.OccupiedSeat.GetBlendShapeWeight(0);
            yield return new WaitForSeconds(0.6f);
            Assert.That(Mathf.Abs(sofa.OccupiedSeat.GetBlendShapeWeight(0) - before), Is.GreaterThan(2f));
            preview.ResetPreview();
            sofa.ResetAnomaly();
            Assert.That(sofa.OccupiedSeat.gameObject.activeSelf, Is.False);
            Assert.That(sofa.OccupiedSeat.GetBlendShapeWeight(0), Is.Zero);
        }
    }
}
#endif
