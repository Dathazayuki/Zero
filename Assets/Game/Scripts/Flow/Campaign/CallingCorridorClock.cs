using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class CallingCorridorClock : MonoBehaviour
    {
        [SerializeField] private Transform hourPivot;
        [SerializeField] private Transform minutePivot;
        [SerializeField] private GameObject dial;
        [SerializeField] private GameObject offlineCover;
        public bool IsConfigured => hourPivot != null && minutePivot != null && hourPivot != minutePivot &&
            dial != null && offlineCover != null;
        public int HourDigit { get; private set; } = -1;
        public int MinuteDigit { get; private set; } = -1;

        public void SetHands(int hour, int minute)
        {
            if (!IsConfigured) return;
            HourDigit = hour;
            MinuteDigit = minute;
            bool active = hour > 0 && minute > 0;
            dial.SetActive(active);
            offlineCover.SetActive(!active);
            hourPivot.localRotation = Quaternion.Euler(0f, 0f, active ? -30f * hour : 0f);
            minutePivot.localRotation = Quaternion.Euler(0f, 0f, active ? -30f * minute : 0f);
        }
    }
}
