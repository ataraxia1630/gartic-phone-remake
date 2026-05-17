using UnityEngine;
using UnityEngine.UI;

public class ColorButton : MonoBehaviour
{
    public DrawingManager drawingManager;
    public Color color;

    void Start()
    {
        // Tự động lấy màu từ Image của button
        color = GetComponent<Image>().color;

        GetComponent<Button>().onClick.AddListener(() =>
        {
            drawingManager.SetColor(color);
        });
    }
}
