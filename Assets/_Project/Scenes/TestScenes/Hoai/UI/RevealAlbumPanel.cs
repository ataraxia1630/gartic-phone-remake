using Fusion;
using InkEcho.Hoai.Drawing;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using InkEcho.Network.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InkEcho.Hoai.UI
{
    // Reveal phase: host clicks Next to reveal links one by one.
    // RevealLinkIndex 0 = prompt; 1..N-1 = drawings; N = final guess.
    // All clients see the same progressive state. Only host has the Next button.
    public class RevealAlbumPanel : MonoBehaviour
    {
        [System.Serializable]
        public class DrawingLinkSlot
        {
            public GameObject root;
            public RawImage drawingImage;
            public TextMeshProUGUI authorLabel;
        }

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleLabel;

        [Header("Prompt (link 0)")]
        [SerializeField] private GameObject promptContainer;
        [SerializeField] private TextMeshProUGUI promptLabel;
        [SerializeField] private TextMeshProUGUI promptAuthorLabel;

        [Header("Drawings (link 1..N-1)")]
        [SerializeField] private DrawingLinkSlot[] drawingSlots;

        [Header("Final Guess (last link)")]
        [SerializeField] private GameObject guessContainer;
        [SerializeField] private TextMeshProUGUI finalGuessLabel;
        [SerializeField] private TextMeshProUGUI guessAuthorLabel;

        [Header("Host Controls")]
        [SerializeField] private GameObject hostControlsRoot;
        [SerializeField] private Button nextButton;
        [SerializeField] private TextMeshProUGUI nextButtonLabel;

        [Header("Non-host Status")]
        [SerializeField] private TextMeshProUGUI statusLabel;

        private byte _shownAlbum = byte.MaxValue;
        private byte _shownLink = byte.MaxValue;

        private void OnEnable()
        {
            _shownAlbum = byte.MaxValue;
            _shownLink = byte.MaxValue;
            if (nextButton != null)
            {
                nextButton.onClick.RemoveListener(OnHostNext);
                nextButton.onClick.AddListener(OnHostNext);
            }
            HideAllContent();
        }

        private void OnDisable()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(OnHostNext);
        }

        private void Update()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            var album = ServiceLocator.Get<AlbumStore>();
            if (pm == null || album == null) return;

            bool isHost = pm.HasStateAuthority;
            if (hostControlsRoot != null) hostControlsRoot.SetActive(isHost);
            if (statusLabel != null) statusLabel.text = isHost ? string.Empty : "Chờ host mở...";

            byte chainSlot = pm.RevealAlbumIndex;
            byte revealedLink = pm.RevealLinkIndex;
            byte totalLinks = album.LinksPerChain;

            if (isHost && nextButton != null)
            {
                nextButton.interactable = true;
                if (nextButtonLabel != null)
                    nextButtonLabel.text = ResolveNextButtonText(chainSlot, revealedLink, totalLinks, album.PlayerCount);
            }

            if (chainSlot == _shownAlbum && revealedLink == _shownLink) return;
            _shownAlbum = chainSlot;
            _shownLink = revealedLink;

            RefreshDisplay(album, chainSlot, revealedLink, totalLinks);
        }

        private string ResolveNextButtonText(byte chainSlot, byte revealedLink, byte totalLinks, byte playerCount)
        {
            bool allLinksShown = totalLinks == 0 || revealedLink >= (byte)(totalLinks - 1);
            bool isLastAlbum = chainSlot >= (byte)(playerCount - 1);
            if (allLinksShown && isLastAlbum) return "Kết thúc";
            if (allLinksShown) return "Album tiếp theo ▶";
            return "Tiếp theo ▶";
        }

        private void RefreshDisplay(AlbumStore album, byte chainSlot, byte revealedLink, byte totalLinks)
        {
            var channel = ServiceLocator.Get<DrawingChannel>();

            if (titleLabel != null)
            {
                var entry0 = album.GetEntry(0, chainSlot);
                titleLabel.text = $"Album của {ResolveName(entry0.OriginPlayer)}";
            }

            // Link 0 = prompt, shown as soon as album is opened (revealedLink always >= 0)
            if (promptContainer != null) promptContainer.SetActive(true);
            var prompt = album.GetEntry(0, chainSlot);
            if (promptLabel != null) promptLabel.text = $"\"{prompt.Prompt}\"";
            if (promptAuthorLabel != null) promptAuthorLabel.text = $"— {ResolveName(prompt.OriginPlayer)}";

            // Drawing links: link 1..totalLinks-2
            int drawingCount = Mathf.Max(0, (int)totalLinks - 2);
            for (int i = 0; i < drawingSlots.Length; i++)
            {
                var dslot = drawingSlots[i];
                if (dslot?.root == null) continue;
                byte linkIdx = (byte)(i + 1);
                bool show = i < drawingCount && revealedLink >= linkIdx;
                dslot.root.SetActive(show);
                if (!show) continue;

                if (channel != null && channel.TryGetDrawing(linkIdx, chainSlot, out var tex, out var drawAuthor))
                {
                    if (dslot.drawingImage != null) { dslot.drawingImage.texture = tex; dslot.drawingImage.enabled = true; }
                    if (dslot.authorLabel != null) dslot.authorLabel.text = $"Vẽ bởi: {ResolveName(drawAuthor)}";
                }
                else
                {
                    var entry = album.GetEntry(linkIdx, chainSlot);
                    if (dslot.drawingImage != null) dslot.drawingImage.enabled = false;
                    if (dslot.authorLabel != null) dslot.authorLabel.text = $"Vẽ bởi: {ResolveName(entry.WorkerPlayer)} (chưa tải)";
                }
            }

            // Final guess: last link
            if (totalLinks > 1)
            {
                byte lastLink = (byte)(totalLinks - 1);
                bool showGuess = revealedLink >= lastLink;
                if (guessContainer != null) guessContainer.SetActive(showGuess);
                if (showGuess)
                {
                    var lastEntry = album.GetEntry(lastLink, chainSlot);
                    string guess = lastEntry.GuessRole0.ToString();
                    if (string.IsNullOrEmpty(guess)) guess = lastEntry.GuessRole1.ToString();
                    if (finalGuessLabel != null) finalGuessLabel.text = $"\"{guess}\"";
                    if (guessAuthorLabel != null) guessAuthorLabel.text = $"— {ResolveName(lastEntry.WorkerPlayer)}";
                }
            }
        }

        private void HideAllContent()
        {
            if (promptContainer != null) promptContainer.SetActive(false);
            if (guessContainer != null) guessContainer.SetActive(false);
            foreach (var dslot in drawingSlots)
                if (dslot?.root != null) dslot.root.SetActive(false);
        }

        private void OnHostNext()
        {
            var pm = ServiceLocator.Get<PhaseManager>();
            if (pm == null || !pm.HasStateAuthority) return;
            pm.RevealNext();
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
