using System.Collections.Generic;
using UnityEngine;

namespace InkEcho.Gameplay.Data
{
    public class DrawingReplayRenderer: MonoBehaviour
    {
        [SerializeField] 
        private GameObject linePrefab;
        [SerializeField] 
        private float lineWidth = 0.05f;
        [SerializeField]
        private Color lineColor = Color.black;

        private readonly List<GameObject> _lineObjects = new();

        public void RenderStrokes(IReadOnlyList<List<Vector3>> strokes)
        {
            Clear();
            if (strokes == null)
            {
                Debug.LogWarning("[DrawingReplayRenderer] No strokes to render");
                return;
            }

            foreach (var stroke in strokes)
            {
                var lr = CreateLine();
                lr.positionCount = stroke.Count;
                lr.SetPositions(stroke.ToArray());
            }
        }
        public void Clear()
        {
            foreach (var lineObj in _lineObjects)
            {
                Destroy(lineObj);
            }
            _lineObjects.Clear();
        }
        public LineRenderer CreateLine()
        {
            GameObject obj;
            if (linePrefab != null)
            {
                obj = Instantiate(linePrefab, transform);
            }
            else
            {
                obj = new GameObject("Line", typeof(LineRenderer));
                obj.transform.SetParent(transform);
            }
            _lineObjects.Add(obj);

            var lr = obj.GetComponent<LineRenderer>() ?? obj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.sortingOrder = 100; // Ensure lines render on top
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            return lr;
        }
        public void OnDestroy()
        {
            Clear();
        }
    }
}
