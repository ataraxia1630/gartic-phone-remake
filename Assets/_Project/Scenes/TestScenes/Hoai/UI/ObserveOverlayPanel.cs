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

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
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
