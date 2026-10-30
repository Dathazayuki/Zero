using UnityEngine;
using Zero.AnomalyPrototype.Interaction;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class CabaretRitualDoor : MonoBehaviour
    {
        [SerializeField] private Transform[] leaves;
        [SerializeField] private BoxCollider blocker;
        [SerializeField] private PushDoorController automaticDoor;
        [SerializeField, Min(.05f)] private float slamSeconds = .22f;
        [SerializeField, Min(.1f)] private float openSeconds = .6f;
        private Quaternion[] openRotations;
        private bool closed, captured;
        private bool automaticDoorWasEnabled;
        public bool IsConfigured => blocker != null && HasLeaves();
        public bool IsLocked => captured && closed;

        private bool HasLeaves()
        {
            if (leaves == null || leaves.Length == 0) return false;
            for (int i = 0; i < leaves.Length; i++)
                if (leaves[i] == null) return false;
            return true;
        }

        public void Capture()
        {
            if (captured || !IsConfigured) return;
            captured = true;
            openRotations = new Quaternion[leaves.Length];
            for (int i = 0; i < leaves.Length; i++) openRotations[i] = leaves[i].localRotation;
            if (automaticDoor != null)
                openRotations[0] = Quaternion.Euler(0f, automaticDoor.OpenAngle, 0f);
            automaticDoorWasEnabled = automaticDoor != null && automaticDoor.enabled;
        }

        public void SetClosed(bool value)
        {
            Capture();
            if (!captured) return;
            closed = value;
            gameObject.SetActive(true);
            blocker.enabled = value;
            SetAutomaticDoorEnabled(!value && automaticDoorWasEnabled);
        }

        private void Update()
        {
            if (!captured || Time.deltaTime <= 0f) return;
            float speed = 100f / (closed ? slamSeconds : openSeconds);
            for (int i = 0; i < leaves.Length; i++)
                leaves[i].localRotation = Quaternion.RotateTowards(leaves[i].localRotation,
                    closed ? Quaternion.identity : openRotations[i], speed * Time.deltaTime);
        }

        public void Restore()
        {
            RestorePresentation();
            gameObject.SetActive(false);
        }

        private void RestorePresentation()
        {
            closed = false;
            if (blocker != null) blocker.enabled = false;
            if (!captured) return;
            SetAutomaticDoorEnabled(automaticDoorWasEnabled);
            for (int i = 0; i < leaves.Length; i++)
                if (leaves[i] != null) leaves[i].localRotation = openRotations[i];
        }

        private void SetAutomaticDoorEnabled(bool value)
        {
            if (automaticDoor == null || automaticDoor.enabled == value) return;
            Quaternion pose = leaves[0].localRotation;
            automaticDoor.enabled = value;
            leaves[0].localRotation = pose;
        }

        private void OnDisable() => RestorePresentation();
    }
}
