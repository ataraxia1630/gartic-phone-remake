using Fusion;
using GarticPhone.Network.Core;
using GarticPhone.Network.Data;
using GarticPhone.Network.Players;
using UnityEngine;

namespace GarticPhone.Network.Phases.Strategies
{
    public class DrawPhase : PhaseStrategyBase
    {
        public override PhaseType Type => PhaseType.Draw;

        public override void OnEnter(PhaseManager manager)
        {
            var seconds = manager.ResolveDuration(PhaseType.Draw);
            if (seconds > 0f) manager.PhaseTimer = Fusion.TickTimer.CreateFromSeconds(manager.Runner, seconds);
            var registry = ServiceLocator.Get<PlayerRegistry>();
            registry?.ResetSubmittedFlags();
            Debug.Log("[Phase] Draw entered");
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
