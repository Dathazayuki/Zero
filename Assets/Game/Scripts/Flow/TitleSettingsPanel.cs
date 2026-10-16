using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class TitleSettingsPanel : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        private TitleMenuController title;

        private void Awake() => title = GetComponentInParent<TitleMenuController>(true);
        private void OnEnable()
        {
            if (backButton != null) backButton.onClick.AddListener(Back);
        }
        private void OnDisable()
        {
            if (backButton != null) backButton.onClick.RemoveListener(Back);
        }
        public void SelectInitial()
        {
            if (backButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        }
        private void Back() => title?.ShowMain();
    }
}
