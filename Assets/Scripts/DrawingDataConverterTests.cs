using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using InkEcho.Network.Data;

/// <summary>
/// Unit tests cho DrawingDataConverter
/// Chạy: Window > TextTest Runner hoặc Run Tests
/// </summary>
public class DrawingDataConverterTests
{
    [Test]
    public void PointsToByteArray_EmptyList_ReturnsEmpty()
    {
        var points = new List<Vector3>();
        byte[] result = DrawingDataConverter.PointsToByteArray(points);
        Assert.That(result.Length, Is.EqualTo(0));
    }

    [Test]
    public void PointsToByteArray_SinglePoint_EncodesCorrectly()
    {
        var points = new List<Vector3> { new Vector3(1.5f, 2.5f, 0) };
        byte[] result = DrawingDataConverter.PointsToByteArray(points);

        // Kích thước: 2 (count) + 4 (first point)
        Assert.That(result.Length, Is.GreaterThanOrEqualTo(6));
    }

    [Test]
    public void PointsToByteArray_MultiplePoints_EncodesCorrectly()
    {
        var points = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(2, 2, 0)
        };

        byte[] result = DrawingDataConverter.PointsToByteArray(points);

        // Kích thước: 2 (count) + 4 (first) + 2*2 (deltas)
        Assert.That(result.Length, Is.GreaterThanOrEqualTo(10));
    }

    [Test]
    public void ByteArrayToPoints_EmptyArray_ReturnsEmpty()
    {
        byte[] data = new byte[0];
        List<Vector3> result = DrawingDataConverter.ByteArrayToPoints(data);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    [Test]
    public void ByteArrayToPoints_ValidData_DecodesCorrectly()
    {
        var original = new List<Vector3>
        {
            new Vector3(1.5f, 2.5f, 0),
            new Vector3(1.6f, 2.6f, 0),
            new Vector3(1.7f, 2.7f, 0)
        };

        byte[] encoded = DrawingDataConverter.PointsToByteArray(original);
        List<Vector3> decoded = DrawingDataConverter.ByteArrayToPoints(encoded);

        Assert.That(decoded.Count, Is.EqualTo(original.Count));

        // Kiểm tra sai số nhỏ
        for (int i = 0; i < original.Count; i++)
        {
            Assert.That(Vector3.Distance(original[i], decoded[i]), Is.LessThan(0.02f));
        }
    }

    [Test]
    public void RoundTrip_LargeDataset_PreservesAccuracy()
    {
        var original = new List<Vector3>();

        // Tạo 100 điểm
        for (int i = 0; i < 100; i++)
        {
            original.Add(new Vector3(i * 0.1f, Mathf.Sin(i * 0.1f), 0));
        }

        byte[] encoded = DrawingDataConverter.PointsToByteArray(original);
        List<Vector3> decoded = DrawingDataConverter.ByteArrayToPoints(encoded);

        Assert.That(decoded.Count, Is.EqualTo(original.Count));

        float maxError = 0;
        for (int i = 0; i < original.Count; i++)
        {
            float error = Vector3.Distance(original[i], decoded[i]);
            maxError = Mathf.Max(maxError, error);
        }

        Assert.That(maxError, Is.LessThan(0.02f));
    }

    [Test]
    public void CalculateEncodedSize_Correct()
    {
        int size = DrawingDataConverter.CalculateEncodedSize(100);

        // 2 (count) + 4 (first) + 98*2 (deltas) = 202
        Assert.That(size, Is.EqualTo(202));
    }

    [Test]
    public void CalculateCompressionRatio_CorrectPercentage()
    {
        int pointCount = 100;
        int originalSize = pointCount * 12;  // 1200
        int encodedSize = 202;

        float ratio = DrawingDataConverter.CalculateCompressionRatio(pointCount, encodedSize);

        // (1200 - 202) / 1200 * 100 ≈ 83.17%
        Assert.That(ratio, Is.GreaterThan(80).And.LessThan(85));
    }

    [Test]
    public void ValidateByteArray_InvalidSize_ReturnsFalse()
    {
        byte[] data = new byte[1];  // Quá nhỏ
        bool result = DrawingDataConverter.ValidateByteArray(data);
        Assert.That(result, Is.False);
    }

    [Test]
    public void ValidateByteArray_ValidData_ReturnsTrue()
    {
        var points = new List<Vector3> { new Vector3(1, 2, 0), new Vector3(2, 3, 0) };
        byte[] encoded = DrawingDataConverter.PointsToByteArray(points);
        bool result = DrawingDataConverter.ValidateByteArray(encoded);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CalculatePointsHash_SamePoints_SameHash()
    {
        var points = new List<Vector3>
        {
            new Vector3(1, 2, 0),
            new Vector3(2, 3, 0)
        };

        ulong hash1 = DrawingDataConverter.CalculatePointsHash(points);
        ulong hash2 = DrawingDataConverter.CalculatePointsHash(points);

        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void CalculatePointsHash_DifferentPoints_DifferentHash()
    {
        var points1 = new List<Vector3> { new Vector3(1, 2, 0) };
        var points2 = new List<Vector3> { new Vector3(2, 3, 0) };

        ulong hash1 = DrawingDataConverter.CalculatePointsHash(points1);
        ulong hash2 = DrawingDataConverter.CalculatePointsHash(points2);

        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void StrokeData_CreateAndRetrieve_Works()
    {
        var points = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(2, 2, 0)
        };

        var stroke = new StrokeData(0, 5, points);

        Assert.That(stroke.StrokeId, Is.EqualTo(0));
        Assert.That(stroke.PlayerId, Is.EqualTo(5));
        Assert.That(stroke.PointCount, Is.EqualTo(3));
        Assert.That(stroke.PointsData.Length, Is.GreaterThan(0));
    }

    [Test]
    public void StrokeData_Compression_CalculatedCorrectly()
    {
        var points = new List<Vector3>();
        for (int i = 0; i < 50; i++)
        {
            points.Add(new Vector3(i * 0.1f, i * 0.1f, 0));
        }

        var stroke = new StrokeData(0, 0, points);
        float ratio = stroke.GetCompressionRatio();

        Assert.That(ratio, Is.GreaterThan(75).And.LessThan(85));
    }
}
