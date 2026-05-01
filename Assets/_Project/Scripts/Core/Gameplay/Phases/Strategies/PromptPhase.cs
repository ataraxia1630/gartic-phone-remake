using Fusion;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Players;
using UnityEngine;

namespace InkEcho.Network.Phases.Strategies
{
    public class PromptPhase : PhaseStrategyBase
    {
        public override PhaseType Type => PhaseType.Prompt;

        public override void OnEnter(PhaseManager manager)
        {
            var seconds = manager.ResolveDuration(PhaseType.Prompt);
            if (seconds > 0f) manager.PhaseTimer = Fusion.TickTimer.CreateFromSeconds(manager.Runner, seconds);
            var registry = ServiceLocator.Get<PlayerRegistry>();
            registry?.ResetSubmittedFlags();
            Debug.Log("[Phase] Prompt entered");
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
