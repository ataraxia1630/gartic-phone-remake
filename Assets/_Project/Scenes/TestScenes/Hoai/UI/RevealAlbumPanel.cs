using System.Text;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using TMPro;
using UnityEngine;

namespace InkEcho.Hoai.UI
{
    /// <summary>
    /// Shown during PhaseType.Reveal. Displays the chain currently focused by
    /// PhaseManager.RevealAlbumIndex as a text-only readout. Real drawing playback
    /// requires per-chain LineRenderer storage which is scene work, not in scope here.
    /// </summary>
    public class RevealAlbumPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI chainBodyLabel;

        private readonly StringBuilder _sb = new StringBuilder(256);

        private void Update()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var album = ServiceLocator.Get<AlbumStore>();
            if (pm == null || album == null)
            {
                if (titleLabel != null) titleLabel.text = "(reveal idle)";
                if (chainBodyLabel != null) chainBodyLabel.text = string.Empty;
                return;
            }

            var chainSlot = pm.RevealAlbumIndex;
            var links = album.LinksPerChain;

            if (titleLabel != null)
            {
                titleLabel.text = $"Album #{chainSlot}";
            }

            if (chainBodyLabel == null) return;

            _sb.Clear();
            for (byte link = 0; link < links; link++)
            {
                var entry = album.GetEntry(link, chainSlot);
                if (link == 0)
                {
                    _sb.Append("[Prompt] ").AppendLine(entry.Prompt.ToString());
                }
                else if (link == links - 1)
                {
                    _sb.Append("[Final Guess] ").AppendLine(entry.Guess.ToString());
                }
                else
                {
                    _sb.Append("[Draw L").Append(link).Append("] hash=")
                       .Append(entry.DrawingHash.ToString("X"))
                       .Append(" strokes=").Append(entry.DrawingStrokes)
                       .AppendLine();
                }
            }

            chainBodyLabel.text = _sb.ToString();
        }
    }
}
