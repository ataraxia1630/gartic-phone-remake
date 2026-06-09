using System.Collections.Generic;
using UnityEngine;

namespace InkEcho.Gameplay.Data
{
    public static class DrawingTextureStore
    {
        private static readonly Dictionary<int, Texture2D> _textures = new();
        private static readonly Dictionary<int, byte[]> _rawPngs = new();

        public static void StoreTexture(int chainLink, int originSlot, byte[] pngBytes)
        {
            var key = chainLink * 32 + originSlot;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(pngBytes))
            {
                Object.Destroy(tex);
                Debug.LogWarning($"[DrawingTextureStore] Failed to decode PNG for chainLink={chainLink}, originSlot={originSlot}");
                return;
            }
            if (_textures.TryGetValue(key, out var old)) Object.Destroy(old);
            _textures[key] = tex;
            _rawPngs[key] = pngBytes;
            Debug.Log($"[DrawingTextureStore] Stored texture: chainLink={chainLink}, originSlot={originSlot}, size={pngBytes.Length} bytes");
        }

        public static Texture2D GetTexture(int chainLink, int originSlot)
        {
            _textures.TryGetValue(chainLink * 32 + originSlot, out var tex);
            return tex;
        }

        public static byte[] GetRawPng(int chainLink, int originSlot)
        {
            _rawPngs.TryGetValue(chainLink * 32 + originSlot, out var png);
            return png;
        }

        // Trả về tất cả raw PNG đang được lưu (dùng để re-broadcast về host khi Reveal bắt đầu).
        public static IEnumerable<(byte chainLink, byte originSlot, byte[] png)> GetAllRawPngs()
        {
            foreach (var kv in _rawPngs)
            {
                byte link = (byte)(kv.Key / 32);
                byte slot = (byte)(kv.Key % 32);
                yield return (link, slot, kv.Value);
            }
        }

        public static void Clear()
        {
            foreach (var tex in _textures.Values)
                if (tex != null) Object.Destroy(tex);
            _textures.Clear();
            _rawPngs.Clear();
        }
    }
}