using System;
using UnityEngine;

namespace Zero.AnomalyPrototype.Anomalies
{
    public sealed class ArchiveFallingFilesAnomaly : AnomalyBehaviour
    {
        [SerializeField] private Transform[] files = Array.Empty<Transform>();
        [SerializeField] private Rigidbody[] bodies = Array.Empty<Rigidbody>();
        [SerializeField, Min(0f)] private float pushForce = 0.8f;
        [SerializeField] private Vector3 pushDirection = Vector3.zero;
        private Vector3[] normalPositions;
        private Quaternion[] normalRotations;
        private bool[] normalKinematic;
        private bool[] normalGravity;
        public bool IsConfigured => files != null && files.Length > 0 && bodies != null &&
            files.Length == bodies.Length && ReferencesValid();
        public float PushForce => pushForce;
        public Vector3 PushDirection => pushDirection;

        private bool ReferencesValid()
        {
            for (int i = 0; i < files.Length; i++)
                if (files[i] == null || bodies[i] == null || bodies[i].transform != files[i]) return false;
            return true;
        }

        public void Configure(Transform[] targets, Rigidbody[] physicsBodies, float force = 0.8f, Vector3 direction = default)
        {
            files = targets;
            bodies = physicsBodies;
            pushForce = force;
            pushDirection = direction;
        }

        public override void CaptureNormalState()
        {
            if (normalPositions != null || !IsConfigured) return;
            normalPositions = new Vector3[files.Length];
            normalRotations = new Quaternion[files.Length];
            normalKinematic = new bool[files.Length];
            normalGravity = new bool[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                normalPositions[i] = files[i].localPosition;
                normalRotations[i] = files[i].localRotation;
                normalKinematic[i] = bodies[i].isKinematic;
                normalGravity[i] = bodies[i].useGravity;
            }
        }

        protected override void OnActivate()
        {
            CaptureNormalState();
            if (normalPositions == null || !IsConfigured) return;
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                body.isKinematic = false;
                body.useGravity = true;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();

                Vector3 dir = pushDirection;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f && files[i] != null)
                {
                    dir = files[i].right;
                    dir.y = 0f;
                    if (dir.sqrMagnitude < 0.001f)
                    {
                        dir = files[i].forward;
                        dir.y = 0f;
                    }
                }

                if (dir.sqrMagnitude > 0.001f && pushForce > 0f)
                {
                    dir.Normalize();
                    body.AddForce(dir * pushForce, ForceMode.Impulse);
                }
            }
        }

        protected override void OnResetAnomaly()
        {
            if (normalPositions == null) return;
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i] == null || bodies[i] == null) continue;
                Restore(i);
            }
        }

        private void Restore(int index)
        {
            Rigidbody body = bodies[index];
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            files[index].localPosition = normalPositions[index];
            files[index].localRotation = normalRotations[index];
            body.position = files[index].position;
            body.rotation = files[index].rotation;
            body.useGravity = normalGravity[index];
            body.isKinematic = normalKinematic[index];
            body.Sleep();
        }

        private void OnDisable() => ResetAnomaly();
    }
}
