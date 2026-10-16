using System;
using System.Collections;
using UnityEngine;
using Zero.AnomalyPrototype.Player;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class WatchfulHallSession : MonoBehaviour, IMinigameSession
    {
        [SerializeField] private FirstPersonController player;
        [SerializeField] private CharacterController playerBody;
        [SerializeField] private WatchfulHallChargePad[] pads;
        [SerializeField] private WatchfulHallExitGate exitGate;
        [SerializeField] private WatchfulHallThreatDirector threats;
        [SerializeField, Min(0.1f)] private float chargeSeconds = 15f;
        [SerializeField, Range(1f, 2f)] private float settleSeconds = 1.5f;
        [SerializeField, Min(0.001f)] private float movementTolerance = 0.03f;
        private WatchfulHallChargeRules rules;
        private Vector3 anchor;
        private Vector3 previousPosition;
        public bool IsReady { get; private set; }
        public bool IsUnlocked => rules != null && rules.IsUnlocked;
        public bool IsCharging => rules != null && rules.IsCharging;
        public bool IsCompleted => rules != null && rules.IsCompleted;
        public event Action Completed;
        public float GetProgress(int pad) => rules != null ? rules.Progress(pad) : 0f;

        private void Start()
        {
            IsReady = player != null && playerBody != null && playerBody.transform == player.transform
                && exitGate != null && exitGate.IsReady && pads != null && pads.Length == 4;
            if (IsReady) IsReady = PadsReady();
            if (!IsReady) { Debug.LogError("WATCHFUL_HALL_CONFIGURATION_INVALID: objective references", this); return; }
            rules = new WatchfulHallChargeRules(chargeSeconds, settleSeconds);
            if (threats != null) threats.Bind(this, player);
            ResetAttempt();
        }

        private bool PadsReady()
        {
            for (int i = 0; i < pads.Length; i++)
                if (pads[i] == null || !pads[i].IsReady) return false;
            return true;
        }

        public IEnumerator Prepare() { yield break; }

        public void Begin(MinigameContext context)
        {
            if (!IsReady || context == null || context.Player != player) return;
            ResetAttempt();
        }

        private void LateUpdate()
        {
            if (!IsReady || rules.IsCompleted || Time.deltaTime <= 0f) return;
            if (threats != null && threats.isActiveAndEnabled && threats.IsReady && !threats.IsPlaying) return;
            Vector3 position = player.transform.position;
            bool stationary = IsStationary(position);
            Vector3 feet = playerBody.bounds.center - Vector3.up * playerBody.bounds.extents.y;
            int pad = OccupiedPad(feet);
            rules.Tick(pad, stationary && playerBody.enabled && playerBody.isGrounded, Time.deltaTime);
            Present();
            CheckCompletion(position);
        }

        private void CheckCompletion(Vector3 position)
        {
            if (exitGate.HasPassed(position) && rules.Complete())
            {
                if (threats != null) threats.StopActors();
                Completed?.Invoke();
            }
        }

        private bool IsStationary(Vector3 position)
        {
            Vector3 drift = Vector3.ProjectOnPlane(position - anchor, Vector3.up);
            Vector3 movement = Vector3.ProjectOnPlane(position - previousPosition, Vector3.up);
            previousPosition = position;
            bool still = !player.IsMoving && drift.sqrMagnitude <= movementTolerance * movementTolerance
                && movement.sqrMagnitude <= 0.000004f;
            if (!still) anchor = position;
            return still;
        }

        private int OccupiedPad(Vector3 feet)
        {
            for (int i = 0; i < pads.Length; i++)
                if (pads[i].ContainsFeet(feet)) return i;
            return -1;
        }

        private void Present()
        {
            for (int i = 0; i < pads.Length; i++)
                pads[i].Present(rules.Progress(i), chargeSeconds, rules.IsCharged(i), rules.IsChargingPad(i));
            exitGate.Present(rules);
        }

        public void ResetAttempt()
        {
            if (!IsReady || rules == null) return;
            rules.Reset();
            anchor = previousPosition = player.transform.position;
            exitGate.ResetPresentation();
            Present();
            if (threats != null) threats.ResetThreats();
        }

        public void PauseCharging()
        {
            rules?.Pause();
            if (IsReady) Present();
        }

        private void OnDisable()
        {
            rules?.Pause();
            if (!IsReady) return;
            if (threats != null) threats.Suspend();
            exitGate.ResetPresentation();
            for (int i = 0; i < pads.Length; i++) pads[i].Present(0f, chargeSeconds, false);
        }

        private void OnEnable()
        {
            if (player != null) anchor = previousPosition = player.transform.position;
            if (IsReady && rules != null) Present();
        }
    }
}
