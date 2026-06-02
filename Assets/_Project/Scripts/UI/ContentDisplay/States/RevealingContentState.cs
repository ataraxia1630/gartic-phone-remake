using UnityEngine;

namespace InkEcho.UI.ContentDisplay.States
{
    /// <summary>
    /// Trạng thái reveal dần nội dung (animate fade-in, etc)
    /// </summary>
    public class RevealingContentState : UIContentState
    {
        private float _revealProgress = 0f;
        private float _revealDuration = 1f;

        public override string StateName => "Revealing";

        public override void OnEnter(UIContentController controller)
        {
            Debug.Log("[UIContent] Entering Revealing state");

            _revealProgress = 0f;
            _revealDuration = controller.RevealDuration;

            // Hiển thị panel nhưng trong trạng thái alpha=0
            ShowPanelsWithAlpha(controller, 0f);
        }

        public override void Tick(UIContentController controller)
        {
            _revealProgress += Time.deltaTime;
            float normalizedProgress = Mathf.Clamp01(_revealProgress / _revealDuration);

            ShowPanelsWithAlpha(controller, normalizedProgress);

            // Khi reveal xong, chuyển sang Displaying state
            if (normalizedProgress >= 1f)
            {
                controller.TransitionToState(new DisplayingContentState());
            }
        }

        private void ShowPanelsWithAlpha(UIContentController controller, float alpha)
        {
            var panel = controller.CurrentRound == 0 ? controller.PromptPanel : controller.DrawingPanel;

            if (panel != null)
            {
                var canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = panel.AddComponent<CanvasGroup>();

                canvasGroup.alpha = alpha;
                panel.SetActive(true);
            }
        }
    }
}
