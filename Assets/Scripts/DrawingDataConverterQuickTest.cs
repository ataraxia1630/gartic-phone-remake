using System.Collections.Generic;
using UnityEngine;
using InkEcho.Network.Data;

/// <summary>
/// Quick verification script - gắn vào GameObject và chạy play mode
/// để kiểm tra chức năng DrawingDataConverter
/// </summary>
public class DrawingDataConverterQuickTest : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== Starting DrawingDataConverter Quick Test ===\n");

        Test_BasicEncoding();
        Test_Decoding();
        Test_Compression();
        Test_Validation();
        Test_Manager();

        Debug.Log("\n=== All Tests Completed ===");
    }

    private void Test_BasicEncoding()
    {
        Debug.Log("TEST 1: Basic Encoding");

        var points = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(2, 2, 0)
        };

        byte[] encoded = DrawingDataConverter.PointsToByteArray(points);
        Debug.Log($"  ✓ Encoded {points.Count} points to {encoded.Length} bytes");

        Assert(encoded.Length > 0, "Encoded data should not be empty");
    }

    private void Test_Decoding()
    {
        Debug.Log("\nTEST 2: Decoding");

        var original = new List<Vector3>
        {
            new Vector3(1.5f, 2.5f, 0),
            new Vector3(1.6f, 2.6f, 0),
            new Vector3(1.7f, 2.7f, 0)
        };

        byte[] encoded = DrawingDataConverter.PointsToByteArray(original);
        List<Vector3> decoded = DrawingDataConverter.ByteArrayToPoints(encoded);

        Debug.Log($"  ✓ Decoded back to {decoded.Count} points");

        Assert(decoded.Count == original.Count, "Point count should match");

        float maxError = 0;
        for (int i = 0; i < original.Count; i++)
        {
            float error = Vector3.Distance(original[i], decoded[i]);
            maxError = Mathf.Max(maxError, error);
        }

        Debug.Log($"  ✓ Max reconstruction error: {maxError:F6} (acceptable: < 0.02)");
        Assert(maxError < 0.02f, "Reconstruction error should be small");
    }

    private void Test_Compression()
    {
        Debug.Log("\nTEST 3: Compression Ratio");

        var points = new List<Vector3>();
        for (int i = 0; i < 100; i++)
        {
            points.Add(new Vector3(i * 0.1f, Mathf.Sin(i * 0.1f), 0));
        }

        byte[] encoded = DrawingDataConverter.PointsToByteArray(points);

        int originalSize = points.Count * 12;  // 3 floats × 4 bytes
        int encodedSize = encoded.Length;
        float compressionRatio = DrawingDataConverter.CalculateCompressionRatio(points.Count, encodedSize);

        Debug.Log($"  Original: {originalSize} bytes");
        Debug.Log($"  Encoded: {encodedSize} bytes");
        Debug.Log($"  ✓ Compression: {compressionRatio:F1}%");

        Assert(compressionRatio > 75, "Should achieve >75% compression");
    }

    private void Test_Validation()
    {
        Debug.Log("\nTEST 4: Data Validation");

        var points = new List<Vector3> { new Vector3(1, 2, 0), new Vector3(2, 3, 0) };
        byte[] encoded = DrawingDataConverter.PointsToByteArray(points);

        bool valid = DrawingDataConverter.ValidateByteArray(encoded);
        Debug.Log($"  ✓ Validation result: {(valid ? "PASS" : "FAIL")}");

        Assert(valid, "Valid data should pass validation");

        // Test invalid data
        byte[] invalidData = new byte[1];
        bool invalidValid = DrawingDataConverter.ValidateByteArray(invalidData);
        Debug.Log($"  ✓ Invalid data rejected: {(!invalidValid ? "PASS" : "FAIL")}");

        Assert(!invalidValid, "Invalid data should not pass validation");
    }

    private void Test_Manager()
    {
        Debug.Log("\nTEST 5: DrawingDataManager");

        var manager = DrawingDataManager.Instance;

        var points = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(2, 2, 0)
        };

        var stroke = manager.CreateStroke(0, points);
        Debug.Log($"  ✓ Created stroke #{stroke.StrokeId}");
        Debug.Log($"    Size: {stroke.GetDataSize()} bytes");
        Debug.Log($"    Compression: {stroke.GetCompressionRatio():F1}%");

        if (manager.TryGetStroke(stroke.StrokeId, out var cached))
        {
            Debug.Log($"  ✓ Retrieved stroke from cache");
        }

        var stats = manager.GetStatistics();
        Debug.Log($"  ✓ Stats - Strokes: {stats.TotalStrokes}, Points: {stats.TotalPoints}");
    }

    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError($"  ✗ ASSERTION FAILED: {message}");
        }
    }
}
