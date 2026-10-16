using UnityEngine;

namespace Zero.AnomalyPrototype.Player
{
    public static class DeathCameraNoise
    {
        public static Vector3 Sample(float time, float seedOffset, float amplitude)
        {
            return new Vector3(Mathf.PerlinNoise(time, seedOffset + 0.17f) - 0.5f,
                Mathf.PerlinNoise(seedOffset + 0.43f, time) - 0.5f,
                Mathf.PerlinNoise(time, seedOffset + 0.79f) - 0.5f) * amplitude;
        }
    }
}
