using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using TMPro;
using UnityEngine;

namespace InkEcho.Gameplay.UI
{
    /// <summary>
    /// Attach to a dedicated panel in UI_Draw.unity (separate from PromptPanel/UIContentController).
    /// Persistently shows the original prompt only during the very first Draw round (ChainLinkIndex == 1),
    /// where the worker draws directly from a prompt written in the Prompt phase. Later Draw rounds
    /// draw from a guess written by the previous worker, so the panel stays hidden there.
    /// </summary>
    public class DrawPromptDisplay : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text promptLabel;

        private bool _shown;
        private byte _shownChainLink = byte.MaxValue;
        private byte _shownOriginSlot = byte.MaxValue;

        private void OnEnable()
        {
            _shown = false;
            _shownChainLink = byte.MaxValue;
            _shownOriginSlot = byte.MaxValue;
        }

        private void OnDisable()
        {
            _shown = false;
            SetPanelVisible(false);
        }

        /// <summary>
        /// Toggles panel visibility without ever disabling the GameObject this script lives on —
        /// doing so would disable this MonoBehaviour too and permanently stop Update() from running,
        /// since nothing outside this component would be left to re-enable it.
        /// </summary>
        private void SetPanelVisible(bool visible)
        {
            if (panel == null) return;

            if (panel == gameObject)
            {
                if (promptLabel != null) promptLabel.gameObject.SetActive(visible);
                return;
            }

            panel.SetActive(visible);
        }

        private void Update()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var album = ServiceLocator.Get<AlbumStore>();
            var runner = NetworkBootstrap.Instance?.Runner;
            if (pm == null || album == null || runner == null) return;

            if (pm.CurrentPhase != PhaseType.Draw)
            {
                if (_shown)
                {
                    _shown = false;
                    SetPanelVisible(false);
                }
                return;
            }

            if (!pm.TryGetAssignment(runner.LocalPlayer, out var assignment)) return;

            if (_shown && assignment.ChainLinkIndex == _shownChainLink && assignment.AlbumOriginSlotIndex == _shownOriginSlot)
                return;

            // Only the first Draw round (chain link 1) draws from an original prompt (chain link 0).
            // Later Draw rounds draw from a guess written by the previous worker — keep panel hidden.
            if (assignment.ChainLinkIndex != 1)
            {
                SetPanelVisible(false);
                _shown = true;
                _shownChainLink = assignment.ChainLinkIndex;
                _shownOriginSlot = assignment.AlbumOriginSlotIndex;
                return;
            }

            var entry = album.GetEntry(0, assignment.AlbumOriginSlotIndex);
            var promptText = entry.Prompt.ToString();

            // The submitted prompt may not have synced from the state authority yet when this
            // Draw round starts — keep retrying each frame until real text arrives instead of
            // latching onto an empty string forever.
            if (string.IsNullOrEmpty(promptText)) return;

            if (promptLabel != null) promptLabel.text = promptText;
            SetPanelVisible(true);

            _shown = true;
            _shownChainLink = assignment.ChainLinkIndex;
            _shownOriginSlot = assignment.AlbumOriginSlotIndex;
        }
    }
}
