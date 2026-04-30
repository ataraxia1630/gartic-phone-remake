using System.Collections.Generic;
using Fusion;
using GarticPhone.Network.Players;
using GarticPhone.Network.Core;
using UnityEngine;

namespace GarticPhone.Network.Data
{
    public class AlbumStore : NetworkBehaviour
    {
        public const int MaxEntries = 64;

        [Networked, Capacity(MaxEntries)]
        public NetworkArray<AlbumEntry> Entries => default;

        [Networked] public byte PlayerCount { get; set; }
        [Networked] public byte TotalRounds { get; set; }

        public override void Spawned()
        {
            ServiceLocator.Register<AlbumStore>(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ServiceLocator.Unregister<AlbumStore>(this);
        }

        public void Init(byte playerCount, byte totalRounds)
        {
            if (!HasStateAuthority) return;
            PlayerCount = playerCount;
            TotalRounds = totalRounds;
            // initialize entries
            for (byte r = 0; r < totalRounds; r++)
            {
                for (byte s = 0; s < playerCount; s++)
                {
                    var idx = IndexOf(r, s);
                    Entries.Set(idx, AlbumEntry.Empty(r, s, PlayerRef.None));
                }
            }
        }

        private int IndexOf(int round, int originSlot) => round * PlayerCount + originSlot;

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_SubmitPrompt(byte originSlot, NetworkString<_32> prompt, RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            var player = info.Source;
            var round = (byte)ServiceLocator.Get<Phases.PhaseManager>().RoundIndex;
            var idx = IndexOf(round, originSlot);
            var entry = Entries.Get(idx);
            entry.Prompt = prompt;
            entry.OriginPlayer = player;
            Entries.Set(idx, entry);

            var registry = ServiceLocator.Get<PlayerRegistry>();
            registry?.SetSubmittedPhase(player, true);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_SubmitDrawing(byte originSlot, ulong hash, ushort strokes, RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            var player = info.Source;
            var round = (byte)ServiceLocator.Get<Phases.PhaseManager>().RoundIndex;
            var idx = IndexOf(round, originSlot);
            var entry = Entries.Get(idx);
            entry.DrawingHash = hash;
            entry.DrawingStrokes = strokes;
            Entries.Set(idx, entry);

            var registry = ServiceLocator.Get<PlayerRegistry>();
            registry?.SetSubmittedPhase(player, true);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_SubmitGuess(byte originSlot, NetworkString<_32> guess, RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            var player = info.Source;
            var round = (byte)ServiceLocator.Get<Phases.PhaseManager>().RoundIndex;
            var idx = IndexOf(round, originSlot);
            var entry = Entries.Get(idx);
            entry.Guess = guess;
            Entries.Set(idx, entry);

            var registry = ServiceLocator.Get<PlayerRegistry>();
            registry?.SetSubmittedPhase(player, true);
        }

        public AlbumEntry GetEntry(int round, int originSlot)
        {
            var idx = IndexOf(round, originSlot);
            return Entries.Get(idx);
        }
    }
}
