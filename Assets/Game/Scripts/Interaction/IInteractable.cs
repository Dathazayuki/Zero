namespace Zero.AnomalyPrototype.Interaction
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact(Player.PlayerInteraction interactor);
    }
}
