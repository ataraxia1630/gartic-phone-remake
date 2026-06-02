using UnityEngine;

namespace InkEcho.UI.ContentDisplay.States
{
    /// <summary>
    /// State với animation scale-in (zoom)
    /// </summary>
    public class ScaleInContentState : UIContentState
    {
        private float _scaleProgress = 0f;
        private float _scaleDuration = 0.5f;

        public override string StateName => "ScaleInContent";

        public override void OnEnter(UIContentController controller)
        {
            Debug.Log("[UIContent] Entering ScaleIn state");

            _scaleProgress = 0f;
            _scaleDuration = controller.RevealDuration;

            var panel = controller.CurrentRound == 0 ? controller.PromptPanel : controller.DrawingPanel;

            if (panel != null)
            {
                var rectTransform = panel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.localScale = Vector3.zero; // Bắt đầu từ 0
                }

                panel.SetActive(true);

                var canvasGroup = panel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = panel.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 1f;
            }
        }

        public override void Tick(UIContentController controller)
        {
            _scaleProgress += Time.deltaTime;
            float normalizedProgress = Mathf.Clamp01(_scaleProgress / _scaleDuration);

            var panel = controller.CurrentRound == 0 ? controller.PromptPanel : controller.DrawingPanel;

            if (panel != null)
            {
                var rectTransform = panel.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Easing: Ease-out back (bounce effect)
                    float eased = EaseOutBack(normalizedProgress);
                    rectTransform.localScale = Vector3.one * eased;
                }
            }

            if (normalizedProgress >= 1f)
            {
                controller.TransitionToState(new DisplayingContentState());
            }
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
