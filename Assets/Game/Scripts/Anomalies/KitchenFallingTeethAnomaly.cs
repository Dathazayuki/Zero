using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class KitchenFallingTeethAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Rigidbody[] teeth = Array.Empty<Rigidbody>();
        private Vector3[] positions;
        private Quaternion[] rotations;
        private bool[] active, gravity, kinematic;
        public bool IsConfigured
        {
            get
            {
                if (teeth == null || teeth.Length == 0) return false;
                for (int i = 0; i < teeth.Length; i++) if (teeth[i] == null) return false;
                return true;
            }
        }

        public override void CaptureNormalState()
        {
            if (positions != null || !IsConfigured) return;
            positions = new Vector3[teeth.Length]; rotations = new Quaternion[teeth.Length];
            active = new bool[teeth.Length]; gravity = new bool[teeth.Length]; kinematic = new bool[teeth.Length];
            for (int i = 0; i < teeth.Length; i++)
            {
                positions[i] = teeth[i].transform.localPosition;
                rotations[i] = teeth[i].transform.localRotation;
                active[i] = teeth[i].gameObject.activeSelf;
                gravity[i] = teeth[i].useGravity; kinematic[i] = teeth[i].isKinematic;
            }
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (positions == null || !IsConfigured) return;
            for (int i = 0; i < teeth.Length; i++)
            {
                Rigidbody body = teeth[i];
                body.gameObject.SetActive(true);
                body.isKinematic = false; body.useGravity = true;
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
        }

        protected override void OnResetAnomaly()
        {
            if (positions == null) return;
            for (int i = 0; i < teeth.Length; i++)
            {
                Rigidbody body = teeth[i];
                if (body == null) continue;
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.isKinematic = true;
                body.transform.SetLocalPositionAndRotation(positions[i], rotations[i]);
                body.position = body.transform.position; body.rotation = body.transform.rotation;
                body.useGravity = gravity[i]; body.isKinematic = kinematic[i];
                body.Sleep(); body.gameObject.SetActive(active[i]);
            }
        }

        private void OnDisable() => ResetAnomaly();
    }
}
