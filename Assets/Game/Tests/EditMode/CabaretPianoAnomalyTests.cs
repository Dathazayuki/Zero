using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Anomalies;

namespace Zero.Tests.EditMode
{
    public sealed class CabaretPianoAnomalyTests
    {
        [Test]
        public void PhraseOnlyUsesAuthoredKeysAndLeavesTimeForRelease()
        {
            var unique = new HashSet<int>();
            Assert.That(CabaretPianoAnomaly.NoteCount, Is.GreaterThan(0));
            for (int i=0;i<CabaretPianoAnomaly.NoteCount;i++)
            {
                int note=CabaretPianoAnomaly.NoteAt(i);
                Assert.That(note, Is.InRange(0,35), "Phrase must fit the 36 authored white keys");
                unique.Add(note);
            }
            Assert.That(unique.Count, Is.GreaterThan(1));
            Assert.That(CabaretPianoAnomaly.NoteInterval, Is.GreaterThan(.1f));
        }

        [Test]
        public void MissingAudioAndKeysCanActivateResetAndDisableSafely()
        {
            var root = new GameObject("PianoResetTest");
            try
            {
                var anomaly=root.AddComponent<CabaretPianoAnomaly>();
                anomaly.Activate();
                anomaly.ResetAnomaly();
                anomaly.ResetAnomaly();
                root.SetActive(false);
                Assert.That(anomaly.IsActive, Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
