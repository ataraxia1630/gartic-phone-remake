using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace InkEcho.Network.Data
{
    /// <summary>
    /// Quản lý lưu trữ và truyền tải dữ liệu vẽ qua mạng
    /// Hỗ trợ:
    /// - Chunking (chia nhỏ dữ liệu lớn)
    /// - Compression tracking
    /// - Network optimization
    /// </summary>
    public class DrawingDataManager : Singleton<DrawingDataManager>
    {
        [Header("Network")]
        [SerializeField] private int maxChunkSize = 1000;  // Bytes per chunk

        [Header("Debug")]
        [SerializeField] private bool enableLogging = true;

        // Cache for recent strokes
        private readonly Dictionary<int, StrokeData> _strokeCache = new Dictionary<int, StrokeData>();
        private int _strokeIdCounter = 0;

        protected override void OnAwake()
        {
            base.OnAwake();
        }

        /// <summary>
        /// Tạo StrokeData mới từ danh sách điểm
        /// </summary>
        public StrokeData CreateStroke(int playerId, List<Vector3> points)
        {
            var stroke = new StrokeData
            {
                StrokeId = _strokeIdCounter++,
                PlayerId = playerId,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                PointCount = points.Count,
                PointsData = DrawingDataConverter.PointsToByteArray(points)
            };

            _strokeCache[stroke.StrokeId] = stroke;

            if (enableLogging)
            {
                Debug.Log($"[DrawingDataManager] Created stroke #{stroke.StrokeId} " +
                    $"({stroke.PointCount} points, {stroke.GetDataSize()} bytes, " +
                    $"{stroke.GetCompressionRatio():F1}% compression)");
            }

            return stroke;
        }

        /// <summary>
        /// Lấy StrokeData từ cache
        /// </summary>
        public bool TryGetStroke(int strokeId, out StrokeData stroke)
        {
            return _strokeCache.TryGetValue(strokeId, out stroke);
        }

        /// <summary>
        /// Xóa StrokeData khỏi cache
        /// </summary>
        public void RemoveStroke(int strokeId)
        {
            _strokeCache.Remove(strokeId);
        }

        /// <summary>
        /// Chia nhỏ byte array thành chunks để gửi qua mạng
        /// </summary>
        public byte[][] ChunkData(byte[] data)
        {
            if (data == null || data.Length == 0)
                return new[] { new byte[0] };

            int chunkCount = (data.Length + maxChunkSize - 1) / maxChunkSize;
            var chunks = new byte[chunkCount][];

            for (int i = 0; i < chunkCount; i++)
            {
                int start = i * maxChunkSize;
                int length = Mathf.Min(maxChunkSize, data.Length - start);
                chunks[i] = new byte[length];
                Array.Copy(data, start, chunks[i], 0, length);
            }

            return chunks;
        }

        /// <summary>
        /// Gộp chunks lại thành byte array
        /// </summary>
        public byte[] MergeChunks(byte[][] chunks)
        {
            if (chunks == null || chunks.Length == 0)
                return new byte[0];

            int totalSize = 0;
            foreach (var chunk in chunks)
            {
                totalSize += chunk?.Length ?? 0;
            }

            var result = new byte[totalSize];
            int offset = 0;

            foreach (var chunk in chunks)
            {
                if (chunk == null) continue;
                Array.Copy(chunk, 0, result, offset, chunk.Length);
                offset += chunk.Length;
            }

            return result;
        }

        /// <summary>
        /// Thống kê dữ liệu vẽ
        /// </summary>
        public DrawingStats GetStatistics()
        {
            var stats = new DrawingStats();

            foreach (var stroke in _strokeCache.Values)
            {
                stats.TotalStrokes++;
                stats.TotalPoints += stroke.PointCount;
                stats.TotalDataSize += stroke.GetDataSize();

                if (stats.MinDataSize == 0 || stroke.GetDataSize() < stats.MinDataSize)
                    stats.MinDataSize = stroke.GetDataSize();

                if (stroke.GetDataSize() > stats.MaxDataSize)
                    stats.MaxDataSize = stroke.GetDataSize();
            }

            if (stats.TotalStrokes > 0)
            {
                stats.AvgDataSize = stats.TotalDataSize / stats.TotalStrokes;
            }

            return stats;
        }

        /// <summary>
        /// Xóa cache
        /// </summary>
        public void ClearCache()
        {
            _strokeCache.Clear();
            _strokeIdCounter = 0;
        }

        /// <summary>
        /// Log thống kê
        /// </summary>
        public void LogStatistics()
        {
            var stats = GetStatistics();
            Debug.Log($"[DrawingDataManager] Statistics:\n" +
                $"  Total Strokes: {stats.TotalStrokes}\n" +
                $"  Total Points: {stats.TotalPoints}\n" +
                $"  Total Data Size: {stats.TotalDataSize} bytes\n" +
                $"  Avg Data Size: {stats.AvgDataSize} bytes\n" +
                $"  Min Data Size: {stats.MinDataSize} bytes\n" +
                $"  Max Data Size: {stats.MaxDataSize} bytes");
        }
    }

    /// <summary>
    /// Thống kê về dữ liệu vẽ
    /// </summary>
    [System.Serializable]
    public struct DrawingStats
    {
        public int TotalStrokes;
        public int TotalPoints;
        public int TotalDataSize;
        public int AvgDataSize;
        public int MinDataSize;
        public int MaxDataSize;
    }

    /// <summary>
    /// Base class cho Singleton pattern
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        var obj = new GameObject($"[{typeof(T).Name}]");
                        _instance = obj.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                OnAwake();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnAwake() { }
    }
}
