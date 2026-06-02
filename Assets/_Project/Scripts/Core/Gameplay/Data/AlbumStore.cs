using Fusion;
using InkEcho.Gameplay.Data;
using InkEcho.Network.Core;
using InkEcho.Network.Players;
using UnityEngine;

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
            DrawingStrokeStore.ClearStrokes();
        }

        private int IndexOf(int chainLink, int originSlot) => chainLink * PlayerCount + originSlot;

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_SubmitPrompt(byte originSlot, NetworkString<_64> prompt, byte roleIndex, RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            var pm = ServiceLocator.Get<Phases.PhaseManager>();
            if (pm == null || pm.CurrentPhase != Phases.PhaseType.Prompt) return;
            var player = info.Source;
            if (!pm.TryGetAssignment(player, out var assignment)) return;
            if (assignment.AlbumOriginSlotIndex != originSlot) return;

            var idx = IndexOf(assignment.ChainLinkIndex, originSlot);
            var entry = Entries.Get(idx);

            string currentText = entry.Prompt.ToString();
            string newText = prompt.ToString();

            if (roleIndex == 1)
            {
                entry.Prompt = new NetworkString<_64>(string.IsNullOrEmpty(currentText) ? newText : currentText + " " + newText);
            }
            else
            {
                entry.Prompt = new NetworkString<_64>(string.IsNullOrEmpty(currentText) ? newText : newText + " " + currentText);
            }

            entry.OriginPlayer = player;
            Entries.Set(idx, entry);

            var registry = ServiceLocator.Get<PlayerRegistry>();
            registry?.SetSubmittedPhase(player, true);
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
        public void Rpc_SubmitFinalGuess(byte originSlot, NetworkString<_64> guess, byte roleIndex, RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            var pm = ServiceLocator.Get<Phases.PhaseManager>();
            if (pm == null || pm.CurrentPhase != Phases.PhaseType.FinalGuess) return;
            var player = info.Source;
            if (!pm.TryGetAssignment(player, out var assignment)) return;
            if (assignment.AlbumOriginSlotIndex != originSlot) return;

            var idx = IndexOf(assignment.ChainLinkIndex, originSlot);
            var entry = Entries.Get(idx);

            // Param vẫn là NetworkString<_64> để không phá vỡ caller; lưu xuống field _32 (cắt bớt nếu quá dài).
            if (roleIndex == 0)
            {
                entry.GuessRole0 = new NetworkString<_32>(guess.ToString());
            }
            else if (roleIndex == 1)
            {
                entry.GuessRole1 = new NetworkString<_32>(guess.ToString());
            }

            Entries.Set(idx, entry);

            var registry = ServiceLocator.Get<PlayerRegistry>();
            registry?.SetSubmittedPhase(player, true);
        }

        public AlbumEntry GetEntry(int chainLink, int originSlot)
        {
            var idx = IndexOf(chainLink, originSlot);
            return Entries.Get(idx);
        }

        // Host-side helper: set WorkerPlayer only if it has not been assigned yet.
        public void SetWorkerIfMissing(int chainLink, int originSlot, PlayerRef player)
        {
            if (!HasStateAuthority) return;
            var idx = IndexOf(chainLink, originSlot);
            var entry = Entries.Get(idx);
            if (entry.WorkerPlayer.IsRealPlayer) return;
            entry.WorkerPlayer = player;
            Entries.Set(idx, entry);
        }
    }
}
