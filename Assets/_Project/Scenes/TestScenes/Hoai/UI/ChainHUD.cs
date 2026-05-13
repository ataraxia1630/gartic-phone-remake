using Fusion;
using InkEcho.Network.Core;
using InkEcho.Network.Phases;
using TMPro;
using UnityEngine;

namespace InkEcho.Hoai.UI
{
    public class ChainHUD : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI phaseLabel;
        [SerializeField] private TextMeshProUGUI roundLabel;
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private TextMeshProUGUI assignmentLabel;

        private void Update()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            if (pm == null)
            {
                SetText(phaseLabel, "Phase: —");
                SetText(roundLabel, "Round: —");
                SetText(timerLabel, "Timer: —");
                SetText(assignmentLabel, "Assignment: —");
                return;
            }

            SetText(phaseLabel, $"Phase: {pm.CurrentPhase}");
            SetText(roundLabel, $"Round: {pm.RoundIndex}/{pm.TotalRounds}");

            var runner = NetworkBootstrap.Instance?.Runner;
            if (runner != null && pm.Runner != null)
            {
                var remaining = pm.PhaseTimer.RemainingTime(pm.Runner);
                SetText(timerLabel, remaining.HasValue ? $"Timer: {remaining.Value:F1}s" : "Timer: —");
            }
            else
            {
                SetText(timerLabel, "Timer: —");
            }

            if (runner != null && pm.TryGetAssignment(runner.LocalPlayer, out var assignment))
            {
                SetText(assignmentLabel, $"Chain: {assignment.AlbumOriginSlotIndex} | Link: {assignment.ChainLinkIndex}");
            }
            else
            {
                SetText(assignmentLabel, "Assignment: (none this phase)");
            }
        }

        private static void SetText(TextMeshProUGUI label, string value)
        {
            if (label != null) label.text = value;
        }
    }
}
