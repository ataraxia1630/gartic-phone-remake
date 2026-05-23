using InkEcho.Gameplay.Data;
using InkEcho.Network.Core;
using InkEcho.Network.Phases;
using TMPro;
using UnityEngine;

namespace InkEcho.Hoai.UI
{
    /// <summary>
    /// Shown during PhaseType.Observe. Counts down and tells the local player
    /// which chain they will draw on next. Drawing input is already gated by
    /// DrawingNetwork so this panel only needs to handle visuals.
    /// </summary>
    public class ObserveOverlayPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI countdownLabel;
        [SerializeField] private TextMeshProUGUI hintLabel;
        [SerializeField] private string hintFormat = "Bạn sẽ vẽ tiếp chain {0} sau...";
        [SerializeField] private DrawingReplayRenderer replayRenderer;
        private bool _rendered;

        private void OnEnable()
        {
            _rendered = false;
            replayRenderer ??= GetComponentInChildren<DrawingReplayRenderer>(true);
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
            replayRenderer?.Clear();
            _rendered = false;
        }
        private void TryRenderDrawing()
        {
            if (replayRenderer == null) { Debug.LogWarning("[Observe] replayRenderer is NULL"); return; }

            var pm = ServiceLocator.Get<PhaseManager>();
            var runner = NetworkBootstrap.Instance?.Runner;
            if (pm == null || runner == null) { Debug.LogWarning("[Observe] PhaseManager or Runner is NULL"); return; }

            if (!pm.TryGetAssignment(runner.LocalPlayer, out var assignment))
            {
                Debug.LogWarning($"[Observe] TryGetAssignment failed. RoundIndex={pm.RoundIndex}, TotalRounds={pm.TotalRounds}");
                return;
            }
            int prevChainLink = assignment.ChainLinkIndex - 1;
            Debug.Log($"[Observe] assignment: ChainLink={assignment.ChainLinkIndex}, OriginSlot={assignment.AlbumOriginSlotIndex} | Looking for strokes at chainLink={prevChainLink}"); if (prevChainLink < 0) { Debug.LogWarning("[Observe] prevChainLink < 0, nothing to show"); return; }

            var strokes = DrawingStrokeStore.GetStrokes(prevChainLink, assignment.AlbumOriginSlotIndex);
            Debug.Log($"[Observe] GetStrokes({prevChainLink}, {assignment.AlbumOriginSlotIndex}) => {(strokes == null ? "null" : strokes.Count + " strokes")}");

            if (strokes == null || strokes.Count == 0) return;

            replayRenderer.RenderStrokes(strokes);
            _rendered = true;
            Debug.Log("[Observe] Drawing rendered successfully");

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
                {
                    hintLabel.text = string.Format(hintFormat, assignment.AlbumOriginSlotIndex);
                }
                else
                {
                    hintLabel.text = "Quan sát bức vẽ...";
                }
            }
        }
    }
}
