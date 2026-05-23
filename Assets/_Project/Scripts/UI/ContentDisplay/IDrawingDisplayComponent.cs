namespace InkEcho.UI.ContentDisplay
{
    /// <summary>
    /// Interface cho component hiển thị Drawing
    /// Dùng generic object để tránh phụ thuộc vào AlbumEntry
    /// </summary>
    public interface IDrawingDisplayComponent
    {
        void SetDrawing(object drawingData);
        void Clear();
    }
}
