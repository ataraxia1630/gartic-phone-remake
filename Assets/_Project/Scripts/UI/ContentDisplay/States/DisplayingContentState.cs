using UnityEngine;

namespace InkEcho.UI.ContentDisplay.States
{
    /// <summary>
    /// Trạng thái hiển thị nội dung từ người chơi trước
    /// </summary>
    public class DisplayingContentState : UIContentState
    {
        public override string StateName => "Displaying";

        public override void OnEnter(UIContentController controller)
        {
            Debug.Log("[UIContent] Entering Displaying state");

            if (controller.CurrentRound > 0 && controller.CurrentChainIndex >= 0)
            {
                DisplayPreviousContent(controller);
            }
        }

        private void DisplayPreviousContent(UIContentController controller)
        {
            // TODO: Integrate với AlbumStore từ ServiceLocator nếu cần
            // Tạm thời hiển thị các panel cơ bản
            if (controller.ContentType == ContentDisplayType.Prompt)
            {
                DisplayPrompt(controller, "Prompt from previous round");
            }
            else if (controller.ContentType == ContentDisplayType.Drawing)
            {
                DisplayDrawing(controller, null);
            }
        }

        private void DisplayPrompt(UIContentController controller, string promptText)
        {
            if (controller.PromptPanel != null)
            {
                controller.PromptPanel.SetActive(true);
                var displayComponent = controller.PromptPanel.GetComponent<IPromptDisplayComponent>();
                if (displayComponent != null)
                {
                    displayComponent.SetPrompt(promptText);
                }
            }

            if (controller.DrawingPanel != null)
                controller.DrawingPanel.SetActive(false);
        }

        private void DisplayDrawing(UIContentController controller, object drawingData)
        {
            if (controller.DrawingPanel != null)
            {
                controller.DrawingPanel.SetActive(true);
                var displayComponent = controller.DrawingPanel.GetComponent<IDrawingDisplayComponent>();
                if (displayComponent != null)
                {
                    displayComponent.SetDrawing(drawingData);
                }
            }

            if (controller.PromptPanel != null)
                controller.PromptPanel.SetActive(false);
        }
    }
}
