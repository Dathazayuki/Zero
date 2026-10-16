using NUnit.Framework;
using UnityEngine;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class PauseMenuLayoutTests
    {
        [Test]
        public void CanvasOwnsAllPagesAndStartsHidden()
        {
            var parent = new GameObject("PauseLayoutTest");
            try
            {
                PauseMenuView view = PauseMenuView.Create(parent.transform, null);
                Assert.That(view.gameObject.activeSelf, Is.False);
                Assert.That(view.GetComponent<Canvas>().renderMode, Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                view.Show();
                Assert.That(view.transform.Find("MenuCard/Main").gameObject.activeSelf, Is.True);
                Assert.That(view.transform.Find("MenuCard/Settings").gameObject.activeSelf, Is.False);
                int resumes = 0;
                view.ResumeRequested += () => resumes++;
                view.Back();
                Assert.That(resumes, Is.EqualTo(1));
                view.Hide();
                Assert.That(view.gameObject.activeSelf, Is.False);
            }
            finally { Object.DestroyImmediate(parent); }
        }
    }
}
