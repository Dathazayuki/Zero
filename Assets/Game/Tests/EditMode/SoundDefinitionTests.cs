using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class SoundDefinitionTests
    {
        [Test]
        public void DistanceAndGainPropertiesClampInvalidAuthoringValues()
        {
            var sound = new SoundDefinition("test", null, AudioBus.Sfx);
            JsonUtility.FromJsonOverwrite("{\"volume\":2,\"minDistance\":5,\"maxDistance\":2}", sound);
            Assert.That(sound.Volume, Is.EqualTo(1f));
            Assert.That(sound.MaxDistance, Is.EqualTo(5f));
        }
    }
}
