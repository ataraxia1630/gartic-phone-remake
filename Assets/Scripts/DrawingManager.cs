using System.Collections.Generic;
using UnityEngine;

public class DrawingManager : MonoBehaviour
{
    public GameObject linePrefab;
    public float minDistance = 0.05f;

    private LineRenderer currentLine;
    private List<Vector3> points = new List<Vector3>();

    public Collider2D drawingArea;

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
            currentLine = null;
        }
    }

    void StartLine()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 10f;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;

        if (!drawingArea.OverlapPoint(worldPos))
            return;

        GameObject lineObj = Instantiate(linePrefab);

        currentLine = lineObj.GetComponent<LineRenderer>();

        currentLine.useWorldSpace = true;
        currentLine.positionCount = 0;
        currentLine.startWidth = 0.05f;
        currentLine.endWidth = 0.05f;
        currentLine.sortingOrder = 100;

        currentLine.material = new Material(Shader.Find("Sprites/Default"));
        currentLine.startColor = Color.black;
        currentLine.endColor = Color.black;

        points.Clear();
    }

    void DrawLine()
    {
        if (currentLine == null) return;

        Vector3 mousePos = Input.mousePosition;

        // Quan trọng: khoảng cách từ camera (-10) đến mặt phẳng vẽ (0)
        mousePos.z = 10f;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;

        if (drawingArea != null && !drawingArea.OverlapPoint(worldPos))
        {
            return; // Không vẽ nếu ngoài khu vực vẽ
        }

        if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], worldPos) > minDistance)
        {
            points.Add(worldPos);
            currentLine.positionCount = points.Count;
            currentLine.SetPositions(points.ToArray());
        }
    }
}