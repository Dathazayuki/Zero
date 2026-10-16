namespace Zero.AnomalyPrototype.Flow
{
    public readonly struct SoundHandle
    {
        internal int Slot { get; }
        internal uint Generation { get; }
        internal SoundHandle(int slot, uint generation) { Slot = slot; Generation = generation; }
        public bool IsValid => Generation != 0;
    }
}
