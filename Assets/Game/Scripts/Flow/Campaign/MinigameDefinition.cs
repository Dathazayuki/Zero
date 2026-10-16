using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    [CreateAssetMenu(menuName = "Zero/Campaign/Minigame")]
    public sealed class MinigameDefinition : ScriptableObject
    {
        [SerializeField] private string minigameId;
        [SerializeField] private string sceneName;
        [SerializeField] private string designTheme;
        [SerializeField] private bool contentReady;
        public string MinigameId => minigameId;
        public string SceneName => sceneName;
        public string DesignTheme => designTheme;
        public bool IsReady => contentReady && !string.IsNullOrWhiteSpace(sceneName);
    }
}
