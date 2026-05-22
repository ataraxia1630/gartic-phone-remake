using InkEcho.Hoai.Drawing;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using InkEcho.Network.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Fusion;

namespace InkEcho.Hoai.UI
{
    // Reveal phase: hiển thị album của chain RevealAlbumIndex. Walks qua mọi link:
    //   link 0 = prompt gốc, link 1..N-1 = drawings, last link = final guess.
    // Mỗi link có 1 RawImage để show tranh (link 0 và last để null), kèm author label.
    // Phần text (prompt/guess) hiển thị ở title + footer.
    public class RevealAlbumPanel : MonoBehaviour
    {
        [System.Serializable]
        public class LinkSlot
        {
            public RawImage drawingImage;
            public TextMeshProUGUI authorLabel;
            public TextMeshProUGUI captionLabel; // optional: hiện text dưới mỗi drawing
        }

        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI promptLabel;
        [SerializeField] private TextMeshProUGUI finalGuessLabel;
        [SerializeField] private TextMeshProUGUI countdownLabel;
        [SerializeField] private LinkSlot[] drawingSlots;
        [SerializeField] private string albumTitleFormat = "Album #{0}";
        [SerializeField] private string authorFormat = "by {0}";

        private byte _lastShownChain = byte.MaxValue;

        private void OnEnable()
        {
            _lastShownChain = byte.MaxValue;
        }

        private void Update()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var album = ServiceLocator.Get<AlbumStore>();
            var channel = ServiceLocator.Get<DrawingChannel>();
            if (pm == null || album == null) return;

            byte chainSlot = pm.RevealAlbumIndex;
            byte links = album.LinksPerChain;

            if (countdownLabel != null)
            {
                var remaining = pm.PhaseTimer.RemainingTime(pm.Runner);
                countdownLabel.text = remaining.HasValue ? Mathf.CeilToInt(remaining.Value).ToString() : "—";
            }

            if (chainSlot == _lastShownChain) return;
            _lastShownChain = chainSlot;

            if (titleLabel != null) titleLabel.text = string.Format(albumTitleFormat, chainSlot);

            // Link 0 = prompt
            if (links > 0)
            {
                var first = album.GetEntry(0, chainSlot);
                if (promptLabel != null)
                {
                    promptLabel.text = $"Prompt: {first.Prompt} ({ResolveName(first.OriginPlayer)})";
                }
            }

            // Last link = final guess
            if (links > 1)
            {
                var last = album.GetEntry((byte)(links - 1), chainSlot);
                if (finalGuessLabel != null)
                {
                    finalGuessLabel.text = $"Guess: {last.Guess} ({ResolveName(last.WorkerPlayer)})";
                }
            }

            // Drawing links: link 1..links-2
            int drawingLinkCount = Mathf.Max(0, links - 2);
            for (int i = 0; i < drawingSlots.Length; i++)
            {
                var slot = drawingSlots[i];
                if (slot == null) continue;
                if (i >= drawingLinkCount)
                {
                    SetSlotEmpty(slot);
                    continue;
                }

                byte linkIdx = (byte)(i + 1);
                var entry = album.GetEntry(linkIdx, chainSlot);

                if (channel != null && channel.TryGetDrawing(linkIdx, chainSlot, out var tex, out var author))
                {
                    if (slot.drawingImage != null) { slot.drawingImage.texture = tex; slot.drawingImage.enabled = true; }
                    if (slot.authorLabel != null) slot.authorLabel.text = string.Format(authorFormat, ResolveName(author));
                    if (slot.captionLabel != null) slot.captionLabel.text = $"Link {linkIdx}";
                }
                else
                {
                    if (slot.drawingImage != null) { slot.drawingImage.texture = null; slot.drawingImage.enabled = false; }
                    if (slot.authorLabel != null) slot.authorLabel.text = string.Format(authorFormat, ResolveName(entry.WorkerPlayer));
                    if (slot.captionLabel != null) slot.captionLabel.text = $"Link {linkIdx} (chưa có tranh)";
                }
            }
        }

        private void SetSlotEmpty(LinkSlot slot)
        {
            if (slot.drawingImage != null) { slot.drawingImage.texture = null; slot.drawingImage.enabled = false; }
            if (slot.authorLabel != null) slot.authorLabel.text = string.Empty;
            if (slot.captionLabel != null) slot.captionLabel.text = string.Empty;
        }

        private static string ResolveName(PlayerRef player)
        {
            if (!player.IsRealPlayer) return "?";
            var registry = ServiceLocator.Get<PlayerRegistry>();
            if (registry != null && registry.TryGetSlot(player, out var slot))
            {
                return slot.DisplayName.Value;
            }
            return $"Player{player.PlayerId}";
        }
    }
}
