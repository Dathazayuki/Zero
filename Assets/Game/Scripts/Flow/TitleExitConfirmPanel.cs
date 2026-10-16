using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Zero.AnomalyPrototype.Flow
{
    public sealed class TitleExitConfirmPanel : MonoBehaviour
    {
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        private TitleMenuController title;

        private void Awake() => title = GetComponentInParent<TitleMenuController>(true);
        private void OnEnable()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.AddListener(Cancel);
        }
        private void OnDisable()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(Cancel);
        }
        public void SelectInitial()
        {
            if (cancelButton != null && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(cancelButton.gameObject);
        }
        private void Confirm() => GameFlowController.Instance?.ExitApplication();
        private void Cancel() => title?.ShowMain();
    }
}
