using System.Collections.Generic;
using UnityEngine;

namespace InkEcho.Gameplay.Data
{
    public static class DrawingTextureStore
    {
        private static readonly Dictionary<int, Texture2D> _textures = new();

        public static void StoreTexture(int chainLink, int originSlot, byte[] pngBytes)
        {
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(pngBytes))
            {
                Object.Destroy(tex);
                Debug.LogWarning($"[DrawingTextureStore] Failed to decode PNG for chainLink={chainLink}, originSlot={originSlot}");
                return;
            }
            var key = chainLink * 32 + originSlot;
            if (_textures.TryGetValue(key, out var old)) Object.Destroy(old);
            _textures[key] = tex;
            Debug.Log($"[DrawingTextureStore] Stored texture: chainLink={chainLink}, originSlot={originSlot}, size={pngBytes.Length} bytes");
        }

        public static Texture2D GetTexture(int chainLink, int originSlot)
        {
            _textures.TryGetValue(chainLink * 32 + originSlot, out var tex);
            return tex;
        }

        public static void Clear()
        {
            foreach (var tex in _textures.Values)
                if (tex != null) Object.Destroy(tex);
            _textures.Clear();
        }
    }
}