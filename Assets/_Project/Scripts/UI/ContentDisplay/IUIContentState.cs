namespace InkEcho.UI.ContentDisplay
{
    /// <summary>
    /// Định nghĩa interface cho UI Content States
    /// Quản lý các trạng thái hiển thị/ẩn nội dung theo lượt
    /// </summary>
    public interface IUIContentState
    {
        string StateName { get; }

        void OnEnter(UIContentController controller);
        void OnExit(UIContentController controller);
        void Tick(UIContentController controller);
    }
}
