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
            Debug.Log($"[AlbumStore] Spawned (Id={Object?.Id}, HasStateAuthority={HasStateAuthority})");

            ServiceLocator.Register<AlbumStore>(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            Debug.Log($"[AlbumStore] Despawned (Id={Object?.Id})");

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

            string existing = entry.Prompt.ToString();
            if (roleIndex == 0 || string.IsNullOrEmpty(existing))
            {
                entry.Prompt = prompt;
                entry.OriginPlayer = player;
                Entries.Set(idx, entry);
            }

            Debug.Log($"[AlbumStore] Rpc_SubmitPrompt: role={roleIndex}, chainLink={assignment.ChainLinkIndex}, originSlot={originSlot}, text=\"{entry.Prompt}\"");

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
            if (assignment.PairRole == 0)
                entry.WorkerPlayer = player;
            else
                entry.WorkerPlayer2 = player; Entries.Set(idx, entry);

            ServiceLocator.Get<PlayerRegistry>()?.SetSubmittedPhase(player, true);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_SubmitFinalGuess(byte originSlot, NetworkString<_16> guess, byte roleIndex, RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            var pm = ServiceLocator.Get<Phases.PhaseManager>();
            if (pm == null || pm.CurrentPhase != Phases.PhaseType.FinalGuess) return;
            var player = info.Source;
            if (!pm.TryGetAssignment(player, out var assignment)) return;
            if (assignment.AlbumOriginSlotIndex != originSlot) return;

            // FinalGuess luôn lưu ở link cuối cùng (LinksPerChain - 1).
            byte finalLink = (byte)(LinksPerChain - 1);
            var idx = IndexOf(finalLink, originSlot);
            var entry = Entries.Get(idx);

            // Param vẫn là NetworkString<_64> để không phá vỡ caller; lưu xuống field _32 (cắt bớt nếu quá dài).
            if (roleIndex == 0)
            {
                entry.GuessRole0 = guess;
            }
            else if (roleIndex == 1)
            {
                entry.GuessRole1 = guess;
            }

            entry.WorkerPlayer = player; // LƯU LẠI TÁC GIẢ ĐỂ REVEAL KHÔNG BỊ TRỐNG
            Entries.Set(idx, entry);
            Debug.Log($"[AlbumStore] Rpc_SubmitFinalGuess: role={roleIndex}, finalLink={finalLink}, originSlot={originSlot}, guess=\"{guess}\"");

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

        [ContextMenu("Debug Dump Album")]
        public void DebugDumpAlbum()
        {
            Debug.Log($"==== ALBUM DUMP (PlayerCount: {PlayerCount}, Links: {LinksPerChain}) ====");
            for (byte chain = 0; chain < PlayerCount; chain++)
            {
                var originPlayer = GetEntry(0, chain).OriginPlayer;
                Debug.Log($"-- Album {chain} (Origin: {originPlayer}) --");
                for (byte link = 0; link < LinksPerChain; link++)
                {
                    var entry = GetEntry(link, chain);
                    Debug.Log($"   Link {link}: Worker={entry.WorkerPlayer}, Worker2={entry.WorkerPlayer2}, Prompt='{entry.Prompt}', Guess0='{entry.GuessRole0}', Guess1='{entry.GuessRole1}', Strokes={entry.DrawingStrokes}");
                }
            }
        }
    }
}
