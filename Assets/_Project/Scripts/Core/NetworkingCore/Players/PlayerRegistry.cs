using System.Collections.Generic;
using Fusion;
using InkEcho.Network.Core;

namespace InkEcho.Network.Players
{
    public class PlayerRegistry : NetworkBehaviour, IPlayerLeft
    {
        public const int MaxPlayers = 8;

        [Networked, Capacity(MaxPlayers)]
        public NetworkDictionary<PlayerRef, PlayerSlotData> Slots => default;

        public override void Spawned()
        {
            ServiceLocator.Register<PlayerRegistry>(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ServiceLocator.Unregister<PlayerRegistry>(this);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_RegisterPlayer(NetworkString<_16> displayName, RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            var player = info.Source;

            if (Slots.TryGet(player, out var existing))
            {
                existing.DisplayName = displayName;
                existing.IsConnected = true;
                Slots.Set(player, existing);
                return;
            }

            var slot = PlayerSlotData.New(displayName.Value, (byte)Slots.Count);
            Slots.Set(player, slot);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void Rpc_SetReady(NetworkBool ready, RpcInfo info = default)
        {
            if (!HasStateAuthority) return;
            var player = info.Source;
            if (!Slots.TryGet(player, out var slot)) return;
            slot.IsReady = ready;
            Slots.Set(player, slot);
        }

        public void SetSubmittedPhase(PlayerRef player, bool submitted)
        {
            if (!HasStateAuthority) return;
            if (!Slots.TryGet(player, out var slot)) return;
            slot.HasSubmittedPhase = submitted;
            Slots.Set(player, slot);
        }

        public void ResetSubmittedFlags()
        {
            if (!HasStateAuthority) return;
            var keys = new List<PlayerRef>();
            foreach (var pair in Slots) keys.Add(pair.Key);
            foreach (var key in keys)
            {
                var slot = Slots.Get(key);
                slot.HasSubmittedPhase = false;
                Slots.Set(key, slot);
            }
        }

        public void PlayerLeft(PlayerRef player)
        {
            if (!HasStateAuthority) return;
            if (!Slots.TryGet(player, out var slot)) return;
            slot.IsConnected = false;
            Slots.Set(player, slot);
        }

        public int ConnectedCount()
        {
            int n = 0;
            foreach (var pair in Slots) if (pair.Value.IsConnected) n++;
            return n;
        }

        public int GetPlayerCount() => ConnectedCount();

        public bool AreAllConnectedReady()
        {
            int connected = 0, ready = 0;
            foreach (var pair in Slots)
            {
                if (!pair.Value.IsConnected) continue;
                connected++;
                if (pair.Value.IsReady) ready++;
            }
            return connected > 0 && ready == connected;
        }

        public bool AreAllReady() => AreAllConnectedReady();

        public bool AreAllConnectedSubmitted()
        {
            int connected = 0, submitted = 0;
            foreach (var pair in Slots)
            {
                if (!pair.Value.IsConnected) continue;
                connected++;
                if (pair.Value.HasSubmittedPhase) submitted++;
            }
            return connected > 0 && submitted == connected;
        }

        public List<PlayerRef> GetOrderedPlayers()
        {
            var list = new List<PlayerRef>();
            foreach (var pair in Slots) list.Add(pair.Key);
            list.Sort((a, b) => Slots.Get(a).SlotIndex.CompareTo(Slots.Get(b).SlotIndex));
            return list;
        }

        public bool TryGetSlot(PlayerRef player, out PlayerSlotData slot)
        {
            return Slots.TryGet(player, out slot);
        }
    }
}
