using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InkEcho.Hoai.Mock
{
    // Standalone test driver — không cần Fusion / server.
    // Gắn vào một GameObject trong test scene, kéo MockAlbumData và tất cả UI refs vào Inspector.
    // Bấm nút Next trong Play Mode để lần lượt reveal từng link của từng album.
    public class MockRevealDriver : MonoBehaviour
    {
        [System.Serializable]
        public class DrawingSlot
        {
            public GameObject root;
            public RawImage drawingImage;
            public TextMeshProUGUI authorLabel;
        }

        [Header("Data")]
        [SerializeField] private MockAlbumData mockData;

        [Header("Header")]
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI albumIndexLabel;

        [Header("Prompt (link 0)")]
        [SerializeField] private GameObject promptContainer;
        [SerializeField] private TextMeshProUGUI promptLabel;
        [SerializeField] private TextMeshProUGUI promptAuthorLabel;

        [Header("Drawings (link 1..N-1) — tối đa bằng số ô trong drawingSlots")]
        [SerializeField] private DrawingSlot[] drawingSlots;

        [Header("Final Guess (link cuối)")]
        [SerializeField] private GameObject guessContainer;
        [SerializeField] private TextMeshProUGUI finalGuessLabel;
        [SerializeField] private TextMeshProUGUI guessAuthorLabel;

        [Header("Controls")]
        [SerializeField] private Button nextButton;
        [SerializeField] private TextMeshProUGUI nextButtonLabel;

        private int _albumIndex;
        private int _linkIndex; // 0=prompt, 1..M=drawing[i-1], M+1=guess

        private void Start()
        {
            if (nextButton != null) nextButton.onClick.AddListener(OnNext);
            _albumIndex = 0;
            _linkIndex = 0;
            Refresh();
            Canvas.ForceUpdateCanvases();
        }

        private void OnNext()
        {
            if (mockData == null || mockData.albums == null || mockData.albums.Length == 0) return;
            var album = mockData.albums[_albumIndex];
            int lastLink = album.drawings != null ? album.drawings.Length + 1 : 1;

            if (_linkIndex < lastLink)
            {
                _linkIndex++;
            }
            else
            {
                _albumIndex++;
                _linkIndex = 0;
                if (_albumIndex >= mockData.albums.Length)
                {
                    Debug.Log("[MockReveal] Tất cả album đã reveal xong.");
                    if (nextButton != null) nextButton.interactable = false;
                    return;
                }
            }
            Refresh();
        }

        private void Refresh()
        {
            if (mockData == null || mockData.albums == null || mockData.albums.Length == 0)
            {
                if (titleLabel != null) titleLabel.text = "(No MockAlbumData)";
                return;
            }

            var album = mockData.albums[_albumIndex];
            int drawingCount = album.drawings != null ? album.drawings.Length : 0;
            int lastLink = drawingCount + 1;

            if (albumIndexLabel != null)
                albumIndexLabel.text = $"Album {_albumIndex + 1} / {mockData.albums.Length}";

            if (titleLabel != null)
                titleLabel.text = $"{album.ownerName}'s Album";

            // Prompt — hiện ngay khi mở album (linkIndex >= 0 luôn đúng)
            if (promptContainer != null) promptContainer.SetActive(true);
            if (promptLabel != null) promptLabel.text = $"\"{album.originalPrompt}\"";
            if (promptAuthorLabel != null) promptAuthorLabel.text = $"— {album.ownerName}";

            // Drawings
            for (int i = 0; i < drawingSlots.Length; i++)
            {
                var dslot = drawingSlots[i];
                if (dslot?.root == null) continue;
                bool show = i < drawingCount && _linkIndex >= i + 1;
                dslot.root.SetActive(show);
                if (!show) continue;

                var entry = album.drawings[i];
                if (dslot.drawingImage != null)
                {
                    var tex = entry.image != null ? entry.image : MakePlaceholderTexture(i);
                    dslot.drawingImage.texture = tex;
                    dslot.drawingImage.color = Color.white;
                    dslot.drawingImage.enabled = true;
                }
                if (dslot.authorLabel != null)
                    dslot.authorLabel.text = $"Drawn by: {entry.drawerName}";
            }

            // Final guess
            bool showGuess = _linkIndex >= lastLink;
            if (guessContainer != null) guessContainer.SetActive(showGuess);
            if (showGuess)
            {
                if (finalGuessLabel != null) finalGuessLabel.text = $"\"{album.finalGuess}\"";
                if (guessAuthorLabel != null) guessAuthorLabel.text = $"— {album.guesserName}";
            }

            // Button label
            if (nextButtonLabel != null)
            {
                bool allShown = _linkIndex >= lastLink;
                bool isLastAlbum = _albumIndex >= mockData.albums.Length - 1;
                if (allShown && isLastAlbum) nextButtonLabel.text = "Finish";
                else if (allShown) nextButtonLabel.text = "Next Album";
                else nextButtonLabel.text = "Next";
            }

            Canvas.ForceUpdateCanvases();
        }

        private static Texture2D MakePlaceholderTexture(int slotIndex)
        {
            var colors = new[] { Color.cyan, Color.green, Color.yellow, Color.magenta, Color.blue, Color.red };
            var fill = colors[slotIndex % colors.Length];
            var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var pixels = new Color[128 * 128];
            for (int p = 0; p < pixels.Length; p++) pixels[p] = fill;
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
