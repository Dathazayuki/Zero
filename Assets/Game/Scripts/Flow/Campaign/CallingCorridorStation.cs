using TMPro;
using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class CallingCorridorStation : MonoBehaviour
    {
        [SerializeField] private Transform checkpoint;
        [SerializeField] private TMP_Text board;
        [SerializeField] private Transform threatSpawn;
        [SerializeField] private Transform threatExit;
        [SerializeField] private GameObject warningShadow;
        [SerializeField] private Light warningLight;
        private float originalIntensity;
        private BoxCollider readingBoundary;
        private CallingCorridorSession session;
        private int round;
        public Transform Checkpoint => checkpoint;
        public Transform ThreatSpawn => threatSpawn;
        public Transform ThreatExit => threatExit;
        public bool IsConfigured => checkpoint != null && board != null && threatSpawn != null && threatExit != null;

        private void Awake()
        {
            TryGetComponent(out readingBoundary);
            if (warningLight != null) originalIntensity = warningLight.intensity;
            SetWarning(false);
        }

        public void Bind(CallingCorridorSession owner, int index)
        {
            session = owner;
            round = index;
        }

        public void Show(bool visible)
        {
            if (board == null) return;
            int found = session != null ? session.Decision : 0;
            board.text = "CATCH THE RABBIT\n" + found + " / 7";
            if (found < 7)
                board.text += "\n" + CallingCorridorRules.CodePrefix(found) + " - [B]\n" +
                    CallingCorridorRules.ClockSymbols(found);
            else
                board.text += "\nARCHIVE END";
        }

        public void SetWarning(bool active)
        {
            if (warningShadow != null) warningShadow.SetActive(active);
            if (warningLight != null) warningLight.intensity = active ? originalIntensity * 0.15f : originalIntensity;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (session == null || !session.IsPlaying || session.Round != round) return;
            if (session.IsPlayer(other)) Show(session.Decision == CallingCorridorRules.RoundStart(round));
        }

        private void OnTriggerExit(Collider other)
        {
            if (session == null || !session.IsPlaying || session.Round != round) return;
            if (!session.IsPlayer(other) || readingBoundary == null) return;
            Vector3 center = readingBoundary.transform.TransformPoint(readingBoundary.center);
            if (Vector3.Dot(other.transform.position - center, checkpoint.forward) > 0f) Show(false);
        }

        private void OnDisable() => SetWarning(false);
    }
}
