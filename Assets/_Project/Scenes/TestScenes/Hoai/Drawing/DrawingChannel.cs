using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using InkEcho.Network.Players;
using UnityEngine;

namespace InkEcho.Hoai.Drawing
{
    // Side-channel transport + local cache cho PNG drawings.
    // [Networked] không chứa được full PNG nên dùng SendReliableDataToPlayer (Fusion tự chunk).
    // - Local worker nộp PNG → gửi reliable data tới State Authority.
    // - State Authority lưu canonical + fanout cho mọi client khác.
    // - Mỗi client decode PNG → Texture2D lazy khi UI cần.
    public class DrawingChannel : NetworkBehaviour
    {
        public struct DrawingRecord
        {
            public byte ChainLink;
            public byte OriginSlot;
            public PlayerRef Author;
            public byte[] Png;
            public Texture2D CachedTexture;
        }

        private const byte MSG_SUBMIT = 1; // worker → host
        private const byte MSG_FANOUT = 2; // host → other clients

        private readonly Dictionary<int, DrawingRecord> _records = new Dictionary<int, DrawingRecord>(64);

        public event Action<byte, byte, PlayerRef> OnDrawingReady;

        public override void Spawned()
        {
            ServiceLocator.Register<DrawingChannel>(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ServiceLocator.Unregister<DrawingChannel>(this);
            foreach (var rec in _records.Values)
            {
                if (rec.CachedTexture != null) UnityEngine.Object.Destroy(rec.CachedTexture);
            }
            _records.Clear();
        }

        public static int MakeIndex(byte link, byte slot) => (link << 8) | slot;

        public bool TryGetDrawing(byte link, byte slot, out Texture2D texture, out PlayerRef author)
        {
            if (_records.TryGetValue(MakeIndex(link, slot), out var rec) && rec.Png != null && rec.Png.Length > 0)
            {
                if (rec.CachedTexture == null)
                {
                    rec.CachedTexture = new Texture2D(2, 2);
                    rec.CachedTexture.LoadImage(rec.Png);
                    _records[MakeIndex(link, slot)] = rec;
                }
                texture = rec.CachedTexture;
                author = rec.Author;
                return true;
            }

            texture = null;
            author = PlayerRef.None;
            return false;
        }

        // Gọi từ local worker khi nộp bài Draw phase.
        public void SubmitLocalDrawing(byte chainLink, byte originSlot, byte[] png)
        {
            if (png == null || png.Length == 0) return;
            if (Runner == null) return;

            var localPlayer = Runner.LocalPlayer;

            // Lưu local trước (worker cũng cần nhìn lại được).
            StoreRecord(chainLink, originSlot, localPlayer, png);

            // Báo AlbumStore qua RPC để track "đã nộp" + author.
            var album = ServiceLocator.Get<AlbumStore>();
            if (album != null)
            {
                ulong fingerprint = ComputeFingerprint(png);
                album.Rpc_SubmitDrawing(originSlot, fingerprint, (ushort)Mathf.Min(png.Length, ushort.MaxValue));
            }

            // Nếu chính mình là State Authority → fanout luôn cho client khác.
            if (HasStateAuthority)
            {
                FanoutToAll(chainLink, originSlot, localPlayer, png);
                return;
            }

            // Worker thường → gửi reliable data về State Authority.
            var hostPlayer = ResolveStateAuthorityPlayer();
            if (hostPlayer == PlayerRef.None) return;

            var key = BuildKey(MSG_SUBMIT, chainLink, originSlot);
            Runner.SendReliableDataToPlayer(hostPlayer, key, png);
        }

        // Handler cho OnReliableDataReceived (gọi từ NetworkBootstrap).
        public void HandleReliableData(PlayerRef sender, ReliableKey key, ArraySegment<byte> data)
        {
            if (!TryParseKey(key, out byte msgType, out byte link, out byte slot)) return;

            var bytes = new byte[data.Count];
            Buffer.BlockCopy(data.Array, data.Offset, bytes, 0, data.Count);

            switch (msgType)
            {
                case MSG_SUBMIT:
                    // Chỉ State Authority chấp nhận submit.
                    if (!HasStateAuthority) return;
                    StoreRecord(link, slot, sender, bytes);
                    FanoutToAll(link, slot, sender, bytes);
                    break;

                case MSG_FANOUT:
                    // Tin từ host, author = đính trong record do host gán; nhưng reliable data không
                    // mang author riêng → lookup AlbumStore.WorkerPlayer cho cặp (link, slot).
                    var author = ResolveAuthor(link, slot);
                    StoreRecord(link, slot, author, bytes);
                    break;
            }
        }

        private void FanoutToAll(byte link, byte slot, PlayerRef author, byte[] png)
        {
            if (Runner == null) return;
            var key = BuildKey(MSG_FANOUT, link, slot);
            foreach (var p in Runner.ActivePlayers)
            {
                if (p == Runner.LocalPlayer) continue; // host đã có rồi
                if (p == author) continue;             // worker cũng đã có rồi
                Runner.SendReliableDataToPlayer(p, key, png);
            }
        }

        private void StoreRecord(byte link, byte slot, PlayerRef author, byte[] png)
        {
            var idx = MakeIndex(link, slot);
            if (_records.TryGetValue(idx, out var old) && old.CachedTexture != null)
            {
                UnityEngine.Object.Destroy(old.CachedTexture);
            }
            _records[idx] = new DrawingRecord
            {
                ChainLink = link,
                OriginSlot = slot,
                Author = author,
                Png = png,
                CachedTexture = null,
            };
            OnDrawingReady?.Invoke(link, slot, author);
        }

        private PlayerRef ResolveAuthor(byte link, byte slot)
        {
            var album = ServiceLocator.Get<AlbumStore>();
            if (album == null) return PlayerRef.None;
            return album.GetEntry(link, slot).WorkerPlayer;
        }

        private PlayerRef ResolveStateAuthorityPlayer()
        {
            // Host trong Shared Mode = master client. Object.StateAuthority cho biết ai đang giữ authority của object này.
            if (Object != null && Object.StateAuthority.IsRealPlayer) return Object.StateAuthority;
            return PlayerRef.None;
        }

        private static ReliableKey BuildKey(byte msgType, byte link, byte slot)
        {
            // Pack (msgType, link, slot) vào word0: bit 16..23 = msgType, bit 8..15 = link, bit 0..7 = slot.
            int word0 = (msgType << 16) | (link << 8) | slot;
            return ReliableKey.FromInts(word0, 0, 0, 0);
        }

        private static bool TryParseKey(ReliableKey key, out byte msgType, out byte link, out byte slot)
        {
            key.GetInts(out int word0, out _, out _, out _);
            msgType = (byte)((word0 >> 16) & 0xFF);
            link = (byte)((word0 >> 8) & 0xFF);
            slot = (byte)(word0 & 0xFF);
            return msgType == MSG_SUBMIT || msgType == MSG_FANOUT;
        }

        private static ulong ComputeFingerprint(byte[] data)
        {
            // FNV-1a 64-bit để track "đã nộp gì". Không cần crypto-strong.
            ulong hash = 1469598103934665603UL;
            unchecked
            {
                for (int i = 0; i < data.Length; i++)
                {
                    hash ^= data[i];
                    hash *= 1099511628211UL;
                }
            }
            return hash;
        }
    }
}
