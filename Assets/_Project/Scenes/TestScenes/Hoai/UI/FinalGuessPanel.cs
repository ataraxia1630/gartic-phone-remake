using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InkEcho.Hoai.UI
{
    /// <summary>
    /// Shown during PhaseType.FinalGuess. The local player has an assignment for
    /// exactly one chain — they look at the last drawing in that chain and submit
    /// a single guess.
    /// </summary>
    public class FinalGuessPanel : MonoBehaviour
    {
        [SerializeField] private TMP_InputField input;
        [SerializeField] private Button submitButton;
        [SerializeField] private TextMeshProUGUI targetChainLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [SerializeField] private string targetChainFormat = "Đoán prompt gốc của chain #{0}";

        private bool _submitted;

        private void OnEnable()
        {
            _submitted = false;
            if (input != null) input.text = string.Empty;
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(OnSubmit);
                submitButton.onClick.AddListener(OnSubmit);
                submitButton.interactable = true;
            }
            if (statusLabel != null) statusLabel.text = string.Empty;
            UpdateTargetLabel();
        }

        private void OnDisable()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(OnSubmit);
        }

        private void Update()
        {
            UpdateTargetLabel();
        }

        private void UpdateTargetLabel()
        {
            if (targetChainLabel == null) return;
            var pm = ServiceLocator.Get<PhaseManager>();
            var runner = NetworkBootstrap.Instance?.Runner;
            if (pm == null || runner == null)
            {
                targetChainLabel.text = string.Empty;
                return;
            }

            if (pm.TryGetAssignment(runner.LocalPlayer, out var assignment))
            {
                targetChainLabel.text = string.Format(targetChainFormat, assignment.AlbumOriginSlotIndex);
            }
            else
            {
                targetChainLabel.text = "(không có chain để đoán)";
            }
        }

        public void OnSubmit()
        {
            if (_submitted) return;
            if (input == null || string.IsNullOrWhiteSpace(input.text)) return;

            var pm = ServiceLocator.Get<PhaseManager>();
            var album = ServiceLocator.Get<AlbumStore>();
            var runner = NetworkBootstrap.Instance?.Runner;
            if (pm == null || album == null || runner == null) return;
            if (pm.CurrentPhase != PhaseType.FinalGuess) return;
            if (!pm.TryGetAssignment(runner.LocalPlayer, out var assignment)) return;

            album.Rpc_SubmitFinalGuess(assignment.AlbumOriginSlotIndex, input.text.Trim());
            _submitted = true;
            if (submitButton != null) submitButton.interactable = false;
            if (statusLabel != null) statusLabel.text = "Đã gửi đoán, chờ người khác...";
        }
    }
}
