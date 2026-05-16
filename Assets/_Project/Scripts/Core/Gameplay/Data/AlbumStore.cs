using Fusion;
using InkEcho.Network.Players;
using InkEcho.Network.Core;

namespace InkEcho.Network.Data
{
    public class AlbumStore : NetworkBehaviour
    {
        public const int MaxLinksPerChain = PlayerRegistry.MaxPlayers + 1;
        public const int MaxEntries = PlayerRegistry.MaxPlayers * MaxLinksPerChain;

        [Networked, Capacity(MaxEntries)]
        public NetworkArray<AlbumEntry> Entries => default;

        [Networked] public byte PlayerCount { get; set; }
        [Networked] public byte LinksPerChain { get; set; }

        public override void Spawned()
        {
            ServiceLocator.Register<AlbumStore>(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ServiceLocator.Unregister<AlbumStore>(this);
        }

        public void Init(byte playerCount)
        {
            if (!HasStateAuthority) return;
            PlayerCount = playerCount;
            LinksPerChain = (byte)(playerCount + 1);
            for (byte link = 0; link < LinksPerChain; link++)
            {
                for (byte slot = 0; slot < playerCount; slot++)
                {
                    var idx = IndexOf(link, slot);
                    Entries.Set(idx, AlbumEntry.Empty(link, slot, PlayerRef.None));
                }
            }
        }

        private int IndexOf(int chainLink, int originSlot) => chainLink * PlayerCount + originSlot;

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_SubmitPrompt(byte originSlot, NetworkString<_32> prompt, RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            var pm = ServiceLocator.Get<Phases.PhaseManager>();
            if (pm == null || pm.CurrentPhase != Phases.PhaseType.Prompt) return;
            var player = info.Source;
            if (!pm.TryGetAssignment(player, out var assignment)) return;
            if (assignment.AlbumOriginSlotIndex != originSlot) return;

            var idx = IndexOf(assignment.ChainLinkIndex, originSlot);
            var entry = Entries.Get(idx);
            entry.Prompt = prompt;
            entry.OriginPlayer = player;
            entry.WorkerPlayer = player;
            Entries.Set(idx, entry);

            ServiceLocator.Get<PlayerRegistry>()?.SetSubmittedPhase(player, true);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_SubmitDrawing(byte originSlot, ulong hash, ushort strokes, RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            var pm = ServiceLocator.Get<Phases.PhaseManager>();
            if (pm == null || pm.CurrentPhase != Phases.PhaseType.Draw) return;
            var player = info.Source;
            if (!pm.TryGetAssignment(player, out var assignment)) return;
            if (assignment.AlbumOriginSlotIndex != originSlot) return;

            var idx = IndexOf(assignment.ChainLinkIndex, originSlot);
            var entry = Entries.Get(idx);
            entry.DrawingHash = hash;
            entry.DrawingStrokes = strokes;
            entry.WorkerPlayer = player;
            Entries.Set(idx, entry);

            ServiceLocator.Get<PlayerRegistry>()?.SetSubmittedPhase(player, true);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_SubmitFinalGuess(byte originSlot, NetworkString<_32> guess, RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            HandleFinalGuess(originSlot, guess, info.Source);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_SubmitGuess(byte originSlot, NetworkString<_32> guess, RpcInfo info = default)
        {
            // Backward-compat: legacy callers route here, accepted only during FinalGuess phase
            if (!HasStateAuthority) return;
            HandleFinalGuess(originSlot, guess, info.Source);
        }

        private void HandleFinalGuess(byte originSlot, NetworkString<_32> guess, PlayerRef player)
        {
            var pm = ServiceLocator.Get<Phases.PhaseManager>();
            if (pm == null || pm.CurrentPhase != Phases.PhaseType.FinalGuess) return;
            if (!pm.TryGetAssignment(player, out var assignment)) return;
            if (assignment.AlbumOriginSlotIndex != originSlot) return;

            var idx = IndexOf(assignment.ChainLinkIndex, originSlot);
            var entry = Entries.Get(idx);
            entry.Guess = guess;
            entry.WorkerPlayer = player;
            Entries.Set(idx, entry);

            ServiceLocator.Get<PlayerRegistry>()?.SetSubmittedPhase(player, true);
        }

        public AlbumEntry GetEntry(int chainLink, int originSlot)
        {
            var idx = IndexOf(chainLink, originSlot);
            return Entries.Get(idx);
        }
    }
}
