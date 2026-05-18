using System.Collections.Generic;
using UnityEngine;
using InkEcho.Network.Data;

/// <summary>
/// Ví dụ cách sử dụng DrawingDataConverter để chuyển đổi dữ liệu vẽ
/// </summary>
public class DrawingDataConversionExample : MonoBehaviour
{
    [SerializeField] private List<Vector3> testPoints;

    private void Start()
    {
        if (testPoints == null || testPoints.Count == 0)
            GenerateTestData();

        // Ví dụ 1: Chuyển đổi Points → Bytes
        Example_PointsToBytes();

        // Ví dụ 2: Giải mã Bytes → Points
        Example_BytesToPoints();

        // Ví dụ 3: Tính toán compression ratio
        Example_CompressionRatio();

        // Ví dụ 4: Sử dụng DrawingDataManager
        Example_DrawingDataManager();

        // Ví dụ 5: Chunking dữ liệu lớn
        Example_DataChunking();
    }

    /// <summary>
    /// Ví dụ 1: Chuyển đổi Vector3 list thành byte array
    /// </summary>
    private void Example_PointsToBytes()
    {
        Debug.Log("\n=== EXAMPLE 1: Points → Bytes ===");

        byte[] encoded = DrawingDataConverter.PointsToByteArray(testPoints);

        Debug.Log($"Input: {testPoints.Count} points");
        Debug.Log($"Output: {encoded.Length} bytes");
        Debug.Log($"Data: {string.Join(", ", System.Array.ConvertAll(encoded, b => b.ToString("X2")))}");

        DrawingDataConverter.LogEncodingInfo(testPoints, encoded);
    }

    /// <summary>
    /// Ví dụ 2: Giải mã byte array thành Vector3 list
    /// </summary>
    private void Example_BytesToPoints()
    {
        Debug.Log("\n=== EXAMPLE 2: Bytes → Points ===");

        byte[] encoded = DrawingDataConverter.PointsToByteArray(testPoints);
        List<Vector3> decoded = DrawingDataConverter.ByteArrayToPoints(encoded);

        Debug.Log($"Input: {encoded.Length} bytes");
        Debug.Log($"Output: {decoded.Count} points");

        // Kiểm tra sai số
        float maxError = 0;
        for (int i = 0; i < testPoints.Count; i++)
        {
            float error = Vector3.Distance(testPoints[i], decoded[i]);
            if (error > maxError)
                maxError = error;
        }

        Debug.Log($"Max reconstruction error: {maxError:F6} units");
    }

    /// <summary>
    /// Ví dụ 3: Tính toán tỷ lệ nén
    /// </summary>
    private void Example_CompressionRatio()
    {
        Debug.Log("\n=== EXAMPLE 3: Compression Ratio ===");

        byte[] encoded = DrawingDataConverter.PointsToByteArray(testPoints);
        int originalSize = testPoints.Count * 12;  // 3 floats = 12 bytes
        int encodedSize = encoded.Length;

        float compressionRatio = DrawingDataConverter.CalculateCompressionRatio(testPoints.Count, encodedSize);

        Debug.Log($"Original Size: {originalSize} bytes");
        Debug.Log($"Encoded Size: {encodedSize} bytes");
        Debug.Log($"Saved: {originalSize - encodedSize} bytes ({compressionRatio:F1}%)");
        Debug.Log($"Bandwidth reduction: {(1 - (float)encodedSize / originalSize) * 100:F1}%");
    }

    /// <summary>
    /// Ví dụ 4: Sử dụng DrawingDataManager
    /// </summary>
    private void Example_DrawingDataManager()
    {
        Debug.Log("\n=== EXAMPLE 4: DrawingDataManager ===");

        var manager = DrawingDataManager.Instance;

        // Tạo stroke
        var stroke = manager.CreateStroke(0, testPoints);
        Debug.Log($"Created Stroke ID: {stroke.StrokeId}");
        Debug.Log($"Stroke Size: {stroke.GetDataSize()} bytes");
        Debug.Log($"Compression: {stroke.GetCompressionRatio():F1}%");

        // Lấy stroke từ cache
        if (manager.TryGetStroke(stroke.StrokeId, out var cached))
        {
            Debug.Log($"Retrieved stroke from cache: {cached.StrokeId}");
        }

        // Thống kê
        manager.LogStatistics();
    }

    /// <summary>
    /// Ví dụ 5: Chia nhỏ dữ liệu lớn thành chunks
    /// </summary>
    private void Example_DataChunking()
    {
        Debug.Log("\n=== EXAMPLE 5: Data Chunking ===");

        var manager = DrawingDataManager.Instance;
        byte[] encoded = DrawingDataConverter.PointsToByteArray(testPoints);

        // Chia thành chunks
        byte[][] chunks = manager.ChunkData(encoded);
        Debug.Log($"Original data: {encoded.Length} bytes");
        Debug.Log($"Split into {chunks.Length} chunks");

        for (int i = 0; i < chunks.Length; i++)
        {
            Debug.Log($"  Chunk {i}: {chunks[i].Length} bytes");
        }

        // Gộp lại
        byte[] merged = manager.MergeChunks(chunks);
        Debug.Log($"Merged back: {merged.Length} bytes");
        string integrityStatus = System.Array.Equals(encoded, merged) ? "OK" : "FAILED";
        Debug.Log($"Data integrity: {integrityStatus}");
    }

    /// <summary>
    /// Tạo dữ liệu test
    /// </summary>
    private void GenerateTestData()
    {
        testPoints = new List<Vector3>();

        // Vẽ một hình vuông
        Vector3 center = Vector3.zero;
        float size = 1f;

        testPoints.Add(center + new Vector3(-size, -size, 0));
        testPoints.Add(center + new Vector3(-size, size, 0));
        testPoints.Add(center + new Vector3(size, size, 0));
        testPoints.Add(center + new Vector3(size, -size, 0));
        testPoints.Add(center + new Vector3(-size, -size, 0));

        Debug.Log($"Generated {testPoints.Count} test points");
    }
}
