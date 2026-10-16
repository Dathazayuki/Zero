using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;
using Zero.Editor.CodePolicy;

namespace Zero.Tests.EditMode
{
    public sealed class AudioDefaultsPolicyRegressionTests
    {
        [TestCase("Anomalies/AnomalyDeathPresentation")]
        [TestCase("Anomalies/ArrowLightFlickerAnomaly")]
        [TestCase("Anomalies/DemonHandsOverlay")]
        [TestCase("Flow/BackroomMonsterController")]
        [TestCase("Flow/OfficeExitDoor")]
        [TestCase("Flow/Room2LightController")]
        [TestCase("HorrorEvents/FootstepsBehindEvent")]
        [TestCase("HorrorEvents/LightFlickerEvent")]
        [TestCase("Interaction/PushDoorController")]
        [TestCase("Player/PlayerDeathController")]
        [TestCase("Utilities/FlickeringLightController")]
        [TestCase("Flow/Audio/GameAudioBootstrap")]
        public void RuntimeAudioHasNoEditorDependency(string name)
        {
            string path = "Assets/Game/Scripts/" + name + ".cs";
            var metrics = CodePolicyScanner.AnalyzeFile(Path.GetFullPath(path), path);
            Assert.That(metrics.runtimeEditorReferences, Is.Zero, path);
        }

        [TestCase("Flow/BackroomMonsterController")]
        [TestCase("Utilities/FlickeringLightController")]
        public void RefactoredMethodsStayWithinBranchLimit(string name)
        {
            string path = "Assets/Game/Scripts/" + name + ".cs";
            var metrics = CodePolicyScanner.AnalyzeFile(Path.GetFullPath(path), path);
            Assert.That(metrics.maxBranches, Is.LessThanOrEqualTo(10), path);
        }

        [TestCase("creepyDemonHand")]
        [TestCase("Door_Open")]
        [TestCase("HorrorGirl_FootStep")]
        [TestCase("LightFlicker")]
        [TestCase("LightGlitch")]
        [TestCase("LightOnOff")]
        public void RuntimeCatalogResolvesSameDefaultAsFormerEditorLookup(string name)
        {
            AudioClip expected = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/" + name + ".wav");
            Assert.That(expected, Is.Not.Null);
            Assert.That(GameAudioBootstrap.ResolveDefaultClip(null, "clip." + name), Is.SameAs(expected));
        }

        [Test]
        public void AuthoredClipWinsEvenWhenDefaultIdIsUnknown()
        {
            AudioClip authored = AudioClip.Create("Authored", 32, 1, 44100, false);
            try
            {
                Assert.That(GameAudioBootstrap.ResolveDefaultClip(authored, "unknown"), Is.SameAs(authored));
            }
            finally { Object.DestroyImmediate(authored); }
        }

        [Test]
        public void MissingDefaultFailsSafely()
        {
            Assert.That(GameAudioBootstrap.ResolveDefaultClip(null, "unknown"), Is.Null);
            Assert.That(GameAudioBootstrap.ResolveDefaultClip(null, null), Is.Null);
        }
    }
}
