using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using Fusion.Sockets;
using InkEcho.Gameplay.Data;
using InkEcho.Network.Core;
using InkEcho.Network.Phases;
using InkEcho.Network.Data;
using InkEcho.Network.Players;

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

    private static readonly ReliableKey DrawingTextureKey = ReliableKey.FromInts(0xDA, 0, 0, 0);
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

    private readonly List<Vector3> _currentStrokeUV = new List<Vector3>();
    private bool _wasInDrawPhase = false;
    private byte _cachedChainLink;
    private byte _cachedOriginSlot;
    private bool _hasCachedAssignment;

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
        if (texture == null) return;

        // Detect Draw phase transition to flush any in-progress stroke
        var pm = ServiceLocator.Get<PhaseManager>();
        bool nowInDraw = pm != null && pm.CurrentPhase == PhaseType.Draw;
        if (!_wasInDrawPhase && nowInDraw)
        {
            // Cache assignment immediately on entering Draw phase so FlushCurrentStroke
            // can use the correct chainLink/originSlot even after the phase has changed.
            var runner2 = NetworkBootstrap.Instance?.Runner;
            if (runner2 != null && pm.TryGetAssignment(runner2.LocalPlayer, out var a))
            {
                _cachedChainLink = a.ChainLinkIndex;
                _cachedOriginSlot = a.AlbumOriginSlotIndex;
                _hasCachedAssignment = true;
                Debug.Log($"[DrawingManager] Cached Draw assignment: chainLink={_cachedChainLink}, originSlot={_cachedOriginSlot}");
            }
        }

        if (_wasInDrawPhase && !nowInDraw)
        {
            FlushCurrentStroke();
            if (_hasCachedAssignment)
                BroadcastTexture(_cachedChainLink, _cachedOriginSlot);
            _hasCachedAssignment = false;
        }
        _wasInDrawPhase = nowInDraw;
        // Chỉ xử lý vẽ khi chuột nằm trong DrawingArea
        if (!IsMouseOverDrawingArea()) return;

        if (Input.GetMouseButtonDown(0))
        {
            lastPos = null;
            isDrawing = false;
            _currentStrokeUV.Clear();
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
            FlushCurrentStroke();
        }
    }

    private void BroadcastTexture(byte chainLink, byte originSlot)
    {
        if (texture == null) return;
        var runner = NetworkBootstrap.Instance?.Runner;
        var registry = ServiceLocator.Get<PlayerRegistry>();
        if (runner == null || registry == null) return;

        var png = texture.EncodeToPNG();

        // Prefix with magic byte + chainLink + originSlot so receiver can identify it
        var data = new byte[png.Length + 3];
        data[0] = 0xDA; // magic: Drawing Artwork
        data[1] = chainLink;
        data[2] = originSlot;
        System.Array.Copy(png, 0, data, 3, png.Length);

        // Store locally (this player already has it — no need to receive)
        DrawingTextureStore.StoreTexture(chainLink, originSlot, png);

        // Send to all other connected players
        foreach (var pair in registry.Slots)
        {
            if (!pair.Value.IsConnected) continue;
            if (pair.Key == runner.LocalPlayer) continue;
            // Correct:
            runner.SendReliableDataToPlayer(pair.Key, DrawingTextureKey, data);
            Debug.Log($"[DrawingManager] Sent texture to {pair.Key}: chainLink={chainLink}, originSlot={originSlot}, size={png.Length} bytes");
        }
    }

    private void FlushCurrentStroke()
    {
        Debug.Log($"[DrawingManager] FlushCurrentStroke: uvPoints={_currentStrokeUV.Count}");
        if (_currentStrokeUV.Count == 0) return;
        byte chainLink = _cachedChainLink;
        byte originSlot = _cachedOriginSlot;

        if (!_hasCachedAssignment)
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var runner = NetworkBootstrap.Instance?.Runner;
            if (pm == null || runner == null || pm.CurrentPhase != PhaseType.Draw)
            {
                Debug.LogWarning("[DrawingManager] FlushCurrentStroke: no cached assignment and not in Draw phase, discarding");
                _currentStrokeUV.Clear();
                return;
            }
            if (!pm.TryGetAssignment(runner.LocalPlayer, out var a))
            {
                Debug.LogWarning("[DrawingManager] TryGetAssignment failed");
                _currentStrokeUV.Clear();
                return;
            }
            chainLink = a.ChainLinkIndex;
            originSlot = a.AlbumOriginSlotIndex;
        }
        var registry = ServiceLocator.Get<PlayerRegistry>();
        if (registry == null)
        {
            Debug.LogWarning("[DrawingManager] PlayerRegistry not found, cannot sync stroke");
            _currentStrokeUV.Clear();
            return;
        }
        var strokeBytes = DrawingDataConverter.ViewportPointsToByteArray(new List<Vector3>(_currentStrokeUV));
        registry.Rpc_SyncDrawingStroke(strokeBytes, chainLink, originSlot,
            (byte)(currentColor.r * 255),
            (byte)(currentColor.g * 255),
            (byte)(currentColor.b * 255));
        _currentStrokeUV.Clear();
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
        // Collect normalized UV point for network sync
        _currentStrokeUV.Add(new Vector3(texPos.x / textureWidth, texPos.y / textureHeight, 0f));
        texture.Apply();
        drawingCanvas.texture = texture;
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