using NUnit.Framework;
using Zero.AnomalyPrototype.Flow;

namespace Zero.Tests.EditMode
{
    public sealed class GameFlowScenePhaseResolverTests
    {
        [Test]
        public void TitleLoadCorrectsBootstrapOpeningPhase()
        {
            Assert.AreEqual(GameFlowPhase.Title, Resolve("TitleScene", GameFlowPhase.Opening));
        }

        [Test]
        public void TransitionDoesNotOverrideRequestedPhase()
        {
            Assert.AreEqual(GameFlowPhase.Ending, Resolve("OfficeLoopScene",
                GameFlowPhase.Ending, true));
        }

        [Test]
        public void LoopLoadedAfterOpeningBecomesGameplay()
        {
            Assert.AreEqual(GameFlowPhase.LoopGameplay, Resolve("InfiniteStairwayScene",
                GameFlowPhase.Opening));
        }

        private static GameFlowPhase Resolve(string scene, GameFlowPhase phase,
            bool transitioning = false)
        {
            var names = new GameFlowScenePhaseResolver.SceneNames("TitleScene",
                "OfficeLoopScene", "InfiniteStairwayScene");
            return GameFlowScenePhaseResolver.Resolve(scene, phase, names, transitioning);
        }
    }
}
