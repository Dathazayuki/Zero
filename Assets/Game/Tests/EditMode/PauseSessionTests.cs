using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class PauseSessionTests
    {
        [Test]
        public void DuplicatePauseAndCleanupRestoreOriginalGlobalState()
        {
            float originalScale = Time.timeScale;
            bool originalAudio = AudioListener.pause;
            var session = new PauseSession();
            try
            {
                Time.timeScale = 0.5f;
                AudioListener.pause = true;
                Assert.That(session.Begin(), Is.True);
                Assert.That(session.Begin(), Is.False);
                Assert.That(Time.timeScale, Is.Zero);
                session.End();
                session.End();
                Assert.That(session.IsPaused, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(0.5f));
                Assert.That(AudioListener.pause, Is.True);
            }
            finally
            {
                session.End();
                Time.timeScale = originalScale;
                AudioListener.pause = originalAudio;
            }
        }
    }
}
