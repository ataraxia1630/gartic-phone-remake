using UnityEngine;

namespace InkEcho.Network.Phases.Strategies
{
    public class RevealPhase : PhaseStrategyBase
    {
        public override PhaseType Type => PhaseType.Reveal;

        public override void OnEnter(PhaseManager manager)
        {
            manager.SetRevealAlbumIndex(0);
            manager.SetRevealLinkIndex(0);
            Debug.Log("[Phase] Reveal — host clicks Next to reveal links step by step");
        }

        // Host drives reveal manually via PhaseManager.RevealNext(). Nothing to tick.
        public override void Tick(PhaseManager manager) { }
    }
}
