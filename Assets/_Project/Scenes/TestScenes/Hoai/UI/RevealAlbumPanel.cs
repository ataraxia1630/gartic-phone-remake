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

        [Header("Layout")]
        [Tooltip("Chiều rộng tối đa của mỗi bức tranh (px). Cao tự tính theo tỉ lệ tranh.")]
        [SerializeField] private float maxDrawingWidth = 700f;
        [Tooltip("Chiều cao tối đa của mỗi bức tranh (px) — chống tranh quá cao chiếm hết màn hình.")]
        [SerializeField] private float maxDrawingHeight = 460f;

        private const float LabelHeight = 44f;

        private byte _shownAlbum = byte.MaxValue;
        private byte _shownLink = byte.MaxValue;
        private bool _loggedMissing;

        private void Awake()
        {
            FixViewportMask();

            // Content của ScrollView đã có sẵn VerticalLayoutGroup + ContentSizeFitter (Vertical=Preferred).
            // Chỉ cần đảm bảo ContentSizeFitter tồn tại để Content tự cao theo nội dung → cuộn được.
            RectTransform content = ResolveContent();
            if (content == null) return;
            var csf = content.GetComponent<ContentSizeFitter>();
            if (csf == null) csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // GỐC RỄ vì sao Content vô hình: Viewport dùng legacy Mask + Image alpha=0.
        // Legacy Mask ghi stencil qua clip(color.a - threshold); alpha=0 => không ghi gì =>
        // mọi con fail stencil test "Comp Equal" => vô hình toàn bộ (prompt, ảnh, guess).
        // Thay bằng RectMask2D: clip theo rect, không stencil, không phụ thuộc alpha.
        private void FixViewportMask()
        {
            var scroll = GetComponentInChildren<ScrollRect>(true);
            if (scroll == null) return;
            var viewport = scroll.viewport != null ? scroll.viewport : (scroll.transform.Find("Viewport") as RectTransform);
            if (viewport == null) return;

            var legacy = viewport.GetComponent<Mask>();
            if (legacy != null) Destroy(legacy);

            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
        }

        private RectTransform ResolveContent()
        {
            foreach (var s in drawingSlots)
                if (s?.root != null) return s.root.transform.parent as RectTransform;
            return null;
        }

        // Cấp kích thước cho bức tranh + slot.
        // CỐT LÕI: DrawingSlot có HorizontalLayoutGroup (ControlWidth=0) khiến mọi anchor bị ghi đè
        // và DrawingImage rộng = preferredWidth (chưa set) = 0 → vô hình.
        // Cách dứt điểm: TẮT HLG của slot, kiểm soát image + label hoàn toàn bằng anchor.
        private void LayoutImage(DrawingLinkSlot dslot, Texture2D tex)
        {
            float aspect = (tex.width > 0) ? (float)tex.height / tex.width : 0.6f;
            float w = maxDrawingWidth;
            float h = w * aspect;
            if (h > maxDrawingHeight) { h = maxDrawingHeight; w = h / aspect; }
            float slotH = h + LabelHeight;

            // 1) Tắt layout group lồng trong slot — nguồn gốc xung đột.
            var hlg = dslot.root.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null) hlg.enabled = false;
            var vlgSlot = dslot.root.GetComponent<VerticalLayoutGroup>();
            if (vlgSlot != null) vlgSlot.enabled = false;

            // 2) Slot báo chiều cao cho VerticalLayoutGroup của Content (ControlHeight=1 nên sẽ đọc giá trị này).
            var rootLe = dslot.root.GetComponent<LayoutElement>();
            if (rootLe == null) rootLe = dslot.root.AddComponent<LayoutElement>();
            rootLe.minHeight = slotH;
            rootLe.preferredHeight = slotH;

            // 3) DrawingImage: căn trên-giữa slot, kích thước cố định w×h.
            var imgLe = dslot.drawingImage.GetComponent<LayoutElement>();
            if (imgLe != null) imgLe.ignoreLayout = true;
            var irt = dslot.drawingImage.rectTransform;
            irt.anchorMin = new Vector2(0.5f, 1f);
            irt.anchorMax = new Vector2(0.5f, 1f);
            irt.pivot = new Vector2(0.5f, 1f);
            irt.sizeDelta = new Vector2(w, h);
            irt.anchoredPosition = Vector2.zero;

            // 4) AuthorLabel: dải dưới cùng slot.
            if (dslot.authorLabel != null)
            {
                var le = dslot.authorLabel.GetComponent<LayoutElement>();
                if (le != null) le.ignoreLayout = true;
                var lrt = dslot.authorLabel.rectTransform;
                lrt.anchorMin = new Vector2(0f, 0f);
                lrt.anchorMax = new Vector2(1f, 0f);
                lrt.pivot = new Vector2(0.5f, 0f);
                lrt.sizeDelta = new Vector2(0f, LabelHeight);
                lrt.anchoredPosition = Vector2.zero;
            }
        }

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
            if (gsm == null || album == null || !gsm.Object.IsValid || !album.Object.IsValid)
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

                if (tex != null)
                {
                    if (dslot.drawingImage != null)
                    {
                        dslot.drawingImage.texture = tex;
                        dslot.drawingImage.uvRect = new Rect(0, 0, 1, 1);
                        dslot.drawingImage.enabled = true;
                        dslot.drawingImage.gameObject.SetActive(true);
                        dslot.drawingImage.color = Color.white;

                        LayoutImage(dslot, tex);
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

            // Ép layout tính lại sau khi thay đổi LayoutElement runtime, để ScrollView cấp đúng kích thước.
            var content = ResolveContent();
            if (content != null)
            {
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
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
