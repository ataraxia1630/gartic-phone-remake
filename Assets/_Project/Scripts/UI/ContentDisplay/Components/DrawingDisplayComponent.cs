using UnityEngine;
using UnityEngine.UI;

namespace InkEcho.UI.ContentDisplay.Components
{
    /// <summary>
    /// Implementation cơ bản cho hiển thị Drawing
    /// </summary>
    public class DrawingDisplayComponent : MonoBehaviour, IDrawingDisplayComponent
    {
        [SerializeField] private Image drawingImage;
        [SerializeField] private Text drawingInfoText;

        public void SetDrawing(object drawingData)
        {
            if (drawingData == null) return;

            // TODO: Implement rendering drawing từ drawingData
            // Có thể cast drawingData thành AlbumEntry nếu cần
            // var entry = (AlbumEntry)drawingData;
            // if (drawingInfoText != null)
            // {
            //     drawingInfoText.text = $"Hash: {entry.DrawingHash:X}\nStrokes: {entry.DrawingStrokes}";
            // }
        }

        public void Clear()
        {
            if (drawingImage != null)
                drawingImage.sprite = null;

            if (drawingInfoText != null)
                drawingInfoText.text = string.Empty;
        }
    }
}
