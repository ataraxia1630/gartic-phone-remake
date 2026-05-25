using InkEcho.Gameplay.Data;
using InkEcho.Network.Core;
using InkEcho.Network.Phases;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InkEcho.Gameplay.UI
{
    public class ObserveDrawingDisplay : MonoBehaviour
    {
        [SerializeField] private RawImage displayTarget;
        [SerializeField] private float drawCanvasAspect = 16f / 9f;

        private Texture2D _tex;
        private bool _rendered;

        private void OnEnable()
        {
            _rendered = false;
            if (displayTarget == null)
                displayTarget = GetComponentInChildren<RawImage>(true);
        }

        private void Update()
        {
            if (!_rendered)
                TryRender();
        }

        private void OnDisable()
        {
            _rendered = false;
            if (displayTarget != null) displayTarget.texture = null;
            if (_tex != null) { Destroy(_tex); _tex = null; }
        }

        private void TryRender()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var runner = NetworkBootstrap.Instance?.Runner;
            if (pm == null || runner == null) return;

            if (!pm.TryGetAssignment(runner.LocalPlayer, out var assignment)) return;

            int prevLink = assignment.ChainLinkIndex - 1;
            if (prevLink < 0) { _rendered = true; return; }

            var strokes = DrawingStrokeStore.GetStrokes(prevLink, assignment.AlbumOriginSlotIndex);
            if (strokes == null || strokes.Count == 0) return;

            var colors = DrawingStrokeStore.GetStrokeColors(prevLink, assignment.AlbumOriginSlotIndex);
            RenderToTexture(strokes, colors);
            _rendered = true;
        }

        private void RenderToTexture(IReadOnlyList<List<Vector3>> strokes, IReadOnlyList<Color> strokeColors)
        {
            if (displayTarget == null) return;

            int w = Mathf.RoundToInt(displayTarget.rectTransform.rect.width);
            int h = Mathf.RoundToInt(displayTarget.rectTransform.rect.height);
            int texW = Mathf.RoundToInt(displayTarget.rectTransform.rect.width);
            int texH = Mathf.RoundToInt(displayTarget.rectTransform.rect.height);
            if (texW <= 0 || texH <= 0) { texW = 512; texH = 512; }

            if (_tex == null || _tex.width != texW || _tex.height != texH)
            {
                if (_tex != null) Destroy(_tex);
                _tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            }

            var pixels = new Color[texW * texH];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            _tex.SetPixels(pixels);
            // Letterbox/pillarbox: fit drawing canvas aspect into the texture without distortion.
            // Strokes are stored as UV [0,1] relative to drawCanvasAspect (e.g. 16:9).
            float texAspect = (float)texW / texH;
            int drawW, drawH, offsetX, offsetY;
            if (drawCanvasAspect > texAspect)
            {
                // Drawing wider than display → fit width, add bars top/bottom
                drawW = texW;
                drawH = Mathf.RoundToInt(texW / drawCanvasAspect);
                offsetX = 0;
                offsetY = (texH - drawH) / 2;
            }
            else
            {
                // Drawing taller than display → fit height, add bars left/right
                drawH = texH;
                drawW = Mathf.RoundToInt(texH * drawCanvasAspect);
                offsetX = (texW - drawW) / 2;
                offsetY = 0;
            }

            for (int si = 0; si < strokes.Count; si++)
            {
                var stroke = strokes[si];
                var color = si < strokeColors.Count ? strokeColors[si] : Color.black;
                for (int i = 0; i < stroke.Count; i++)
                {
                    int px = offsetX + Mathf.RoundToInt(stroke[i].x * drawW);
                    int py = offsetY + Mathf.RoundToInt(stroke[i].y * drawH);
                    PaintDot(px, py, 1, color);
                    if (i > 0)
                    {
                        int x0 = offsetX + Mathf.RoundToInt(stroke[i - 1].x * drawW);
                        int y0 = offsetY + Mathf.RoundToInt(stroke[i - 1].y * drawH);
                        PaintSegment(x0, y0, px, py, color);
                    }
                }
            }

            _tex.Apply();
            displayTarget.texture = _tex;
            displayTarget.color = Color.white;
        }

        private void PaintDot(int cx, int cy, int r, Color color)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (dx * dx + dy * dy > r * r) continue;
                    int px = cx + dx, py = cy + dy;
                    if (px >= 0 && px < _tex.width && py >= 0 && py < _tex.height)
                        _tex.SetPixel(px, py, color);
                }
        }

        private void PaintSegment(int x0, int y0, int x1, int y1, Color color)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1))));
            for (int s = 0; s <= steps; s++)
            {
                float t = (float)s / steps;
                PaintDot(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), 1, color);
            }
        }
    }
}