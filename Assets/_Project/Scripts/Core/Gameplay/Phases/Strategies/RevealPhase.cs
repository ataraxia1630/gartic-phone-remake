using Fusion;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using UnityEngine;

namespace InkEcho.Network.Phases.Strategies
{
    public class RevealPhase : PhaseStrategyBase
    {
        public override PhaseType Type => PhaseType.Reveal;

        public override void OnEnter(PhaseManager manager)
        {
            manager.SetRevealAlbumIndex(0);
            ResetTimer(manager);
            Debug.Log("[Phase] Reveal entered");
        }

        public override void Tick(PhaseManager manager)
        {
            if (!manager.PhaseTimer.Expired(manager.Runner)) return;

            var album = ServiceLocator.Get<AlbumStore>();
            var totalAlbums = album != null ? album.PlayerCount : (byte)0;
            var nextIndex = (byte)(manager.RevealAlbumIndex + 1);

            if (totalAlbums == 0 || nextIndex >= totalAlbums)
            {
                manager.AdvancePhase();
                return;
            }

            manager.SetRevealAlbumIndex(nextIndex);
            ResetTimer(manager);
        }

        private void ResetTimer(PhaseManager manager)
        {
            var seconds = manager.ResolveDuration(PhaseType.Reveal);
            if (seconds > 0f) manager.PhaseTimer = TickTimer.CreateFromSeconds(manager.Runner, seconds);
        }
    }
}
