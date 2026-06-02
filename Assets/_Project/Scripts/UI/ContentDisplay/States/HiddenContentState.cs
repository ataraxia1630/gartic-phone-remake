using UnityEngine;

namespace InkEcho.UI.ContentDisplay.States
{
    /// <summary>
    /// Trạng thái ẩn nội dung
    /// </summary>
    public class HiddenContentState : UIContentState
    {
        public override string StateName => "Hidden";

        public override void OnEnter(UIContentController controller)
        {
            Debug.Log("[UIContent] Entering Hidden state");

            if (controller.PromptPanel != null)
                controller.PromptPanel.SetActive(false);

            if (controller.DrawingPanel != null)
                controller.DrawingPanel.SetActive(false);
        }
    }
}
