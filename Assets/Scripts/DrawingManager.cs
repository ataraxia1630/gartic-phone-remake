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

        if (drawingCanvas == null)
        {
            Debug.LogError("[DrawingManager] drawingCanvas (RawImage) chưa được gán trong Inspector! Hãy kéo RawImage vào field 'Drawing Canvas'.", this);
            yield break;
        }

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
            FlushAndBroadcast();
        }
        _wasInDrawPhase = nowInDraw;

        // Fallback: cache assignment during Draw even if we missed the phase transition.
        if (nowInDraw && !_hasCachedAssignment)
        {
            var runner = NetworkBootstrap.Instance?.Runner;
            if (runner != null && pm.TryGetAssignment(runner.LocalPlayer, out var a))
            {
                _cachedChainLink = a.ChainLinkIndex;
                _cachedOriginSlot = a.AlbumOriginSlotIndex;
                _hasCachedAssignment = true;
                Debug.Log($"[DrawingManager] Cached Draw assignment (fallback): chainLink={_cachedChainLink}, originSlot={_cachedOriginSlot}");
            }
        }
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

    // Flush nét đang vẽ dở rồi gửi texture đi. Idempotent: sau khi gửi sẽ clear _hasCachedAssignment
    // nên gọi nhiều lần (Update + OnDestroy) không bị broadcast trùng.
    private void FlushAndBroadcast()
    {
        FlushCurrentStroke();
        if (_hasCachedAssignment)
        {
            // Coop: chỉ role 0 (người vẽ chính) broadcast tranh. Role 1 chỉ xem/hướng dẫn.
            var runner = NetworkBootstrap.Instance?.Runner;
            var pm = ServiceLocator.Get<PhaseManager>();
            bool isSecondaryRole = false;
            if (runner != null && pm != null && pm.TryGetAssignment(runner.LocalPlayer, out var a))
                isSecondaryRole = a.PairRole == 1 && a.HasSecondary;

            if (!isSecondaryRole)
                BroadcastTexture(_cachedChainLink, _cachedOriginSlot);
            else
                Debug.Log($"[DrawingManager] Coop role 1 — bỏ qua broadcast tranh (chainLink={_cachedChainLink}, originSlot={_cachedOriginSlot})");
        }
        _hasCachedAssignment = false;
    }

    void OnDestroy()
    {
        // UI_Draw bị unload ngay khi đổi phase (PhaseSceneController.SwitchPhaseScene).
        // Nếu Update chưa kịp bắt transition để broadcast trong frame đó, gửi nốt tranh trước khi object bị huỷ
        // — nếu không tranh sẽ không bao giờ tới host và Reveal sẽ trống.
        if (_hasCachedAssignment)
            FlushAndBroadcast();
    }

    private void BroadcastTexture(byte chainLink, byte originSlot)
    {
        if (texture == null) return;
        var runner = NetworkBootstrap.Instance?.Runner;
        if (runner == null) return;

        var png = texture.EncodeToPNG();

        // Store locally as OWN drawing — cần phân biệt với PNG nhận từ player khác khi rebroadcast
        DrawingTextureStore.StoreOwnTexture(chainLink, originSlot, png);

        // Prefix with magic byte + chainLink + originSlot so receiver can identify it
        var data = new byte[png.Length + 3];
        data[0] = 0xDA; // magic: Drawing Artwork
        data[1] = chainLink;
        data[2] = originSlot;
        System.Array.Copy(png, 0, data, 3, png.Length);

        // Send to all other connected players (use runner.ActivePlayers — always up-to-date)
        foreach (var player in runner.ActivePlayers)
        {
            if (player == runner.LocalPlayer) continue;
            var key = ReliableKey.FromInts(0xDA, chainLink, originSlot, 0);
            runner.SendReliableDataToPlayer(player, key, data);
            Debug.Log($"[DrawingManager] Sent texture to {player}: chainLink={chainLink}, originSlot={originSlot}, size={png.Length} bytes");
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
            _cachedChainLink = chainLink;
            _cachedOriginSlot = originSlot;
            _hasCachedAssignment = true;
        }
        var registry = ServiceLocator.Get<PlayerRegistry>();
        if (registry == null)
        {
            Debug.LogWarning("[DrawingManager] PlayerRegistry not found, cannot sync stroke");
            _currentStrokeUV.Clear();
            return;
        }
        var allPoints = new List<Vector3>(_currentStrokeUV);
        _currentStrokeUV.Clear();

        byte cr = (byte)(currentColor.r * 255);
        byte cg = (byte)(currentColor.g * 255);
        byte cb = (byte)(currentColor.b * 255);

        // Giới hạn payload RPC của Fusion ~512 byte; mỗi điểm = 4 byte + 2 byte header.
        // Chia nét dài thành nhiều RPC ≤100 điểm (~402 byte), overlap 1 điểm để nét không bị đứt.
        const int MaxPointsPerChunk = 100;
        int i = 0;
        while (i < allPoints.Count)
        {
            int end = Mathf.Min(i + MaxPointsPerChunk, allPoints.Count);
            var chunk = allPoints.GetRange(i, end - i);
            var strokeBytes = DrawingDataConverter.ViewportPointsToByteArray(chunk);
            registry.Rpc_SyncDrawingStroke(strokeBytes, chainLink, originSlot, cr, cg, cb);
            if (end >= allPoints.Count) break;
            i = end - 1; // overlap điểm cuối làm điểm đầu chunk kế
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

    public bool HasTexture => texture != null;

    public byte[] EncodeAsPng()
    {
        if (texture == null) return null;
        return texture.EncodeToPNG();
    }

    public void ApplyPng(byte[] png)
    {
        if (png == null || png.Length == 0) return;
        if (texture == null) return;
        var tmp = new Texture2D(2, 2);
        if (!tmp.LoadImage(png)) { Destroy(tmp); return; }

        if (tmp.width == textureWidth && tmp.height == textureHeight)
        {
            texture.SetPixels(tmp.GetPixels());
        }
        else
        {
            var resampled = ResamplePixels(tmp, textureWidth, textureHeight);
            texture.SetPixels(resampled);
        }
        texture.Apply();
        drawingCanvas.texture = texture;
        undoStack.Clear();
        redoStack.Clear();
        Destroy(tmp);
    }

    public void ResetCanvas()
    {
        if (texture == null) return;
        texture.SetPixels(clearColors);
        texture.Apply();
        drawingCanvas.texture = texture;
        undoStack.Clear();
        redoStack.Clear();
        lastPos = null;
        isDrawing = false;
    }

    private static Color[] ResamplePixels(Texture2D source, int targetW, int targetH)
    {
        var src = source.GetPixels();
        var dst = new Color[targetW * targetH];
        for (int y = 0; y < targetH; y++)
        {
            int sy = Mathf.Clamp((int)((y + 0.5f) * source.height / targetH), 0, source.height - 1);
            for (int x = 0; x < targetW; x++)
            {
                int sx = Mathf.Clamp((int)((x + 0.5f) * source.width / targetW), 0, source.width - 1);
                dst[y * targetW + x] = src[sy * source.width + sx];
            }
        }
        return dst;
    }
}