using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InkEcho.Gameplay.Data
{
    public static class DrawingStrokeStore
    {
        private static readonly Dictionary<int, List<List<Vector3>>> _strokes = new();

        public static void StoreStroke(int chainLink, int originSlot, List<Vector3> points)
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
        }
        public static IReadOnlyList<List<Vector3>> GetStrokes(int chainLink, int originSlot)
        {
            var key = Key(chainLink, originSlot);
            if (_strokes.TryGetValue(key, out var strokeList))
            {
                return strokeList;
            }
            Debug.LogWarning($"[DrawingStrokeStore] No strokes found for chainLink {chainLink}, originSlot {originSlot}");
            return new List<List<Vector3>>();
        }
        public static void ClearStrokes()
        {
            _strokes.Clear();
        }
        private static int Key(int chainLink, int originSlot)
        {
            return chainLink * 32 + originSlot; // Simple hashing to create a unique key
        }
    }
}
