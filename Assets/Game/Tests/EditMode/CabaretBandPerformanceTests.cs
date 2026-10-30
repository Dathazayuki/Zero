using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretBandPerformanceTests
    {
        [Test]
        public void DrumstickTipTouchesAuthoredContactAndGripStaysFixedWhileLifting()
        {
            var contact = new Pose(new Vector3(1.94f, 1.284f, 5.78f), Quaternion.Euler(75f, 0f, 0f));
            const float half = .19f;
            Pose down = CabaretBandPerformance.EvaluateStrike(contact, half, 0f);
            Pose lifted = CabaretBandPerformance.EvaluateStrike(contact, half, 35f);
            Vector3 tip = down.position + down.rotation * Vector3.up * half;
            Assert.That(Vector3.Distance(tip, contact.position), Is.LessThan(.00001f));
            Vector3 grip = down.position - down.rotation * Vector3.up * half;
            Vector3 liftedGrip = lifted.position - lifted.rotation * Vector3.up * half;
            Assert.That(Vector3.Distance(grip, liftedGrip), Is.LessThan(.00001f));
            Assert.That((lifted.position + lifted.rotation * Vector3.up * half).y, Is.GreaterThan(tip.y));
        }
    }
}
