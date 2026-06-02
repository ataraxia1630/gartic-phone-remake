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
        [SerializeField] private RawImage rawDisplayImage;  // preferred — assign in Inspector
        [SerializeField] private Image displayImage;        // fallback if no RawImage
        private bool _rendered;
        private bool _loggedOnce;
        private Texture2D _displayTexture;

        private void OnEnable()
        {
            _rendered = false;
            _loggedOnce = false;
            if (rawDisplayImage == null)
                rawDisplayImage = GetComponentInChildren<RawImage>(true);
            if (displayImage == null)
                displayImage = GetComponentInChildren<Image>(true);
            Debug.Log($"[Observe] OnEnable: rawImage={(rawDisplayImage != null ? rawDisplayImage.gameObject.name : "NULL")}, image={(displayImage != null ? displayImage.gameObject.name : "NULL")}");
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
            _loggedOnce = false;
            if (rawDisplayImage != null) rawDisplayImage.texture = null;
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
                if (!_loggedOnce) Debug.LogWarning($"[Observe] TryGetAssignment failed. RoundIndex={pm.RoundIndex}, Phase={pm.CurrentPhase}");
                _loggedOnce = true;
                return;
            }

            int prevChainLink = assignment.ChainLinkIndex - 1;
            if (!_loggedOnce)
                Debug.Log($"[Observe] Assignment: chainLink={assignment.ChainLinkIndex}, originSlot={assignment.AlbumOriginSlotIndex}, prevChainLink={prevChainLink}");

            if (prevChainLink < 0)
            {
                _loggedOnce = true;
                return;
            }

            var strokes = DrawingStrokeStore.GetStrokes(prevChainLink, assignment.AlbumOriginSlotIndex);
            if (!_loggedOnce)
                Debug.Log($"[Observe] GetStrokes({prevChainLink}, {assignment.AlbumOriginSlotIndex}) => {strokes?.Count ?? 0} strokes");
            _loggedOnce = true;

            if (strokes == null || strokes.Count == 0) return;

            RenderStrokesToTexture(strokes);
            _rendered = true;
            Debug.Log("[Observe] Drawing rendered successfully");
        }

        private void RenderStrokesToTexture(IReadOnlyList<List<Vector3>> strokes)
        {
            RectTransform rt = rawDisplayImage != null
                ? rawDisplayImage.rectTransform
                : displayImage != null ? displayImage.rectTransform : null;

            if (rt == null) { Debug.LogWarning("[Observe] No display component found"); return; }

            int w = Mathf.RoundToInt(rt.rect.width);
            int h = Mathf.RoundToInt(rt.rect.height);
            Debug.Log($"[Observe] RenderStrokesToTexture: rect={w}x{h}, strokes={strokes.Count}");
            if (w <= 0 || h <= 0) { w = 512; h = 512; }

            if (_displayTexture == null || _displayTexture.width != w || _displayTexture.height != h)
            {
                if (_displayTexture != null) Object.Destroy(_displayTexture);
                _displayTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            }

            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            _displayTexture.SetPixels(pixels);

            int dotCount = 0;
            foreach (var stroke in strokes)
            {
                for (int i = 0; i < stroke.Count; i++)
                {
                    int px = Mathf.RoundToInt(stroke[i].x * w);
                    int py = Mathf.RoundToInt(stroke[i].y * h);
                    DrawDot(px, py, 3);
                    dotCount++;
                    if (i > 0)
                    {
                        int prevX = Mathf.RoundToInt(stroke[i - 1].x * w);
                        int prevY = Mathf.RoundToInt(stroke[i - 1].y * h);
                        DrawSegment(prevX, prevY, px, py);
                    }
                }
            }

            _displayTexture.Apply();
            Debug.Log($"[Observe] Texture drawn: {dotCount} dots total");

            if (rawDisplayImage != null)
            {
                rawDisplayImage.texture = _displayTexture;
                rawDisplayImage.color = Color.white;
                rawDisplayImage.enabled = true;
            }
            else if (displayImage != null)
            {
                displayImage.type = Image.Type.Simple;
                displayImage.color = Color.white;
                displayImage.preserveAspect = false;
                displayImage.enabled = true;
                displayImage.sprite = Sprite.Create(_displayTexture, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f));
            }
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
