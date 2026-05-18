using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Fusion;

namespace InkEcho.Network.Data
{
    /// <summary>
    /// Chuyên dụng chuyển đổi dữ liệu vẽ giữa các định dạng:
    /// - Vector3 list → Byte array (gửi mạng)
    /// - Byte array → Vector3 list (nhận từ mạng)
    /// - Texture2D → Byte array (nếu cần)
    /// - Byte array → Texture2D (nếu cần)
    /// </summary>
    public static class DrawingDataConverter
    {
        private const int POINT_PRECISION_MULTIPLIER = 1000;  // Độ chính xác 0.001 cho điểm đầu
        private const int DELTA_PRECISION_MULTIPLIER = 100;   // Độ chính xác 0.01 cho delta

        /// <summary>
        /// Chuyển đổi danh sách điểm Vector3 thành byte array được tối ưu hóa
        /// </summary>
        public static byte[] PointsToByteArray(List<Vector3> points)
        {
            if (points == null || points.Count == 0)
                return new byte[0];

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                // Ghi số điểm
                writer.Write((ushort)points.Count);

                Vector3 lastPoint = Vector3.zero;

                for (int i = 0; i < points.Count; i++)
                {
                    Vector3 point = points[i];

                    if (i == 0)
                    {
                        // Điểm đầu tiên: tọa độ tuyệt đối
                        writer.Write((short)(point.x * POINT_PRECISION_MULTIPLIER));
                        writer.Write((short)(point.y * POINT_PRECISION_MULTIPLIER));
                    }
                    else
                    {
                        // Các điểm tiếp theo: delta encoding
                        Vector3 delta = point - lastPoint;
                        writer.Write((sbyte)(delta.x * DELTA_PRECISION_MULTIPLIER));
                        writer.Write((sbyte)(delta.y * DELTA_PRECISION_MULTIPLIER));
                    }

                    lastPoint = point;
                }

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Giải mã byte array thành danh sách Vector3
        /// </summary>
        public static List<Vector3> ByteArrayToPoints(byte[] data)
        {
            var points = new List<Vector3>();

            if (data == null || data.Length < 2)
                return points;

            try
            {
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    ushort pointCount = reader.ReadUInt16();
                    Vector3 lastPoint = Vector3.zero;

                    for (int i = 0; i < pointCount; i++)
                    {
                        if (i == 0)
                        {
                            float x = reader.ReadInt16() / (float)POINT_PRECISION_MULTIPLIER;
                            float y = reader.ReadInt16() / (float)POINT_PRECISION_MULTIPLIER;
                            lastPoint = new Vector3(x, y, 0f);
                        }
                        else
                        {
                            float deltaX = reader.ReadSByte() / (float)DELTA_PRECISION_MULTIPLIER;
                            float deltaY = reader.ReadSByte() / (float)DELTA_PRECISION_MULTIPLIER;
                            lastPoint = lastPoint + new Vector3(deltaX, deltaY, 0f);
                        }

                        points.Add(lastPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DrawingDataConverter] Lỗi giải mã byte array: {ex.Message}");
            }

            return points;
        }

        /// <summary>
        /// Chuyển đổi Texture2D thành byte array (PNG format)
        /// </summary>
        public static byte[] TextureToByteArray(Texture2D texture)
        {
            if (texture == null)
                return new byte[0];

            return texture.EncodeToPNG();
        }

        /// <summary>
        /// Giải mã byte array thành Texture2D (PNG format)
        /// </summary>
        public static Texture2D ByteArrayToTexture(byte[] data)
        {
            if (data == null || data.Length == 0)
                return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (texture.LoadImage(data))
            {
                return texture;
            }

            UnityEngine.Object.Destroy(texture);
            return null;
        }

        /// <summary>
        /// Tính kích thước dữ liệu sau khi nén
        /// </summary>
        public static int CalculateEncodedSize(int pointCount)
        {
            if (pointCount == 0) return 0;

            // 2 bytes cho ushort (số điểm)
            int size = 2;

            // Điểm đầu: 2 short (4 bytes)
            size += 4;

            // Các điểm còn lại: 2 sbyte mỗi điểm (2 bytes)
            if (pointCount > 1)
                size += (pointCount - 1) * 2;

            return size;
        }

        /// <summary>
        /// Tính tỷ lệ nén
        /// </summary>
        public static float CalculateCompressionRatio(int pointCount, int encodedSize)
        {
            if (pointCount == 0) return 0;

            // Kích thước gốc: mỗi Vector3 là 3 float (12 bytes)
            int originalSize = pointCount * 12;

            if (originalSize == 0) return 0;
            return (float)(originalSize - encodedSize) / originalSize * 100f;
        }

        /// <summary>
        /// Xác thực dữ liệu byte array có hợp lệ không
        /// </summary>
        public static bool ValidateByteArray(byte[] data)
        {
            if (data == null || data.Length < 2)
                return false;

            try
            {
                using (var stream = new MemoryStream(data))
                using (var reader = new BinaryReader(stream))
                {
                    ushort pointCount = reader.ReadUInt16();

                    // Tính kích thước dự kiến
                    int expectedSize = CalculateEncodedSize(pointCount);

                    // Kiểm tra kích thước khớp
                    if (data.Length != expectedSize)
                    {
                        Debug.LogWarning($"[DrawingDataConverter] Kích thước không khớp. Dự kiến: {expectedSize}, thực tế: {data.Length}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DrawingDataConverter] Lỗi xác thực: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tạo hash từ điểm dữ liệu (để kiểm tra integrity)
        /// </summary>
        public static ulong CalculatePointsHash(List<Vector3> points)
        {
            if (points == null || points.Count == 0)
                return 0UL;

            ulong hash = 1469598103934665603UL;
            unchecked
            {
                foreach (var point in points)
                {
                    var fx = (int)(point.x * 1000f);
                    var fy = (int)(point.y * 1000f);
                    hash ^= (ulong)(fx & 0xFFFFFFFF);
                    hash *= 1099511628211UL;
                    hash ^= (ulong)(fy & 0xFFFFFFFF);
                    hash *= 1099511628211UL;
                }
            }

            return hash;
        }

        /// <summary>
        /// Log thông tin chi tiết về dữ liệu được mã hóa
        /// </summary>
        public static void LogEncodingInfo(List<Vector3> points, byte[] encoded)
        {
            int pointCount = points?.Count ?? 0;
            int originalSize = pointCount * 12;
            int encodedSize = encoded?.Length ?? 0;
            float compressionRatio = CalculateCompressionRatio(pointCount, encodedSize);

            Debug.Log($"[DrawingDataConverter] Encoding Info:\n" +
                $"  Points: {pointCount}\n" +
                $"  Original Size: {originalSize} bytes\n" +
                $"  Encoded Size: {encodedSize} bytes\n" +
                $"  Compression: {compressionRatio:F2}%");
        }
    }
}
