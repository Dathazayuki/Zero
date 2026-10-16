namespace Zero.AnomalyPrototype.Flow
{
    public static class GameFlowScenePhaseResolver
    {
        public readonly struct SceneNames
        {
            public readonly string Title;
            public readonly string Office;
            public readonly string Loop;

            public SceneNames(string title, string office, string loop)
            {
                Title = title;
                Office = office;
                Loop = loop;
            }
        }

        public static GameFlowPhase Resolve(string sceneName, GameFlowPhase current,
            SceneNames names, bool isTransitioning)
        {
            if (isTransitioning) return current;
            if (sceneName == names.Title) return GameFlowPhase.Title;
            if (sceneName == names.Loop && current == GameFlowPhase.Opening)
                return GameFlowPhase.LoopGameplay;
            if (sceneName == names.Office && current == GameFlowPhase.LoopGameplay)
                return GameFlowPhase.Opening;
            return current;
        }
    }
}
