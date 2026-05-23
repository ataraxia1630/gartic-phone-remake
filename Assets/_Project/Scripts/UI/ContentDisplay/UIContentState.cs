namespace InkEcho.UI.ContentDisplay
{
    /// <summary>
    /// Base class cho UI Content States
    /// </summary>
    public abstract class UIContentState : IUIContentState
    {
        public abstract string StateName { get; }

        public virtual void OnEnter(UIContentController controller) { }
        public virtual void OnExit(UIContentController controller) { }
        public virtual void Tick(UIContentController controller) { }
    }
}
