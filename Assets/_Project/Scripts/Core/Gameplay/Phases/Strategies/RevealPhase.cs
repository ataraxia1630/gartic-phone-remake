using InkEcho.Network.Core;
using InkEcho.Network.Players;
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

            // Host yêu cầu tất cả client gửi lại tranh của họ để đảm bảo DrawingTextureStore đầy đủ.
            if (manager.HasStateAuthority)
            {
                var registry = ServiceLocator.Get<PlayerRegistry>();
                registry?.Rpc_RequestRebroadcastToHost(manager.Runner.LocalPlayer);
                Debug.Log("[Phase] Reveal — requested drawing re-broadcast from all clients");
            }

            Debug.Log("[Phase] Reveal — host clicks Next to reveal links step by step");
        }

        // Host drives reveal manually via PhaseManager.RevealNext(). Nothing to tick.
        public override void Tick(PhaseManager manager) { }
    }
}
