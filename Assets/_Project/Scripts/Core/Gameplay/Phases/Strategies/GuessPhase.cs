using Fusion;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Players;
using UnityEngine;

namespace InkEcho.Network.Phases.Strategies
{
    public class GuessPhase : PhaseStrategyBase
    {
        public override PhaseType Type => PhaseType.Guess;

        //public override void OnEnter(PhaseManager manager)
        //{
        //    var seconds = manager.ResolveDuration(PhaseType.Guess);
        //    if (seconds > 0f) manager.PhaseTimer = Fusion.TickTimer.CreateFromSeconds(manager.Runner, seconds);
        //    var registry = ServiceLocator.Get<PlayerRegistry>();
        //    registry?.ResetSubmittedFlags();
        //    Debug.Log("[Phase] Guess entered");
        //}

        public override void OnEnter(PhaseManager manager)
        {
            var seconds = manager.ResolveDuration(PhaseType.Guess);
            if (seconds > 0f) manager.PhaseTimer = Fusion.TickTimer.CreateFromSeconds(manager.Runner, seconds);
            var registry = ServiceLocator.Get<PlayerRegistry>();
            registry?.ResetSubmittedFlags();
            Debug.Log("[Phase] Guess entered");

            // Show previous drawing so the guesser can see what they need to guess
            if (manager.RoundIndex > 0)
                ShowPreviousContent(manager);
        }

        private void ShowPreviousContent(PhaseManager manager)
        {
            var uiHelper = Object.FindObjectOfType<InkEcho.UI.PhaseUIHelper>();
            if (uiHelper != null)
            {
                uiHelper.ShowDrawing(manager.RoundIndex - 1, manager.RevealAlbumIndex);
                Debug.Log($"[GuessPhase] Showing drawing from round {manager.RoundIndex - 1}");
            }
        }

        public override void Tick(PhaseManager manager)
        {
            var registry = ServiceLocator.Get<PlayerRegistry>();
            if (registry != null && registry.AreAllConnectedSubmitted())
            {
                manager.AdvancePhase();
                return;
            }

            if (manager.PhaseTimer.Expired(manager.Runner)) manager.AdvancePhase();
        }
    }
}
