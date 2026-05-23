using InkEcho.Gameplay.Data;
using InkEcho.Network.Core;
using InkEcho.Network.Phases;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InkEcho.Hoai.UI
{
    public class ObserveOverlayPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI countdownLabel;
        [SerializeField] private TextMeshProUGUI hintLabel;
        [SerializeField] private string hintFormat = "Bạn sẽ vẽ tiếp chain {0} sau...";
        [SerializeField] private Image displayImage;
        private bool _rendered;
        private Texture2D _displayTexture;

        private void OnEnable()
        {
            _rendered = false;
            if (displayImage == null)
                displayImage = GetComponentInChildren<Image>(true);
            Refresh();
        }

        private void Update()
        {
            Refresh();
            if (!_rendered)
                TryRenderDrawing();
        }

        private void OnDisable()
        {
            _rendered = false;
            if (displayImage != null) displayImage.sprite = null;
            if (_displayTexture != null) { Object.Destroy(_displayTexture); _displayTexture = null; }
        }

        private void TryRenderDrawing()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var runner = NetworkBootstrap.Instance?.Runner;
            if (pm == null || runner == null) return;

            if (!pm.TryGetAssignment(runner.LocalPlayer, out var assignment))
            {
                Debug.LogWarning($"[Observe] TryGetAssignment failed. RoundIndex={pm.RoundIndex}");
                return;
            }

            int prevChainLink = assignment.ChainLinkIndex - 1;
            if (prevChainLink < 0) return;

            var strokes = DrawingStrokeStore.GetStrokes(prevChainLink, assignment.AlbumOriginSlotIndex);
            Debug.Log($"[Observe] GetStrokes({prevChainLink}, {assignment.AlbumOriginSlotIndex}) => {strokes?.Count ?? 0} strokes");

            if (strokes == null || strokes.Count == 0) return;

            RenderStrokesToImage(strokes);
            _rendered = true;
            Debug.Log("[Observe] Drawing rendered successfully");
        }

        private void RenderStrokesToImage(IReadOnlyList<List<Vector3>> strokes)
        {
            if (displayImage == null) { Debug.LogWarning("[Observe] displayImage is NULL"); return; }

            int w = Mathf.RoundToInt(displayImage.rectTransform.rect.width);
            int h = Mathf.RoundToInt(displayImage.rectTransform.rect.height);
            if (w <= 0 || h <= 0) { w = 600; h = 300; }

            if (_displayTexture == null || _displayTexture.width != w || _displayTexture.height != h)
            {
                if (_displayTexture != null) Object.Destroy(_displayTexture);
                _displayTexture = new Texture2D(w, h);
            }

            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            _displayTexture.SetPixels(pixels);

            foreach (var stroke in strokes)
            {
                for (int i = 0; i < stroke.Count; i++)
                {
                    int px = Mathf.RoundToInt(stroke[i].x * w);
                    int py = Mathf.RoundToInt(stroke[i].y * h);
                    DrawDot(px, py, 3);
                    if (i > 0)
                    {
                        int prevX = Mathf.RoundToInt(stroke[i - 1].x * w);
                        int prevY = Mathf.RoundToInt(stroke[i - 1].y * h);
                        DrawSegment(prevX, prevY, px, py);
                    }
                }
            }

            _displayTexture.Apply();
            var rect = new Rect(0, 0, w, h);
            displayImage.sprite = Sprite.Create(_displayTexture, rect, new Vector2(0.5f, 0.5f));
        }

        private void DrawDot(int cx, int cy, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (dx * dx + dy * dy > radius * radius) continue;
                    int px = cx + dx, py = cy + dy;
                    if (px >= 0 && px < _displayTexture.width && py >= 0 && py < _displayTexture.height)
                        _displayTexture.SetPixel(px, py, Color.black);
                }
        }

        private void DrawSegment(int x0, int y0, int x1, int y1)
        {
            float dist = Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1));
            int steps = Mathf.Max(1, Mathf.CeilToInt(dist));
            for (int s = 0; s <= steps; s++)
            {
                float t = (float)s / steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x0, x1, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y0, y1, t));
                DrawDot(x, y, 3);
            }
        }

        private void Refresh()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var runner = NetworkBootstrap.Instance?.Runner;
            if (pm == null || runner == null) return;

            var remaining = pm.PhaseTimer.RemainingTime(pm.Runner);
            if (countdownLabel != null)
            {
                countdownLabel.text = remaining.HasValue
                    ? Mathf.CeilToInt(remaining.Value).ToString()
                    : "—";
            }

            if (hintLabel != null)
            {
                if (pm.TryGetAssignment(runner.LocalPlayer, out var assignment))
                    hintLabel.text = string.Format(hintFormat, assignment.AlbumOriginSlotIndex);
                else
                    hintLabel.text = "Quan sát bức vẽ...";
            }
        }
    }
}
