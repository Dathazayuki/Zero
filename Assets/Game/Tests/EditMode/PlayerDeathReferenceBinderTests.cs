using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.Tests.EditMode
{
    public sealed class PlayerDeathReferenceBinderTests
    {
        [Test]
        public void MissingOptionalDeathPresentationKeepsDefaultStyle()
        {
            Assert.IsFalse(PlayerDeathReferenceBinder.IsFreeze(null, null));
        }

        [Test]
        public void OptionalAudioSourceIsCreatedOnlyOnce()
        {
            var root = new GameObject("Death reference test");
            try
            {
                AudioSource first = PlayerDeathReferenceBinder.ResolveAudio(root, null);
                Assert.AreSame(first, PlayerDeathReferenceBinder.ResolveAudio(root, null));
                Assert.IsFalse(first.playOnAwake);
                Assert.AreEqual(0f, first.spatialBlend);
                PlayerDeathReferenceBinder.BindBlackout(null);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
