namespace InkEcho.UI.ContentDisplay
{
    /// <summary>
    /// Interface cho component hiển thị Prompt
    /// </summary>
    public interface IPromptDisplayComponent
    {
        void SetPrompt(string promptText);
        void Clear();
    }
}
