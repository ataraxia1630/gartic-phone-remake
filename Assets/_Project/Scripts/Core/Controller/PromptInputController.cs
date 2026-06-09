using Fusion;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InkEcho.Network.Players
{
    /// <summary>
    /// Attach to the Prompt phase UI (UI_Prompt.unity), alongside the existing TMP_InputField.
    /// Reads the player's typed prompt and submits it via AlbumStore.Rpc_SubmitPrompt on button click.
    /// Also exposes CurrentText so LocalSubmitController can use the live input on forced timeout submit.
    /// </summary>
    public class PromptInputController : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;

        private bool _hasSubmitted;
        private PhaseType _lastObservedPhase = PhaseType.None;

        public string CurrentText => inputField != null ? inputField.text : string.Empty;
        public bool HasSubmitted => _hasSubmitted;

        private void OnEnable() => ServiceLocator.Register<PromptInputController>(this);

        private void OnDisable() => ServiceLocator.Unregister<PromptInputController>(this);

        private void Update()
        {
            var phaseManager = ServiceLocator.Get<PhaseManager>();
            if (phaseManager == null) return;

            if (phaseManager.CurrentPhase != _lastObservedPhase)
            {
                _lastObservedPhase = phaseManager.CurrentPhase;
                _hasSubmitted = false;
                if (inputField != null)
                {
                    inputField.text = string.Empty;
                    inputField.interactable = phaseManager.CurrentPhase == PhaseType.Prompt;
                }
            }
        }

        /// <summary>Called by LocalSubmitController.ForceSubmitCurrentWork — falls back to placeholder text if input is empty.</summary>
        public void ForceSubmit(string fallbackText)
        {
            if (_hasSubmitted) return;

            var phaseManager = ServiceLocator.Get<PhaseManager>();
            var albumStore = ServiceLocator.Get<AlbumStore>();
            if (phaseManager == null || albumStore == null) return;
            if (phaseManager.CurrentPhase != PhaseType.Prompt) return;
            if (!phaseManager.TryGetAssignment(phaseManager.Runner.LocalPlayer, out var assignment)) return;

            var text = CurrentText;
            if (string.IsNullOrWhiteSpace(text)) text = fallbackText;
            if (text.Length > 60) text = text.Substring(0, 60);

            albumStore.Rpc_SubmitPrompt(assignment.AlbumOriginSlotIndex, new NetworkString<_64>(text), assignment.PairRole);
            _hasSubmitted = true;

            if (inputField != null) inputField.interactable = false;

            Debug.Log($"[PromptInputController] Submitted prompt: \"{text}\" for originSlot={assignment.AlbumOriginSlotIndex}");
        }
    }
}
