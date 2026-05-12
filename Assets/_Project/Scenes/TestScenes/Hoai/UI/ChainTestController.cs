using InkEcho.Network.Core;
using InkEcho.Network.Phases;
using UnityEngine;

namespace InkEcho.Hoai.UI
{
    /// <summary>
    /// Drives panel visibility based on the current chain-mode phase.
    /// Drop this on a manager GameObject in the scene, then assign each panel.
    /// </summary>
    public class ChainTestController : MonoBehaviour
    {
        [Header("Panels (root GameObjects)")]
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private GameObject drawPanel;
        [SerializeField] private GameObject observePanel;
        [SerializeField] private GameObject finalGuessPanel;
        [SerializeField] private GameObject revealPanel;
        [SerializeField] private GameObject lobbyOverlay;

        private PhaseType _lastPhase = PhaseType.None;

        private void Update()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var current = pm != null ? pm.CurrentPhase : PhaseType.None;
            if (current == _lastPhase) return;
            _lastPhase = current;
            ApplyVisibility(current);
        }

        private void ApplyVisibility(PhaseType phase)
        {
            Set(promptPanel, phase == PhaseType.Prompt);
            Set(drawPanel, phase == PhaseType.Draw);
            Set(observePanel, phase == PhaseType.Observe);
            Set(finalGuessPanel, phase == PhaseType.FinalGuess);
            Set(revealPanel, phase == PhaseType.Reveal);
            Set(lobbyOverlay, phase == PhaseType.None);
        }

        private static void Set(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
