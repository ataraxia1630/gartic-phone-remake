using UnityEngine;
using InkEcho.Network.GameModes;

namespace InkEcho.Network.Core
{
    [CreateAssetMenu(menuName = "InkEcho/Network/Network Config", fileName = "NetworkConfig")]
    public class NetworkConfig : ScriptableObject
    {
        [Header("Session")]
        [Range(3, 8)] public int MaxPlayers = 8;
        [Range(3, 8)] public int MinPlayersToStart = 3;

        [Header("Room Code")]
        [Range(3, 6)] public int RoomCodeLength = 4;
        [Range(1, 10)] public int RoomCodeRetryAttempts = 5;

        [Header("Default Phase Durations (seconds)")]
        public float PromptPhaseDuration = 30f;
        public float DrawPhaseDuration = 60f;
        public float GuessPhaseDuration = 30f;
        public float RevealPerAlbumDuration = 8f;

        [Header("Per-Mode Overrides (optional)")]
        public GameModeConfig[] ModeConfigs;

        [Header("Region (WebGL needs fixed region)")]
        public string FixedRegion = "asia";

        public GameModeConfig GetModeConfig(GameModeType mode)
        {
            if (ModeConfigs == null) return null;
            for (int i = 0; i < ModeConfigs.Length; i++)
            {
                if (ModeConfigs[i] != null && ModeConfigs[i].Mode == mode) return ModeConfigs[i];
            }
            return null;
        }
    }
}
