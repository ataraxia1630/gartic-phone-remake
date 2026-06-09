using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.StateMachine;
using InkEcho.Network.Players;
using InkEcho.Gameplay.Data;

namespace InkEcho.UI
{
    /// <summary>
    /// Đặt trong ResultScene. Lắng nghe GameStateMachine.RevealAlbumIndex / RevealLinkIndex
    /// để hiển thị từng entry trong album theo kiểu step-by-step.
    /// Host nhấn nút "Next" để advance. Tất cả client nhìn thấy cùng nội dung.
    /// </summary>
    public class RevealAlbumController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI albumTitleText;
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private RawImage drawingImage;
        [SerializeField] private TextMeshProUGUI guessText;
        [SerializeField] private TextMeshProUGUI linkInfoText;

        [Header("Panels")]
        [SerializeField] private GameObject promptPanel;
        [SerializeField] private GameObject drawingPanel;
        [SerializeField] private GameObject guessPanel;

        [Header("Host Controls")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button returnToLobbyButton;
        [SerializeField] private GameObject revealFinishedPanel;

        private byte _lastAlbumIndex = byte.MaxValue;
        private byte _lastLinkIndex = byte.MaxValue;
        private bool _lastIsFinished;

        // Retry state for drawing display
        private bool _pendingDrawing;
        private byte _pendingChainLink;
        private byte _pendingOriginSlot;
        private Texture2D _shownTexture;

        private void Start()
        {
            // Setup button listeners
            if (nextButton != null)
                nextButton.onClick.AddListener(OnNextClicked);

            if (returnToLobbyButton != null)
                returnToLobbyButton.onClick.AddListener(OnReturnToLobbyClicked);

            // Hide all panels initially
            HideAllPanels();

            Debug.Log("[RevealAlbumController] Initialized");
        }

        private void Update()
        {
            var gsm = ServiceLocator.Get<GameStateMachine>();
            if (gsm == null) return;

            // Chỉ hiển thị host controls cho host
            bool isHost = gsm.HasStateAuthority;
            if (nextButton != null) nextButton.gameObject.SetActive(isHost && !gsm.IsRevealFinished);
            if (returnToLobbyButton != null) returnToLobbyButton.gameObject.SetActive(isHost && gsm.IsRevealFinished);
            if (revealFinishedPanel != null) revealFinishedPanel.SetActive(gsm.IsRevealFinished);

            // Poll pending drawing texture
            if (_pendingDrawing)
                TryShowPendingDrawing();
            // Check nếu index thay đổi → cập nhật UI
            if (gsm.RevealAlbumIndex != _lastAlbumIndex ||
                gsm.RevealLinkIndex != _lastLinkIndex ||
                gsm.IsRevealFinished != _lastIsFinished)
            {
                _lastAlbumIndex = gsm.RevealAlbumIndex;
                _lastLinkIndex = gsm.RevealLinkIndex;
                _lastIsFinished = gsm.IsRevealFinished;

                if (!gsm.IsRevealFinished)
                {
                    DisplayCurrentEntry(gsm.RevealAlbumIndex, gsm.RevealLinkIndex);
                }
                else
                {
                    HideAllPanels();
                    Debug.Log("[RevealAlbumController] All albums revealed!");
                }
            }
        }

        private void DisplayCurrentEntry(byte albumIndex, byte linkIndex)
        {
            var album = ServiceLocator.Get<AlbumStore>();
            if (album == null)
            {
                Debug.LogWarning("[RevealAlbumController] AlbumStore not found!");
                return;
            }

            var entry = album.GetEntry(linkIndex, albumIndex);

            // Update album title
            if (albumTitleText != null)
            {
                var registry = ServiceLocator.Get<PlayerRegistry>();
                string ownerName = "Player " + (albumIndex + 1);
                if (registry != null && registry.TryGetPlayerBySlotIndex(albumIndex, out var ownerPlayer))
                {
                    if (registry.TryGetSlot(ownerPlayer, out var slot))
                        ownerName = slot.DisplayName.ToString();
                }
                albumTitleText.text = $"Album: {ownerName} ({albumIndex + 1}/{album.PlayerCount})";
            }

            // Update link info
            if (linkInfoText != null)
                linkInfoText.text = $"Step {linkIndex + 1} / {album.LinksPerChain}";

            // Determine what to show based on link index
            // Link 0 = Prompt, odd links = Drawing, even links = Guess/Prompt, last link = FinalGuess
            HideAllPanels();

            string promptStr = entry.Prompt.ToString();
            bool hasPrompt = !string.IsNullOrEmpty(promptStr);
            bool hasDrawing = entry.DrawingStrokes > 0 || entry.DrawingHash != 0
                || DrawingTextureStore.GetTexture(linkIndex, albumIndex) != null
                || DrawingStrokeStore.HasStrokes(linkIndex, albumIndex);
            string guess0 = entry.GuessRole0.ToString();
            string guess1 = entry.GuessRole1.ToString();
            bool hasGuess = !string.IsNullOrEmpty(guess0) || !string.IsNullOrEmpty(guess1);

            if (linkIndex == 0)
            {
                // First link is always the original prompt
                ShowPrompt(promptStr, "Original Prompt");
            }
            else if (hasDrawing)
            {
                // Show drawing
                ShowDrawing(linkIndex, albumIndex);
            }
            else if (hasGuess)
            {
                // Show guess
                ShowGuess(guess0, guess1);
            }
            else if (hasPrompt)
            {
                // Fallback to prompt if available
                ShowPrompt(promptStr, "Prompt");
            }
            else
            {
                Debug.LogWarning($"[RevealAlbumController] No content for album={albumIndex}, link={linkIndex}");
            }

            Debug.Log($"[RevealAlbumController] Displaying album={albumIndex}, link={linkIndex}, " +
                       $"prompt=\"{promptStr}\", hasDrawing={hasDrawing}, guess0=\"{guess0}\", guess1=\"{guess1}\"");
        }

        private void ShowPrompt(string text, string label = "Prompt")
        {
            if (promptPanel != null) promptPanel.SetActive(true);
            if (promptText != null) promptText.text = string.IsNullOrEmpty(text) ? "(empty)" : text;
        }

        private void ShowDrawing(byte chainLink, byte originSlot)
        {
            if (drawingPanel != null) drawingPanel.SetActive(true);

            _pendingDrawing = true;
            _pendingChainLink = chainLink;
            _pendingOriginSlot = originSlot;
            _shownTexture = null;

            TryShowPendingDrawing();
        }

        private void TryShowPendingDrawing()
        {
            if (!_pendingDrawing || drawingImage == null) return;

            var tex = DrawingTextureStore.GetTexture(_pendingChainLink, _pendingOriginSlot);
            if (tex != null)
            {
                if (tex == _shownTexture) return;
                _shownTexture = tex;
                drawingImage.texture = tex;
                drawingImage.gameObject.SetActive(true);
                _pendingDrawing = false;
                Debug.Log($"[RevealAlbumController] Loaded texture: chainLink={_pendingChainLink}, originSlot={_pendingOriginSlot}");
                return;
            }

            // Try rendering from stroke data
            var strokes = DrawingStrokeStore.GetStrokes(_pendingChainLink, _pendingOriginSlot);
            if (strokes != null && strokes.Count > 0)
            {
                var renderedTex = RenderStrokesToTexture(strokes,
                    DrawingStrokeStore.GetStrokeColors(_pendingChainLink, _pendingOriginSlot));
                if (renderedTex != null)
                {
                    _shownTexture = renderedTex;
                    drawingImage.texture = renderedTex;
                    drawingImage.gameObject.SetActive(true);
                    _pendingDrawing = false;
                    Debug.Log($"[RevealAlbumController] Rendered from strokes: chainLink={_pendingChainLink}, originSlot={_pendingOriginSlot}, strokes={strokes.Count}");
                    return;
                }
            }
            // No data yet — keep polling (drawingImage stays hidden)
            drawingImage.gameObject.SetActive(false);
            Debug.Log($"[RevealAlbumController] Waiting for drawing data: chainLink={_pendingChainLink}, originSlot={_pendingOriginSlot}");
        }

        private void ShowGuess(string guess0, string guess1)
        {
            if (guessPanel != null) guessPanel.SetActive(true);
            string combined = "";
            if (!string.IsNullOrEmpty(guess0)) combined += guess0;
            if (!string.IsNullOrEmpty(guess0) && !string.IsNullOrEmpty(guess1)) combined += " + ";
            if (!string.IsNullOrEmpty(guess1)) combined += guess1;
            if (string.IsNullOrEmpty(combined)) combined = "(no guess)";
            if (guessText != null) guessText.text = combined;
        }

        private void HideAllPanels()
        {
            _pendingDrawing = false;
            if (promptPanel != null) promptPanel.SetActive(false);
            if (drawingPanel != null) drawingPanel.SetActive(false);
            if (guessPanel != null) guessPanel.SetActive(false);
        }

        private void OnNextClicked()
        {
            var gsm = ServiceLocator.Get<GameStateMachine>();
            if (gsm == null) return;
            gsm.Rpc_RevealNext();
        }

        private void OnReturnToLobbyClicked()
        {
            var gsm = ServiceLocator.Get<GameStateMachine>();
            if (gsm == null) return;
            gsm.Rpc_RequestReturnToLobby();
        }

        /// <summary>
        /// Render viewport-UV strokes onto a Texture2D for display in RawImage.
        /// </summary>
        private Texture2D RenderStrokesToTexture(
            System.Collections.Generic.IReadOnlyList<System.Collections.Generic.List<Vector3>> strokes,
            System.Collections.Generic.IReadOnlyList<Color> colors)
        {
            int width = 512;
            int height = 512;
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];

            // Fill with white background
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            for (int s = 0; s < strokes.Count; s++)
            {
                var stroke = strokes[s];
                var color = (colors != null && s < colors.Count) ? colors[s] : Color.black;

                for (int p = 1; p < stroke.Count; p++)
                {
                    // Viewport UV (0..1) → pixel coords
                    int x0 = Mathf.Clamp(Mathf.RoundToInt(stroke[p - 1].x * (width - 1)), 0, width - 1);
                    int y0 = Mathf.Clamp(Mathf.RoundToInt(stroke[p - 1].y * (height - 1)), 0, height - 1);
                    int x1 = Mathf.Clamp(Mathf.RoundToInt(stroke[p].x * (width - 1)), 0, width - 1);
                    int y1 = Mathf.Clamp(Mathf.RoundToInt(stroke[p].y * (height - 1)), 0, height - 1);

                    DrawLine(pixels, width, height, x0, y0, x1, y1, color, 2);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Bresenham line with thickness.
        /// </summary>
        private static void DrawLine(Color[] pixels, int w, int h,
            int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                // Draw thick point
                for (int tx = -thickness; tx <= thickness; tx++)
                {
                    for (int ty = -thickness; ty <= thickness; ty++)
                    {
                        int px = x0 + tx;
                        int py = y0 + ty;
                        if (px >= 0 && px < w && py >= 0 && py < h)
                            pixels[py * w + px] = color;
                    }
                }

                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }
    }
}
