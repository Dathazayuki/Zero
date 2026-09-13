using UnityEngine;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Flow
{
    [DisallowMultipleComponent]
    public sealed class OfficeComputerDisplay : MonoBehaviour
    {
        [SerializeField] private Text displayText;
        [SerializeField] private Image background;

        public void Configure(Text text, Image backgroundImage)
        {
            displayText = text;
            background = backgroundImage;
        }

        public void ShowWorking()
        {
            Apply(
                "MONTHLY REPORT - NIGHT SHIFT\n\nValidating records...\n██████████████████░░  91%\n\n23:57",
                new Color(0.035f, 0.09f, 0.12f, 1f));
        }

        public void ShowCompleted()
        {
            Apply(
                "MONTHLY REPORT - NIGHT SHIFT\n\nSAVE COMPLETE\n████████████████████  100%\n\nYou may leave now.",
                new Color(0.025f, 0.12f, 0.085f, 1f));
        }

        public void ShowUnfinished()
        {
            Apply(
                "MONTHLY REPORT - NIGHT SHIFT\n\nRECOVERED DRAFT\n████░░░░░░░░░░░░░░░░  18%\n\nLast saved: 23:57",
                new Color(0.09f, 0.055f, 0.045f, 1f));
        }

        public void ShowLoopRestarting()
        {
            Apply(
                "MONTHLY REPORT - NIGHT SHIFT\n\nRECOVERED DRAFT\n████░░░░░░░░░░░░░░░░  18%\n\nRow 001: Beginning validation...",
                new Color(0.09f, 0.04f, 0.04f, 1f));
        }

        private void Apply(string content, Color color)
        {
            if (displayText != null)
            {
                displayText.text = content;
            }

            if (background != null)
            {
                background.color = color;
            }
        }
    }
}
