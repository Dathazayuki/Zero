using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class SoundCatalogIndexTests
    {
        [Test]
        public void NamedSoundOverridesGeneratedClipInventory_RegardlessOfAddOrder()
        {
            AudioClip clip = AudioClip.Create("test", 128, 1, 44100, false);
            try
            {
                var generated = new SoundDefinition("clip.test", clip, AudioBus.Sfx);
                var named = new SoundDefinition("office.ambience", clip, AudioBus.Ambience);
                var index = new SoundCatalogIndex();
                Assert.That(index.Add(generated), Is.True);
                Assert.That(index.Add(named), Is.True);
                Assert.That(index.Resolve(null, clip), Is.SameAs(named));
                Assert.That(index.Resolve("missing.id", clip), Is.Null);
            }
            finally { Object.DestroyImmediate(clip); }
        }
    }
}
