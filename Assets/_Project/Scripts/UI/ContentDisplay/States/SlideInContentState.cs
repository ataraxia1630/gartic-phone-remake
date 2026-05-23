using UnityEngine;

namespace InkEcho.UI.ContentDisplay.States
{
    /// <summary>
    /// State với animation slide-in từ dưới
    /// </summary>
    public class SlideInContentState : UIContentState
    {
        private float _slideProgress = 0f;
        private float _slideDuration = 0.5f;
        private Vector2 _startPosition;
        private Vector2 _endPosition;

        public override string StateName => "SlideInContent";

        public override void OnEnter(UIContentController controller)
        {
            Debug.Log("[UIContent] Entering SlideIn state");

            _slideProgress = 0f;
            _slideDuration = controller.RevealDuration;

            var panel = controller.CurrentRound == 0 ? controller.PromptPanel : controller.DrawingPanel;

            if (panel != null)
            {
                var rectTransform = panel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    _endPosition = rectTransform.anchoredPosition;
                    _startPosition = _endPosition + Vector2.down * 200f; // Bắt đầu từ dưới
                    rectTransform.anchoredPosition = _startPosition;
                }

                panel.SetActive(true);

                // Giữ alpha = 1 ngay từ đầu
                var canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = panel.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 1f;
            }
        }

        public override void Tick(UIContentController controller)
        {
            _slideProgress += Time.deltaTime;
            float normalizedProgress = Mathf.Clamp01(_slideProgress / _slideDuration);

            var panel = controller.CurrentRound == 0 ? controller.PromptPanel : controller.DrawingPanel;

            if (panel != null)
            {
                var rectTransform = panel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Easing: Ease-out
                    float eased = Mathf.SmoothStep(0f, 1f, normalizedProgress);
                    rectTransform.anchoredPosition = Vector2.Lerp(_startPosition, _endPosition, eased);
                }
            }

            if (normalizedProgress >= 1f)
            {
                controller.TransitionToState(new DisplayingContentState());
            }
        }
    }
}
