using System;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class WatchfulHallChargeRules
    {
        private readonly float[] progress = new float[4];
        private readonly float chargeSeconds;
        private readonly float settleSeconds;
        private int activePad = -1;
        private float settled;
        public bool IsUnlocked { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool IsCharging => activePad >= 0 && settled >= settleSeconds && !IsCharged(activePad);

        public WatchfulHallChargeRules(float chargeSeconds = 15f, float settleSeconds = 1.5f)
        {
            this.chargeSeconds = Math.Max(0.1f, chargeSeconds);
            this.settleSeconds = Math.Max(0f, settleSeconds);
        }

        public float Progress(int pad) => progress[pad];
        public bool IsCharged(int pad) => progress[pad] >= chargeSeconds;
        public bool IsChargingPad(int pad) => activePad == pad && IsCharging;

        public void Tick(int pad, bool stationary, float deltaTime)
        {
            if (!(deltaTime > 0f) || float.IsInfinity(deltaTime)) return;
            if (pad < 0 || pad >= progress.Length || !stationary)
            {
                Pause();
                return;
            }
            if (activePad != pad) { activePad = pad; settled = 0f; }
            if (IsCharged(pad)) return;
            float settlingStep = Math.Min(deltaTime, Math.Max(0f, settleSeconds - settled));
            settled += settlingStep;
            progress[pad] = Math.Min(chargeSeconds, progress[pad] + deltaTime - settlingStep);
            IsUnlocked = IsCharged(0) && IsCharged(1) && IsCharged(2) && IsCharged(3);
        }

        public void Pause()
        {
            activePad = -1;
            settled = 0f;
        }

        public bool Complete()
        {
            if (!IsUnlocked || IsCompleted) return false;
            IsCompleted = true;
            return true;
        }

        public void Reset()
        {
            Array.Clear(progress, 0, progress.Length);
            IsUnlocked = false;
            IsCompleted = false;
            Pause();
        }
    }
}
