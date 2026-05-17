using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawingManager : MonoBehaviour
{
    [Header("UI References")]
    public RawImage drawingCanvas;  // RawImage thay cho LineRenderer
    public RectTransform drawingAreaRect;
    public Image selectedColorDisplay; // ← Thêm dòng này

    [Header("Settings")]
    public int textureWidth = 800;
    public int textureHeight = 600;
    public float minDistance = 0.05f;  // Đơn vị pixel thay vì world unit

    // State
    private Texture2D texture;
    private Color[] clearColors;

    private Color currentColor = Color.black;
    private int brushSize = 1;  // Đơn vị pixel, dễ hiểu hơn
    private bool isEraser = false;

    // Undo/Redo lưu snapshot texture
    private Stack<Color[]> undoStack = new Stack<Color[]>();
    private Stack<Color[]> redoStack = new Stack<Color[]>();

    private Vector2? lastPos = null;
    private bool isDrawing = false;
    private bool isClearing = false;

    IEnumerator Start()
    {
        yield return null;
        // Lấy kích thước thực của RawImage
        int width = Mathf.RoundToInt(drawingCanvas.rectTransform.rect.width);
        int height = Mathf.RoundToInt(drawingCanvas.rectTransform.rect.height);

        Debug.Log($"Canvas size: {width}x{height}"); // Thêm dòng này

        if (width <= 0 || height <= 0)
        {
            Debug.LogError("DrawingCanvas size = 0! Kiểm tra RawImage chưa được setup đúng");
        }

        texture = new Texture2D(width, height);
        textureWidth = width;
        textureHeight = height;

        Debug.Log($"Texture size: {width}x{height}");

        // Tạo nền trắng
        clearColors = new Color[width * height];
        for (int i = 0; i < clearColors.Length; i++)
            clearColors[i] = Color.white;

        texture.SetPixels(clearColors);
        texture.Apply();
        drawingCanvas.texture = texture;
        UpdateColorDisplay();
    }

    void Update()
    {
        if (texture == null) return; // Thêm guard này

        // Chỉ xử lý vẽ khi chuột nằm trong DrawingArea
        if (!IsMouseOverDrawingArea()) return;

        if (Input.GetMouseButtonDown(0))
        {
            lastPos = null;
            isDrawing = false;
        }

        if (Input.GetMouseButton(0))
        {
            if (isClearing) return; 
            if (!isDrawing)
            {
                undoStack.Push(texture.GetPixels());
                redoStack.Clear();
                isDrawing = true;
            }
            DrawAtMouse();
        }

        if (Input.GetMouseButtonUp(0))
        {
            lastPos = null;
            isDrawing = false;
        }
    }

    bool IsMouseOverDrawingArea()
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            drawingCanvas.rectTransform,
            Input.mousePosition,
            null
        );
    }
    void DrawAtMouse()
    {
        // Convert mouse position sang local point của RawImage
        RectTransform rt = drawingCanvas.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt, Input.mousePosition, null, out Vector2 localPos))
            return;

        // Convert local point sang texture coords
        Vector2 texPos = new Vector2(
            (localPos.x / rt.rect.width + 0.5f) * textureWidth,
            (localPos.y / rt.rect.height + 0.5f) * textureHeight
        );

        // Kiểm tra có trong bounds không
        if (texPos.x < 0 || texPos.x >= textureWidth ||
            texPos.y < 0 || texPos.y >= textureHeight)
        {
            lastPos = null;
            return;
        }

        Color drawColor = isEraser ? Color.white : currentColor;

        // Vẽ line từ điểm trước đến điểm hiện tại (tránh đứt nét)
        if (lastPos.HasValue && Vector2.Distance(lastPos.Value, texPos) > minDistance)
        {
            DrawLine(lastPos.Value, texPos, drawColor);
        }
        else
        {
            DrawCircle((int)texPos.x, (int)texPos.y, drawColor);
        }

        lastPos = texPos;
        texture.Apply();
        drawingCanvas.texture = texture; // Cập nhật texture sau mỗi lần vẽ
    }

    void DrawCircle(int cx, int cy, Color color)
    {
        for (int x = -brushSize; x <= brushSize; x++)
        {
            for (int y = -brushSize; y <= brushSize; y++)
            {
                if (x * x + y * y <= brushSize * brushSize)
                {
                    int px = cx + x;
                    int py = cy + y;
                    if (px >= 0 && px < textureWidth && py >= 0 && py < textureHeight)
                        texture.SetPixel(px, py, color);
                }
            }
        }
    }

    void DrawLine(Vector2 from, Vector2 to, Color color)
    {
        float dist = Vector2.Distance(from, to);
        for (float t = 0; t <= 1; t += 1f / dist)
        {
            Vector2 p = Vector2.Lerp(from, to, t);
            DrawCircle((int)p.x, (int)p.y, color);
        }
    }

    // Public methods (giữ nguyên interface cũ)
    public void SetColor(Color color)
    {
        currentColor = color;
        isEraser = false;
        UpdateColorDisplay();
    }
    void UpdateColorDisplay()
    {
        if (selectedColorDisplay == null) return;

        if (isEraser)
            selectedColorDisplay.color = Color.white; // Eraser hiển thị trắng
        else
            selectedColorDisplay.color = currentColor;
    }

    public void SetBrushSize(float size)
    {
        brushSize = Mathf.RoundToInt(size);  // size từ slider 1-10
        Debug.Log($"BrushSize = {brushSize}px");
    }

    public void ToggleEraser()
    {
        isEraser = !isEraser;
        UpdateColorDisplay(); // ← Thêm dòng này
    }
    public void SetEraser(bool value)
    {
        isEraser = value;
        UpdateColorDisplay(); // ← Thêm dòng này
    }

    public void ClearAll()
    {
        if (texture == null) return;
        isClearing = true;
        undoStack.Push(texture.GetPixels()); // Lưu trước khi clear để có thể Undo
        redoStack.Clear();
        texture.SetPixels(clearColors);
        texture.Apply();
        drawingCanvas.texture = texture;
        isClearing = false;
    }

    public void Undo()
    {
        isDrawing = true; // Ngăn Update() lưu snapshot mới
        Debug.Log($"Undo called - undoStack count: {undoStack.Count}");
        if (undoStack.Count == 0)
        {
            isDrawing = false;
            return;
        }
        redoStack.Push(texture.GetPixels());
        texture.SetPixels(undoStack.Pop());
        texture.Apply();
        drawingCanvas.texture = texture;
        isDrawing = false;
        Debug.Log("Undo done");
    }

    public void Redo()
    {
        isDrawing = true;
        if (redoStack.Count == 0)
        {
            isDrawing = false;
            return;
        }
        undoStack.Push(texture.GetPixels());
        texture.SetPixels(redoStack.Pop());
        texture.Apply();
        drawingCanvas.texture = texture;
        isDrawing = false;
    }
}