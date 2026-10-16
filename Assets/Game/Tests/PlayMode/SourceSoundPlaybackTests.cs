using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.TestTools;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.PlayMode
{
    public sealed class SourceSoundPlaybackTests
    {
        [UnityTest]
        public IEnumerator AuthoredClipWinsAndWorldSoundUsesDistanceAttenuation()
        {
            AudioClip catalogClip = AudioClip.Create("catalog", 128, 1, 44100, false);
            AudioClip authoredClip = AudioClip.Create("authored", 128, 1, 44100, false);
            SoundCatalog catalog = ScriptableObject.CreateInstance<SoundCatalog>();
            catalog.Configure(null, new AudioMixerGroup[0], new List<SoundDefinition>
            {
                new SoundDefinition("test.door", catalogClip, AudioBus.Sfx)
            });
            GameObject serviceObject = new GameObject("Audio service test");
            AudioPlaybackService service = serviceObject.AddComponent<AudioPlaybackService>();
            service.Initialize(catalog, 1);
            GameObject emitterObject = new GameObject("World emitter test");
            AudioSource source = emitterObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            var playback = new SourceSoundPlayback(source, service, AudioBus.Sfx, true);
            playback.Play("test.door", authoredClip, false);
            Assert.That(source.clip, Is.SameAs(authoredClip));
            Assert.That(source.spatialBlend, Is.EqualTo(1f));
            Assert.That(source.rolloffMode, Is.EqualTo(AudioRolloffMode.Linear));
            Assert.That(source.maxDistance, Is.GreaterThan(source.minDistance));
            playback.Stop();
            Object.Destroy(emitterObject);
            Object.Destroy(serviceObject);
            Object.Destroy(catalog);
            Object.Destroy(catalogClip);
            Object.Destroy(authoredClip);
            yield return null;
        }
    }
}
