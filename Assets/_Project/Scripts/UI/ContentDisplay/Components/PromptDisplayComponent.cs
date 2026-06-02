using UnityEngine;

namespace InkEcho.UI.ContentDisplay.Components
{
    /// <summary>
    /// Implementation cơ bản cho hiển thị Prompt
    /// Sử dụng TextMesh Pro (nếu có) hoặc UI.Text làm fallback
    /// </summary>
    public class PromptDisplayComponent : MonoBehaviour, IPromptDisplayComponent
    {
        [SerializeField] private UnityEngine.UI.Text promptLabel;

        public void SetPrompt(string promptText)
        {
            if (promptLabel != null)
                promptLabel.text = promptText;
        }

        public void Clear()
        {
            if (promptLabel != null)
                promptLabel.text = string.Empty;
        }
    }
}
