using System.Collections;
using UnityEngine;
using Zero.AnomalyPrototype.Interaction;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CallingCorridorSafeAlcove : MonoBehaviour
    {
        private BoxCollider volume;
        private PushDoorController door;
        private Coroutine closeRoutine;

        private void Awake()
        {
            volume = GetComponent<BoxCollider>();
            door = GetComponentInChildren<PushDoorController>(true);
        }
        public bool Contains(Vector3 feet)
        {
            if (!isActiveAndEnabled || volume == null) return false;
            Vector3 local = transform.InverseTransformPoint(feet + Vector3.up * 0.5f) - volume.center;
            Vector3 half = volume.size * 0.5f;
            return Mathf.Abs(local.x) <= half.x && Mathf.Abs(local.y) <= half.y && Mathf.Abs(local.z) <= half.z;
        }

        public void ResetDoor()
        {
            if (closeRoutine != null) StopCoroutine(closeRoutine);
            closeRoutine = null;
            door?.OpenImmediately();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (door == null || other.GetComponentInParent<FirstPersonController>() == null) return;
            if (closeRoutine != null) StopCoroutine(closeRoutine);
            closeRoutine = StartCoroutine(CloseBehindPlayer());
        }

        private IEnumerator CloseBehindPlayer()
        {
            yield return new WaitForSeconds(0.35f);
            if (door != null && door.IsOpen) door.ResetDoor();
            closeRoutine = null;
        }

        private void OnDisable()
        {
            if (closeRoutine != null) StopCoroutine(closeRoutine);
            closeRoutine = null;
            door?.ResetDoor();
        }
    }
}
