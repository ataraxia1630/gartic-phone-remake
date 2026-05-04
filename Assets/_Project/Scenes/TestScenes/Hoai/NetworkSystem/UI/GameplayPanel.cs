using Fusion;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using InkEcho.Network.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InkEcho.Network.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class GameplayPanel : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text hintText;

        [Header("Input")]
        [SerializeField] private TMP_InputField textInput;
        [SerializeField] private Button submitTextButton;
        [SerializeField] private Button submitDrawingButton;

        private CanvasGroup panelGroup;

        private void Awake()
        {
            panelGroup = GetComponent<CanvasGroup>();
            if (submitTextButton != null) submitTextButton.onClick.AddListener(OnSubmitTextClicked);
            if (submitDrawingButton != null) submitDrawingButton.onClick.AddListener(OnSubmitDrawingClicked);
        }

        private void Update()
        {
            var phaseManager = ServiceLocator.Get<PhaseManager>();
            bool visible = phaseManager != null && phaseManager.CurrentPhase != PhaseType.None;
            SetVisible(visible);
            if (!visible) return;

            UpdateLabels(phaseManager);
            UpdateControls(phaseManager);
        }

        private void UpdateLabels(PhaseManager phaseManager)
        {
            if (phaseText != null)
            {
                phaseText.text = $"Phase: {phaseManager.CurrentPhase}  Round: {phaseManager.RoundIndex + 1}/{Mathf.Max(1, phaseManager.TotalRounds)}";
            }

            if (hintText == null) return;
            switch (phaseManager.CurrentPhase)
            {
                case PhaseType.Prompt:
                    hintText.text = "Write a prompt and submit.";
                    break;
                case PhaseType.Draw:
                    hintText.text = "Submit a placeholder drawing for the current album.";
                    break;
                case PhaseType.Guess:
                    hintText.text = "Write your guess and submit.";
                    break;
                case PhaseType.Reveal:
                    hintText.text = BuildRevealText(phaseManager);
                    break;
            }
        }

        private string BuildRevealText(PhaseManager phaseManager)
        {
            var album = ServiceLocator.Get<AlbumStore>();
            if (album == null || album.PlayerCount == 0) return "Reveal phase.";

            var slot = phaseManager.RevealAlbumIndex;
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Album {slot + 1}/{album.PlayerCount}");
            for (int round = 0; round < album.TotalRounds; round++)
            {
                var entry = album.GetEntry(round, slot);
                var prompt = entry.Prompt.Value;
                var guess = entry.Guess.Value;
                if (!string.IsNullOrEmpty(prompt)) sb.AppendLine($"  R{round + 1} prompt: {prompt}");
                if (entry.DrawingHash != 0UL) sb.AppendLine($"  R{round + 1} drawing #{entry.DrawingHash & 0xFFFF}");
                if (!string.IsNullOrEmpty(guess)) sb.AppendLine($"  R{round + 1} guess: {guess}");
            }
            return sb.ToString();
        }

        private void UpdateControls(PhaseManager phaseManager)
        {
            bool allowText = phaseManager.CurrentPhase == PhaseType.Prompt || phaseManager.CurrentPhase == PhaseType.Guess;
            bool allowDrawing = phaseManager.CurrentPhase == PhaseType.Draw;

            if (textInput != null) textInput.gameObject.SetActive(allowText);
            if (submitTextButton != null) submitTextButton.gameObject.SetActive(allowText);
            if (submitDrawingButton != null) submitDrawingButton.gameObject.SetActive(allowDrawing);
        }

        private void OnSubmitTextClicked()
        {
            var phaseManager = ServiceLocator.Get<PhaseManager>();
            var localPlayer = NetworkPlayer.Local;
            var albumStore = ServiceLocator.Get<AlbumStore>();
            var registry = ServiceLocator.Get<PlayerRegistry>();
            if (phaseManager == null || localPlayer == null || albumStore == null || registry == null) return;
            if (!phaseManager.TryGetAssignment(localPlayer.Object.InputAuthority, out var assignment)) return;

            var text = textInput != null ? textInput.text.Trim() : string.Empty;
            var networkText = new NetworkString<_32>(string.IsNullOrEmpty(text) ? "..." : text);

            if (phaseManager.CurrentPhase == PhaseType.Prompt)
            {
                albumStore.Rpc_SubmitPrompt(assignment.AlbumOriginSlotIndex, networkText);
            }
            else if (phaseManager.CurrentPhase == PhaseType.Guess)
            {
                albumStore.Rpc_SubmitGuess(assignment.AlbumOriginSlotIndex, networkText);
            }

            if (textInput != null) textInput.text = string.Empty;
        }

        private void OnSubmitDrawingClicked()
        {
            var phaseManager = ServiceLocator.Get<PhaseManager>();
            var localPlayer = NetworkPlayer.Local;
            var albumStore = ServiceLocator.Get<AlbumStore>();
            if (phaseManager == null || localPlayer == null || albumStore == null) return;
            if (!phaseManager.TryGetAssignment(localPlayer.Object.InputAuthority, out var assignment)) return;

            var hash = (ulong)Random.Range(1, int.MaxValue);
            albumStore.Rpc_SubmitDrawing(assignment.AlbumOriginSlotIndex, hash, 0);
        }

        private void SetVisible(bool visible)
        {
            if (panelGroup == null) return;
            panelGroup.alpha = visible ? 1f : 0f;
            panelGroup.interactable = visible;
            panelGroup.blocksRaycasts = visible;
        }
    }
}
