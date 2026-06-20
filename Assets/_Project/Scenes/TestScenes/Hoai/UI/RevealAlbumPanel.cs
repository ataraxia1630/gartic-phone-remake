using Fusion;
using InkEcho.Gameplay.Data;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Players;
using InkEcho.Network.StateMachine;
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
        private bool _loggedMissing;

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
            // ServiceLocator có thể chưa có ref sau khi LoadScene(Single) sang ResultScene —
            // fallback FindObjectOfType + re-register, giống PhaseManager.StartGame.
            var gsm = Resolve<GameStateMachine>();
            var album = Resolve<AlbumStore>();
            if (gsm == null || album == null)
            {
                if (!_loggedMissing)
                {
                    _loggedMissing = true;
                    Debug.LogWarning($"[RevealAlbumPanel] Thiếu singleton trong ResultScene — gsm={(gsm != null)}, album={(album != null)}. Không bind được data reveal.");
                }
                return;
            }
            _loggedMissing = false;

            bool isHost = gsm.HasStateAuthority;
            if (hostControlsRoot != null) hostControlsRoot.SetActive(isHost);
            if (statusLabel != null) statusLabel.text = isHost ? string.Empty : "Chờ host mở...";

            byte chainSlot = gsm.RevealAlbumIndex;
            byte revealedLink = gsm.RevealLinkIndex;
            byte totalLinks = album.LinksPerChain;

            if (isHost && nextButton != null)
            {
                nextButton.interactable = !gsm.IsRevealFinished;
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

            var storedKeys = new System.Text.StringBuilder();
            int storedTextures = 0;
            foreach (var (cl, os, png) in DrawingTextureStore.GetAllRawPngs()) { storedTextures++; storedKeys.Append($"({cl},{os}) "); }
            Debug.Log($"[RevealAlbumPanel] Refresh album={chainSlot} link={revealedLink} totalLinks={totalLinks} drawingCount={drawingCount} playerCount={album.PlayerCount} storedTextures={storedTextures} storedKeys(chainLink,originSlot)=[{storedKeys}]");
            for (int i = 0; i < drawingSlots.Length; i++)
            {
                var dslot = drawingSlots[i];
                if (dslot?.root == null) continue;
                byte linkIdx = (byte)(i + 1);
                bool show = i < drawingCount && revealedLink >= linkIdx;
                dslot.root.SetActive(show);
                if (!show) continue;

                var entry = album.GetEntry(linkIdx, chainSlot);
                var tex = DrawingTextureStore.GetTexture(linkIdx, chainSlot);
                Debug.Log($"[RevealAlbumPanel] slot i={i} -> GetTexture(linkIdx={linkIdx}, chainSlot={chainSlot}) key={linkIdx * 32 + chainSlot} => {(tex != null ? $"FOUND {tex.width}x{tex.height}" : "NULL")}; entry.WorkerPlayer={entry.WorkerPlayer}");
                if (tex != null)
                {
                    if (dslot.drawingImage != null) 
                    { 
                        dslot.drawingImage.texture = tex; 
                        dslot.drawingImage.enabled = true; 
                        dslot.drawingImage.gameObject.SetActive(true);
                        dslot.drawingImage.color = Color.white;

                        // Cách sửa tận gốc vấn đề ScrollView: 
                        // Khi nằm trong Layout Group của ScrollView, RawImage thường bị bóp nghẹt kích thước về 0x0
                        // Ta sẽ tự động gắn/ép LayoutElement để Layout Group cấp đủ không gian (800x600) cho ảnh.
                        var layoutElement = dslot.drawingImage.GetComponent<UnityEngine.UI.LayoutElement>();
                        if (layoutElement == null) layoutElement = dslot.drawingImage.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                        layoutElement.minWidth = 800f;
                        layoutElement.minHeight = 600f;
                        layoutElement.preferredWidth = 800f;
                        layoutElement.preferredHeight = 600f;
                        
                        var rt = dslot.drawingImage.rectTransform;
                        Debug.Log($"[RevealAlbumPanel] UI DEBUG slot {i}: sizeDelta={rt.sizeDelta}, rect={rt.rect}, activeInHierarchy={dslot.drawingImage.gameObject.activeInHierarchy}, scale={rt.localScale}");
                    }
                    else
                    {
                        Debug.LogWarning($"[RevealAlbumPanel] LỖI: drawingImage bị NULL ở slot i={i} trong Inspector!");
                    }
                    if (dslot.authorLabel != null) dslot.authorLabel.text = $"Vẽ bởi: {ResolveName(entry.WorkerPlayer)}";
                }
                else
                {
                    if (dslot.drawingImage != null) 
                    {
                        dslot.drawingImage.enabled = false;
                        dslot.drawingImage.gameObject.SetActive(false);
                    }
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
            var gsm = ServiceLocator.Get<GameStateMachine>();
            if (gsm == null) return;
            gsm.Rpc_RevealNext();
        }

        // Lấy singleton từ ServiceLocator; nếu null (ServiceLocator chưa re-register sau scene load)
        // thì tìm trong scene rồi đăng ký lại. Nếu trả null nghĩa là object thật sự không tồn tại
        // (đã bị despawn) — khi đó log ở Update sẽ báo.
        private static T Resolve<T>() where T : Object
        {
            if (ServiceLocator.TryGet<T>(out var svc) && svc != null) return svc;
            var found = FindAnyObjectByType<T>();
            if (found != null) ServiceLocator.Register<T>(found);
            return found;
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
