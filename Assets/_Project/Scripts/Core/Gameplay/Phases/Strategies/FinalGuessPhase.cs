using Fusion;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Players;
using UnityEngine;

namespace InkEcho.Network.Phases.Strategies
{
    public class FinalGuessPhase : PhaseStrategyBase
    {
        public override PhaseType Type => PhaseType.FinalGuess;

        public override void OnEnter(PhaseManager manager)
        {
            var seconds = manager.ResolveDuration(PhaseType.FinalGuess);
            if (seconds > 0f) manager.PhaseTimer = TickTimer.CreateFromSeconds(manager.Runner, seconds);
            var registry = ServiceLocator.Get<PlayerRegistry>();
            registry?.ResetSubmittedFlags();
            Debug.Log("[Phase] FinalGuess entered");

            // Ghi sẵn tác giả guess theo assignment xác định (như DrawPhase) —
            // tránh "?" khi RPC nộp guess tới host trễ sau khi đã rời phase.
            if (manager.HasStateAuthority)
            {
                var album = ServiceLocator.Get<AlbumStore>();
                if (album != null)
                {
                    foreach (var a in manager.GetCurrentAssignments())
                        album.SetWorkerIfMissing(a.ChainLinkIndex, a.AlbumOriginSlotIndex, a.Worker);
                }
            }

            // NEW: Show previous drawing
            if (manager.RoundIndex > 0)
                ShowPreviousContent(manager);
        }

        private void ShowPreviousContent(PhaseManager manager)
        {
            var uiHelper = Object.FindAnyObjectByType<InkEcho.UI.PhaseUIHelper>();
            if (uiHelper != null)
            {
                uiHelper.ShowDrawing(manager.RoundIndex - 1, manager.RevealAlbumIndex);
                Debug.Log($"[FinalGuessPhase] Showing drawing from round {manager.RoundIndex - 1}");
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
