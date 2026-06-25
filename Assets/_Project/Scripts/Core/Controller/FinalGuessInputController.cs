using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using TMPro;
using UnityEngine;
using Fusion;

namespace InkEcho.Network.Players
{
    public class FinalGuessInputController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;

        public string CurrentText => inputField != null ? inputField.text : string.Empty;

        private void OnEnable() => ServiceLocator.Register<FinalGuessInputController>(this);

        private void OnDisable() => ServiceLocator.Unregister<FinalGuessInputController>(this);

        public void ForceSubmit(string fallbackText)
        {
            var phaseManager = ServiceLocator.Get<PhaseManager>();
            var albumStore = ServiceLocator.Get<AlbumStore>();
            if (phaseManager == null || albumStore == null) return;
            if (phaseManager.CurrentPhase != PhaseType.FinalGuess) return;
            if (!phaseManager.TryGetAssignment(phaseManager.Runner.LocalPlayer, out var assignment)) return;

            var text = CurrentText;
            if (string.IsNullOrWhiteSpace(text)) text = fallbackText;
            text = text.Trim();
            if (text.Length > 16) text = text.Substring(0, 16);

            albumStore.Rpc_SubmitFinalGuess(assignment.AlbumOriginSlotIndex, new NetworkString<_16>(text), assignment.PairRole);

            Debug.Log($"[FinalGuessInputController] Submitted final guess: \"{text}\" for originSlot={assignment.AlbumOriginSlotIndex}");
        }
    }
}
