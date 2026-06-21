using Fusion;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Players;
using UnityEngine;

namespace InkEcho.Network.Phases.Strategies
{
    public class DrawPhase : PhaseStrategyBase
    {
        public override PhaseType Type => PhaseType.Draw;

        //public override void OnEnter(PhaseManager manager)
        //{
        //    var seconds = manager.ResolveDuration(PhaseType.Draw);
        //    if (seconds > 0f) manager.PhaseTimer = Fusion.TickTimer.CreateFromSeconds(manager.Runner, seconds);
        //    var registry = ServiceLocator.Get<PlayerRegistry>();
        //    registry?.ResetSubmittedFlags();
        //    Debug.Log("[Phase] Draw entered");
        //}
        public override void OnEnter(PhaseManager manager)
        {
            var seconds = manager.ResolveDuration(PhaseType.Draw);
            if (seconds > 0f) manager.PhaseTimer = Fusion.TickTimer.CreateFromSeconds(manager.Runner, seconds);
            var registry = ServiceLocator.Get<PlayerRegistry>();
            registry?.ResetSubmittedFlags();

            // Ghi sẵn tác giả (WorkerPlayer) cho mỗi album theo assignment xác định.
            // Tránh bug "Vẽ bởi: ?" khi RPC nộp bài tới host trễ (sau khi host đã rời phase Draw)
            // → Rpc_SubmitDrawing bị guard chặn nên WorkerPlayer không kịp set.
            if (manager.HasStateAuthority)
            {
                var album = ServiceLocator.Get<AlbumStore>();
                if (album != null)
                {
                    foreach (var a in manager.GetCurrentAssignments())
                        album.SetWorkerIfMissing(a.ChainLinkIndex, a.AlbumOriginSlotIndex, a.Worker);
                }
            }

            Debug.Log("[Phase] Draw entered");
        }

        public override void Tick(PhaseManager manager)
        {
            if (manager.PhaseTimer.Expired(manager.Runner)) manager.AdvancePhase();
        }
    }
}
