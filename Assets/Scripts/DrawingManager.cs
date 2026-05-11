using System.Collections.Generic;
using UnityEngine;

public class DrawingManager : MonoBehaviour
{
    public GameObject linePrefab;
    public float minDistance = 0.05f;
    public Collider2D drawingArea;

    // State
    private LineRenderer currentLine;
    private List<Vector3> points = new List<Vector3>();

    private Color currentColor = Color.black;
    private float brushSize = 0.05f;
    private bool isEraser = false;

    //Undo/Redo
    private Stack<GameObject> undoStack = new Stack<GameObject>();
    private Stack<GameObject> redoStack = new Stack<GameObject>();

    private int currentSortingOrder = 0;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            StartLine();
        }

        if (Input.GetMouseButton(0))
        {
            DrawLine();
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndLine();
        }

        if (Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Undo();
            }
            else if (Input.GetKeyDown(KeyCode.Y))
            {
                Redo();
            }
        }
    }

    //Ve line
    void StartLine()
    {
        Debug.Log($"isErasing={isEraser}, color={currentColor}, brushSize={brushSize}");
        Vector3 worldPos = GetWorldPos();
        if (worldPos == Vector3.zero) return;
        if (drawingArea != null && !drawingArea.OverlapPoint(worldPos)) return;

        GameObject lineObj = Instantiate(linePrefab);
        currentLine = lineObj.GetComponent<LineRenderer>();
        currentLine.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        currentLine.useWorldSpace = true;
        currentLine.positionCount = 0;
        currentLine.numCapVertices = 8;
        currentLine.numCornerVertices = 8;

        // ✅ Tạo material 1 lần, dùng chung
        currentLine.material = new Material(Shader.Find("Sprites/Default"));

        if (isEraser)
        {
            // ✅ Dùng biến isErasing thật sự
            currentLine.startColor = Color.white;
            currentLine.endColor = Color.white;
            currentLine.startWidth = brushSize * 3f;
            currentLine.endWidth = brushSize * 3f;
            currentLine.sortingOrder = currentSortingOrder;
        }
        else
        {
            // ✅ Dùng currentColor thật sự
            currentLine.startColor = currentColor;
            currentLine.endColor = currentColor;
            currentLine.startWidth = brushSize;
            currentLine.endWidth = brushSize;
            currentLine.sortingOrder = currentSortingOrder;
        }
        currentSortingOrder++;

        points.Clear();
        AddPoint(worldPos);
    }
    void DrawLine()
    {
        if (currentLine == null) return;

        Vector3 mousePos = Input.mousePosition;

        // Quan trọng: khoảng cách từ camera (-10) đến mặt phẳng vẽ (0)
        mousePos.z = 10f;

        Vector3 worldPos = GetWorldPos();
        worldPos.z = 0f;

        if (drawingArea != null && !drawingArea.OverlapPoint(worldPos))
        {
            return; // Không vẽ nếu ngoài khu vực vẽ
        }

        if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], worldPos) > minDistance)
        {
            AddPoint(worldPos);
        }
    }
    void EndLine()
    {
        if (currentLine == null) return;

        foreach (var go in redoStack) Destroy(go);
        redoStack.Clear();

        undoStack.Push(currentLine.gameObject);
        currentLine = null;
    }
    void AddPoint(Vector3 point)
    {
        points.Add(point);
        currentLine.positionCount = points.Count;
        currentLine.SetPosition(points.Count - 1, point);
    }
    public void SetColor(Color color)
    {
        currentColor = color;
        isEraser = false;
    }
    public void SetBrushSize(float size)
    {
        //goi tu slider, size tu 1-10
        brushSize = size;
    }
    public void ToggleEraser()
    {
        isEraser = !isEraser;
    }
    public void SetEraser(bool value)
    {
        isEraser = value;
    }
    public void ClearAll()
    {
        foreach (var go in undoStack) Destroy(go);
        undoStack.Clear();
        foreach (var go in redoStack) Destroy(go);
        redoStack.Clear();

        if (currentLine != null)
        {
            Destroy(currentLine.gameObject);
            currentLine = null;
        }
    }
    public void Undo()
    {
        if (undoStack.Count == 0 ) return;
        var go = undoStack.Pop();
        go.SetActive(false);
        redoStack.Push(go);
    }
    public void Redo()
    {
        if (redoStack.Count == 0) return;
        var go = redoStack.Pop();
        go.SetActive(true);
        undoStack.Push(go);
    }
    Vector3 GetWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f; // Khoảng cách từ camera đến mặt phẳng vẽ
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}