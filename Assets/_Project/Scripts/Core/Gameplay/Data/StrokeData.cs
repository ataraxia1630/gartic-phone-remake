using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace InkEcho.Network.Data
{
    /// <summary>
    /// Đại diện cho một nét vẽ được mã hóa để truyền qua mạng
    /// </summary>
    [System.Serializable]
    public class StrokeData
    {
        public int StrokeId;
        public int PlayerId;
        public byte[] PointsData;  // Dữ liệu điểm được nén
        public long Timestamp;
        public int PointCount;     // Số điểm thực tế

        public StrokeData() { }

        public StrokeData(int strokeId, int playerId, List<Vector3> points)
        {
            StrokeId = strokeId;
            PlayerId = playerId;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            PointCount = points.Count;
            PointsData = EncodePoints(points);
        }

        /// <summary>
        /// Mã hóa danh sách điểm Vector3 thành byte array
        /// Format: [x1_compressed, y1_compressed, x2_delta, y2_delta, ...]
        /// </summary>
        public static byte[] EncodePoints(List<Vector3> points)
        {
            if (points == null || points.Count == 0)
                return new byte[0];

            using (var stream = new System.IO.MemoryStream())
            {
                using (var writer = new System.IO.BinaryWriter(stream))
                {
                    // Ghi số điểm
                    writer.Write((ushort)points.Count);

                    Vector3 lastPoint = Vector3.zero;

                    for (int i = 0; i < points.Count; i++)
                    {
                        Vector3 point = points[i];

                        // Điểm đầu tiên ghi tọa độ tuyệt đối
                        if (i == 0)
                        {
                            writer.Write((short)(point.x * 1000f));  // Chuyển đổi thành số nguyên với độ chính xác 0.001
                            writer.Write((short)(point.y * 1000f));
                        }
                        else
                        {
                            // Điểm tiếp theo ghi delta (hiệu số)
                            Vector3 delta = point - lastPoint;
                            writer.Write((sbyte)(delta.x * 100f));  // Delta nhỏ hơn nên dùng byte
                            writer.Write((sbyte)(delta.y * 100f));
                        }

                        lastPoint = point;
                    }

                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Giải mã byte array thành danh sách Vector3
        /// </summary>
        public static List<Vector3> DecodePoints(byte[] data)
        {
            var points = new List<Vector3>();

            if (data == null || data.Length < 2)
                return points;

            using (var stream = new System.IO.MemoryStream(data))
            {
                using (var reader = new System.IO.BinaryReader(stream))
                {
                    ushort pointCount = reader.ReadUInt16();

                    Vector3 lastPoint = Vector3.zero;

                    for (int i = 0; i < pointCount; i++)
                    {
                        if (i == 0)
                        {
                            float x = reader.ReadInt16() / 1000f;
                            float y = reader.ReadInt16() / 1000f;
                            lastPoint = new Vector3(x, y, 0f);
                        }
                        else
                        {
                            float deltaX = reader.ReadSByte() / 100f;
                            float deltaY = reader.ReadSByte() / 100f;
                            lastPoint = lastPoint + new Vector3(deltaX, deltaY, 0f);
                        }

                        points.Add(lastPoint);
                    }
                }
            }

            return points;
        }

        /// <summary>
        /// Tính toán kích thước dữ liệu (byte)
        /// </summary>
        public int GetDataSize()
        {
            return PointsData?.Length ?? 0;
        }

        /// <summary>
        /// Tính tỷ lệ nén (%)
        /// </summary>
        public float GetCompressionRatio()
        {
            if (PointCount == 0) return 0;

            // Kích thước gốc: mỗi Vector3 là 3 float (12 bytes)
            int originalSize = PointCount * 12;
            int compressedSize = GetDataSize();

            return (float)(originalSize - compressedSize) / originalSize * 100f;
        }
    }

    /// <summary>
    /// Thông tin để chuyển đổi StrokeData cho mạng
    /// </summary>
    public struct StrokeNetworkMessage : INetworkStruct
    {
        public int StrokeId;
        public PlayerRef Owner;
        public ushort PointCount;
        public ulong Timestamp;  // Unix milliseconds
    }
}
