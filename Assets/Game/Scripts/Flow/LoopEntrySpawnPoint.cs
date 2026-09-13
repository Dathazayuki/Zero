using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class LoopEntrySpawnPoint : MonoBehaviour
    {
        [SerializeField] private Transform spawnTransform;

        public Transform SpawnTransform => spawnTransform != null ? spawnTransform : transform;
    }
}
