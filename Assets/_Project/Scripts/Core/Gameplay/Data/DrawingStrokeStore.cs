using System.Collections.Generic;
using UnityEngine;

namespace InkEcho.Gameplay.Data
{
    public static class DrawingStrokeStore
    {
        private static readonly Dictionary<int, List<List<Vector3>>> _strokes = new();
        private static readonly Dictionary<int, List<Color>> _colors = new();

        public static void StoreStroke(int chainLink, int originSlot, List<Vector3> points, Color color)
        {
            if (points == null || points.Count == 0)
            {
                Debug.LogWarning($"[DrawingStrokeStore] No points to store for chainLink {chainLink}, originSlot {originSlot}");
                return;
            }
            var key = Key(chainLink, originSlot);

            if (!_strokes.TryGetValue(key, out var strokeList))
            {
                strokeList = new List<List<Vector3>>();
                _strokes[key] = strokeList;
            }
            strokeList.Add(new List<Vector3>(points));

            if (!_colors.TryGetValue(key, out var colorList))
            {
                colorList = new List<Color>();
                _colors[key] = colorList;
            }
            colorList.Add(color);

            Debug.Log($"[DrawingStrokeStore] Stored stroke at chainLink={chainLink}, originSlot={originSlot} | total strokes={strokeList.Count}, points={points.Count}");
        }

        public static void StoreStroke(int chainLink, int originSlot, List<Vector3> points)
            => StoreStroke(chainLink, originSlot, points, Color.black);

        public static IReadOnlyList<List<Vector3>> GetStrokes(int chainLink, int originSlot)
        {
            var key = Key(chainLink, originSlot);
            if (_strokes.TryGetValue(key, out var strokeList))
                return strokeList;
            Debug.LogWarning($"[DrawingStrokeStore] No strokes found for chainLink {chainLink}, originSlot {originSlot}");
            return new List<List<Vector3>>();
        }

        public static bool HasStrokes(int chainLink, int originSlot)
        {
            var key = Key(chainLink, originSlot);
            return _strokes.TryGetValue(key, out var strokeList) && strokeList.Count > 0;
        }

        public static IReadOnlyList<Color> GetStrokeColors(int chainLink, int originSlot)
        {
            var key = Key(chainLink, originSlot);
            if (_colors.TryGetValue(key, out var colorList))
                return colorList;
            return new List<Color>();
        }

        public static void ClearStrokes()
        {
            _strokes.Clear();
            _colors.Clear();
        }

        private static int Key(int chainLink, int originSlot)
            => chainLink * 32 + originSlot;
    }
}
