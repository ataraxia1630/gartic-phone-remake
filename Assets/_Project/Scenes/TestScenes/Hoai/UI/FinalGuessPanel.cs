using Fusion;
using InkEcho.Gameplay.Data;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using InkEcho.Network.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InkEcho.Hoai.UI
{
    // Final guess phase: local player có assignment (chain=slot, link=n).
    // Tranh để đoán = link cuối cùng trước đó: (link-1, slot). Hiện cả tranh + InputField.
    public class FinalGuessPanel : MonoBehaviour
    {
        [SerializeField] private RawImage targetDrawing;
        [SerializeField] private TextMeshProUGUI authorLabel;
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Button submitButton;
        [SerializeField] private TextMeshProUGUI targetChainLabel;
        [SerializeField] private TextMeshProUGUI countdownLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private string targetChainFormat = "Guess the original prompt of chain #{0}";
        [SerializeField] private string authorFormat = "Drawing by: {0}";

        private bool _submitted;
        private byte _lastShownLink = byte.MaxValue;
        private byte _lastShownSlot = byte.MaxValue;

        private void OnEnable()
        {
            _submitted = false;
            _lastShownLink = byte.MaxValue;
            _lastShownSlot = byte.MaxValue;
            if (input != null) { input.text = string.Empty; input.interactable = true; }
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(OnSubmit);
                submitButton.onClick.AddListener(OnSubmit);
                submitButton.interactable = true;
            }
            if (statusLabel != null) statusLabel.text = string.Empty;
            ClearImage();
        }

        private void OnDisable()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmit);
        }

        private void Update()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            if (pm == null) return;

            if (countdownLabel != null)
            {
                var remaining = pm.PhaseTimer.RemainingTime(pm.Runner);
                countdownLabel.text = remaining.HasValue ? Mathf.CeilToInt(remaining.Value).ToString() : "—";
            }

            UpdateTargetLabel(pm);
            RefreshDrawing(pm);

            if (!_submitted && pm.CurrentPhase == PhaseType.FinalGuess && pm.PhaseTimer.IsRunning)
            {
                float t = pm.PhaseTimer.RemainingTime(pm.Runner).GetValueOrDefault();
                if (t <= 0.1f) OnSubmit();
            }
        }

        private void UpdateTargetLabel(PhaseManager pm)
        {
            if (targetChainLabel == null) return;
            var runner = NetworkBootstrap.Instance?.Runner;
            if (runner == null) return;
            if (pm.TryGetAssignment(runner.LocalPlayer, out var assignment))
            {
                targetChainLabel.text = string.Format(targetChainFormat, assignment.AlbumOriginSlotIndex);
            }
            else
            {
                targetChainLabel.text = "(no chain to guess)";
            }
        }

        private void RefreshDrawing(PhaseManager pm)
        {
            var runner = NetworkBootstrap.Instance?.Runner;
            if (runner == null) return;
            if (!pm.TryGetAssignment(runner.LocalPlayer, out var assignment)) return;
            if (assignment.ChainLinkIndex == 0) return;

            byte targetLink = (byte)(assignment.ChainLinkIndex - 1);
            byte targetSlot = assignment.AlbumOriginSlotIndex;
            if (targetLink == _lastShownLink && targetSlot == _lastShownSlot && targetDrawing != null && targetDrawing.texture != null)
                return;

            var tex = DrawingTextureStore.GetTexture(targetLink, targetSlot);
            if (tex != null)
            {
                if (targetDrawing != null) { targetDrawing.texture = tex; targetDrawing.enabled = true; }
                if (authorLabel != null)
                {
                    var album = ServiceLocator.Get<AlbumStore>();
                    if (album != null)
                        authorLabel.text = string.Format(authorFormat, ResolveName(album.GetEntry(targetLink, targetSlot).WorkerPlayer));
                }
                _lastShownLink = targetLink;
                _lastShownSlot = targetSlot;
            }
        }

        private void ClearImage()
        {
            if (targetDrawing != null) { targetDrawing.texture = null; targetDrawing.enabled = false; }
            if (authorLabel != null) authorLabel.text = string.Empty;
        }

        public void OnSubmit()
        {
            if (_submitted) return;

            var pm = ServiceLocator.Get<PhaseManager>();
            var album = ServiceLocator.Get<AlbumStore>();
            var runner = NetworkBootstrap.Instance?.Runner;
            if (pm == null || album == null || runner == null) return;
            if (pm.CurrentPhase != PhaseType.FinalGuess) return;
            if (!pm.TryGetAssignment(runner.LocalPlayer, out var assignment)) return;

            string guess = (input != null && !string.IsNullOrWhiteSpace(input.text)) ? input.text.Trim() : "(empty)";
            album.Rpc_SubmitFinalGuess(assignment.AlbumOriginSlotIndex, guess, assignment.PairRole);

            _submitted = true;
            if (input != null) input.interactable = false;
            if (submitButton != null) submitButton.interactable = false;
            if (statusLabel != null) statusLabel.text = "Submitted, waiting for Reveal...";
        }

        private static string ResolveName(PlayerRef player)
        {
            if (!player.IsRealPlayer) return "?";
            var registry = ServiceLocator.Get<PlayerRegistry>();
            if (registry != null && registry.TryGetSlot(player, out var slot))
                return slot.DisplayName.Value;
            return $"Player{player.PlayerId}";
        }
    }
}
