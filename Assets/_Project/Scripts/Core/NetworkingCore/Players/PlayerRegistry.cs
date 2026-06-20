using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using InkEcho.Gameplay.Data;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using UnityEngine;

namespace InkEcho.Network.Players
{
    public class PlayerRegistry : NetworkBehaviour, IPlayerLeft
    {
        public const int MaxPlayers = 6;

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
                // Reconnect: giữ tên đã resolved (đã unique), chỉ cập nhật IsConnected
                existing.IsConnected = true;
                Slots.Set(player, existing);
                return;
            }

            var uniqueName = ResolveUniqueName(displayName.Value);
            var slot = PlayerSlotData.New(uniqueName, (byte)Slots.Count);
            Slots.Set(player, slot);
        }

        private string ResolveUniqueName(string requested)
        {
            const int maxLen = 16;
            var taken = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var pair in Slots)
                taken.Add(pair.Value.DisplayName.Value);

            if (!taken.Contains(requested)) return requested;

            for (int i = 2; i <= MaxPlayers + 1; i++)
            {
                var suffix = $"({i})";
                var baseName = requested.Length + suffix.Length > maxLen
                    ? requested.Substring(0, maxLen - suffix.Length)
                    : requested;
                var candidate = baseName + suffix;
                if (!taken.Contains(candidate)) return candidate;
            }
            return requested;
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
            foreach (var pair in Slots)
            {
                if (!pair.Value.IsConnected) continue;
                list.Add(pair.Key);
            }
            list.Sort((a, b) => Slots.Get(a).SlotIndex.CompareTo(Slots.Get(b).SlotIndex));
            return list;
        }

        public bool TryGetSlotIndex(PlayerRef player, out byte slotIndex)
        {
            if (Slots.TryGet(player, out var slot))
            {
                slotIndex = slot.SlotIndex;
                return true;
            }

            slotIndex = byte.MaxValue;
            return false;
        }

        public bool TryGetPlayerBySlotIndex(byte slotIndex, out PlayerRef player)
        {
            foreach (var pair in Slots)
            {
                if (pair.Value.SlotIndex != slotIndex) continue;
                player = pair.Key;
                return true;
            }

            player = PlayerRef.None;
            return false;
        }

        public bool TryGetSlot(PlayerRef player, out PlayerSlotData slot)
        {
            return Slots.TryGet(player, out slot);
        }
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void Rpc_SyncDrawingStroke(byte[] data, byte chainLink, byte originSlot,
    byte colorR, byte colorG, byte colorB, RpcInfo info = default)
        {
            var points = DrawingDataConverter.ByteArrayToViewportPoints(data);
            if (points.Count == 0) return;
            var color = new Color(colorR / 255f, colorG / 255f, colorB / 255f);
            DrawingStrokeStore.StoreStroke(chainLink, originSlot, points, color);
            Debug.Log($"[PlayerRegistry] Stroke synced: chainLink={chainLink}, originSlot={originSlot}, points={points.Count}");
        }

        // Host gọi khi bắt đầu Reveal: mỗi client gửi lại toàn bộ raw PNG mình đang lưu về host.
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void Rpc_RequestRebroadcastToHost(PlayerRef host, RpcInfo info = default)
        {
            var runner = NetworkBootstrap.Instance?.Runner;
            if (runner == null) { Debug.LogWarning("[Rebroadcast] Runner null, bỏ qua"); return; }

            bool isHost = runner.LocalPlayer == host;
            var allPngs = new System.Collections.Generic.List<(byte, byte, byte[])>(DrawingTextureStore.GetAllRawPngs());
            Debug.Log($"[Rebroadcast] RPC nhận: localPlayer={runner.LocalPlayer}, host={host}, isHost={isHost}, pngCount={allPngs.Count}");

            if (isHost) return;

            if (allPngs.Count == 0)
            {
                Debug.LogWarning("[Rebroadcast] Client không có PNG nào trong DrawingTextureStore để gửi!");
                return;
            }

            foreach (var (chainLink, originSlot, png) in allPngs)
            {
                var packet = new byte[png.Length + 3];
                packet[0] = 0xDA;
                packet[1] = chainLink;
                packet[2] = originSlot;
                Array.Copy(png, 0, packet, 3, png.Length);
                // Dùng key encode (chainLink, originSlot) để tránh collision khi gửi nhiều tranh liên tiếp
                var key = ReliableKey.FromInts(0xDA, chainLink, originSlot, 0);
                runner.SendReliableDataToPlayer(host, key, packet);
                Debug.Log($"[Rebroadcast] Gửi tranh → host: chainLink={chainLink}, originSlot={originSlot}, size={png.Length} bytes");
            }
        }
    }
}
