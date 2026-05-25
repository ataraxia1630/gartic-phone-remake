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

            RenderToTexture(strokes);
            _rendered = true;
        }

        private void RenderToTexture(IReadOnlyList<List<Vector3>> strokes)
        {
            if (displayTarget == null) return;

            int w = Mathf.RoundToInt(displayTarget.rectTransform.rect.width);
            int h = Mathf.RoundToInt(displayTarget.rectTransform.rect.height);
            if (w <= 0 || h <= 0) { w = 512; h = 512; }

            if (_tex == null || _tex.width != w || _tex.height != h)
            {
                if (_tex != null) Destroy(_tex);
                _tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            }

            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            _tex.SetPixels(pixels);

            foreach (var stroke in strokes)
            {
                for (int i = 0; i < stroke.Count; i++)
                {
                    int px = Mathf.RoundToInt(stroke[i].x * w);
                    int py = Mathf.RoundToInt(stroke[i].y * h);
                    PaintDot(px, py, 3);
                    if (i > 0)
                    {
                        int x0 = Mathf.RoundToInt(stroke[i - 1].x * w);
                        int y0 = Mathf.RoundToInt(stroke[i - 1].y * h);
                        PaintSegment(x0, y0, px, py);
                    }
                }
            }

            _tex.Apply();
            displayTarget.texture = _tex;
            displayTarget.color = Color.white;
        }

        private void PaintDot(int cx, int cy, int r)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (dx * dx + dy * dy > r * r) continue;
                    int px = cx + dx, py = cy + dy;
                    if (px >= 0 && px < _tex.width && py >= 0 && py < _tex.height)
                        _tex.SetPixel(px, py, Color.black);
                }
        }

        private void PaintSegment(int x0, int y0, int x1, int y1)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(x0, y0), new Vector2(x1, y1))));
            for (int s = 0; s <= steps; s++)
            {
                float t = (float)s / steps;
                PaintDot(Mathf.RoundToInt(Mathf.Lerp(x0, x1, t)), Mathf.RoundToInt(Mathf.Lerp(y0, y1, t)), 3);
            }
        }
    }
}