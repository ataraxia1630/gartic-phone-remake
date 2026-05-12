using Fusion;
using InkEcho.Network.Core;
using InkEcho.Network.Players;
using UnityEngine;

namespace InkEcho.Network.Phases.Strategies
{
    public class ObservePhase : PhaseStrategyBase
    {
        public override PhaseType Type => PhaseType.Observe;

        public override void OnEnter(PhaseManager manager)
        {
            var seconds = manager.ResolveDuration(PhaseType.Observe);
            if (seconds > 0f) manager.PhaseTimer = TickTimer.CreateFromSeconds(manager.Runner, seconds);
            var registry = ServiceLocator.Get<PlayerRegistry>();
            registry?.ResetSubmittedFlags();
            Debug.Log("[Phase] Observe entered");
        }

        public override void Tick(PhaseManager manager)
        {
            if (manager.PhaseTimer.Expired(manager.Runner)) manager.AdvancePhase();
        }
    }
}
