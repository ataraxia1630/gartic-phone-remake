using InkEcho.Network.Phases;
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

        [Header("Phase UI Scenes")]
        public string PromptSceneName = "UI_Prompt";
        public string ObserveSceneName = "UI_Observe";
        public string DrawSceneName = "UI_Draw";
        public string GuessSceneName = "UI_Guess";
        public string FinalGuessSceneName = "UI_FinalGuess";
        public string RevealSceneName = "UI_Reveal";

        public float ResolvePromptDuration(float fallback) => PromptPhaseDuration > 0f ? PromptPhaseDuration : fallback;
        public float ResolveDrawDuration(float fallback) => DrawPhaseDuration > 0f ? DrawPhaseDuration : fallback;
        public float ResolveGuessDuration(float fallback) => GuessPhaseDuration > 0f ? GuessPhaseDuration : fallback;
        public float ResolveObserveDuration(float fallback) => ObservePhaseDuration > 0f ? ObservePhaseDuration : fallback;
        public float ResolveFinalGuessDuration(float fallback) => FinalGuessPhaseDuration > 0f ? FinalGuessPhaseDuration : fallback;
        public float ResolveRevealDuration(float fallback) => RevealPerAlbumDuration > 0f ? RevealPerAlbumDuration : fallback;

        public string GetSceneNameForPhase(PhaseType phase)
        {
            switch (phase)
            {
                case PhaseType.Prompt: return PromptSceneName;
                case PhaseType.Draw: return DrawSceneName;
                case PhaseType.Guess: return GuessSceneName;
                case PhaseType.Reveal: return RevealSceneName;
                case PhaseType.Observe: return ObserveSceneName;
                case PhaseType.FinalGuess: return FinalGuessSceneName;
                default: return string.Empty;
            }
        }
    }
}
