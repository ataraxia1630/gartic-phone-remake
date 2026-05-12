using UnityEngine;

namespace InkEcho.Network.GameModes
{
    [CreateAssetMenu(menuName = "InkEcho/Network/Game Mode Config", fileName = "GameModeConfig")]
    public class GameModeConfig : ScriptableObject
    {
        [Header("Identity")]
        public GameModeType Mode = GameModeType.Sandwich;

        [Header("Override Phase Durations (seconds, <=0 means use NetworkConfig)")]
        public float PromptPhaseDuration = -1f;
        public float DrawPhaseDuration = -1f;
        public float GuessPhaseDuration = -1f;
        public float ObservePhaseDuration = -1f;
        public float FinalGuessPhaseDuration = -1f;
        public float RevealPerAlbumDuration = -1f;

        public float ResolvePromptDuration(float fallback) => PromptPhaseDuration > 0f ? PromptPhaseDuration : fallback;
        public float ResolveDrawDuration(float fallback) => DrawPhaseDuration > 0f ? DrawPhaseDuration : fallback;
        public float ResolveGuessDuration(float fallback) => GuessPhaseDuration > 0f ? GuessPhaseDuration : fallback;
        public float ResolveObserveDuration(float fallback) => ObservePhaseDuration > 0f ? ObservePhaseDuration : fallback;
        public float ResolveFinalGuessDuration(float fallback) => FinalGuessPhaseDuration > 0f ? FinalGuessPhaseDuration : fallback;
        public float ResolveRevealDuration(float fallback) => RevealPerAlbumDuration > 0f ? RevealPerAlbumDuration : fallback;
    }
}
