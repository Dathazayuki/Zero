using UnityEngine;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class DarumaTimerDisplay : MonoBehaviour
    {
        [SerializeField] private DarumaSession source;
        private static readonly int[] Masks = { 63, 6, 91, 79, 102, 109, 125, 7, 127, 111 };
        private readonly Renderer[] segments = new Renderer[28];
        private int displayedSeconds = -1;
        public int DisplayedSeconds => displayedSeconds;

        private void Awake()
        {
            for (int digit = 0; digit < 4; digit++)
            {
                for (int segment = 0; segment < 7; segment++)
                {
                    Transform child = transform.Find($"DH_Digit{digit}_{(char)('a' + segment)}");
                    if (child != null) segments[digit * 7 + segment] = child.GetComponent<Renderer>();
                }
            }
        }

        private void LateUpdate()
        {
            if (source == null || !source.IsReady) return;
            Show(source.Remaining);
        }

        public void Show(float remaining)
        {
            int seconds = Mathf.Clamp(Mathf.CeilToInt(remaining), 0, 5999);
            if (displayedSeconds == seconds) return;
            displayedSeconds = seconds;
            SetDigit(0, seconds / 60 / 10);
            SetDigit(1, seconds / 60 % 10);
            SetDigit(2, seconds % 60 / 10);
            SetDigit(3, seconds % 10);
        }

        private void SetDigit(int digit, int number)
        {
            for (int segment = 0; segment < 7; segment++)
            {
                Renderer bar = segments[digit * 7 + segment];
                if (bar != null) bar.enabled = (Masks[number] & (1 << segment)) != 0;
            }
        }
    }
}
