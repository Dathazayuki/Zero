using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class MinigameContext
    {
        public FirstPersonController Player { get; }
        public SceneFadeController Fade { get; }
        public Transform Entrance { get; }
        public MinigameContext(FirstPersonController player, SceneFadeController fade, Transform entrance)
        {
            Player = player;
            Fade = fade;
            Entrance = entrance;
        }
    }
}
