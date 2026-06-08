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
            if (panel != null) panel.SetActive(false);
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
                    if (panel != null) panel.SetActive(false);
                }
                return;
            }

            if (!pm.TryGetAssignment(runner.LocalPlayer, out var assignment)) return;

            if (_shown && assignment.ChainLinkIndex == _shownChainLink && assignment.AlbumOriginSlotIndex == _shownOriginSlot)
                return;

            if (assignment.ChainLinkIndex != 1)
            {
                if (panel != null) panel.SetActive(false);
                _shown = true;
                _shownChainLink = assignment.ChainLinkIndex;
                _shownOriginSlot = assignment.AlbumOriginSlotIndex;
                return;
            }

            var entry = album.GetEntry(0, assignment.AlbumOriginSlotIndex);
            var promptText = entry.Prompt.ToString();

            if (string.IsNullOrEmpty(promptText)) return; // retry next frame until synced

            if (promptLabel != null) promptLabel.text = promptText;
            if (panel != null) panel.SetActive(true);

            _shown = true;
            _shownChainLink = assignment.ChainLinkIndex;
            _shownOriginSlot = assignment.AlbumOriginSlotIndex;
        }
    }
}
