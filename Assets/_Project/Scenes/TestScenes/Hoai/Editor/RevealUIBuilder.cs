using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InkEcho.Hoai.UI;
using InkEcho.Hoai.Mock;

namespace InkEcho.Hoai.Editor
{
    // Tạo prefab UI cho Reveal phase qua menu.
    // InkEcho > Build UI > ① tạo RevealAlbumPanel (cần Fusion/server)
    //                     > ② tạo MockRevealPanel (test offline, không cần server)
    public static class RevealUIBuilder
    {
        private const int MaxDrawingSlots = 6;

        private const string RevealPanelPath = "Assets/_Project/Scenes/TestScenes/Hoai/UI/RevealAlbumPanel.prefab";
        private const string MockPanelPath   = "Assets/_Project/Scenes/TestScenes/Hoai/Mock/MockRevealPanel.prefab";

        // ─────────────────────────────────────────────────────────
        // 1. Network panel
        // ─────────────────────────────────────────────────────────
        [MenuItem("InkEcho/Build UI/① Reveal Album Panel (network)")]
        public static void BuildRevealAlbumPanel()
        {
            var root = BuildPanelRoot("RevealAlbumPanel", out var scrollContent, out var footer);
            var comp = root.AddComponent<RevealAlbumPanel>();

            // Header title
            var titleLabel = MakeLabel(root.transform.GetChild(0).gameObject, "TitleLabel",
                "Album của [Player]", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            LayoutEl(titleLabel.gameObject, preferredHeight: 44);

            // Prompt card (link 0)
            var promptCard = MakeCard(scrollContent, "PromptContainer", new Color(0.15f, 0.35f, 0.15f, 0.9f));
            promptCard.SetActive(true); // tạm enable cho dễ nhìn, panel sẽ toggle runtime
            var promptLabel = MakeLabel(promptCard, "PromptLabel",
                "\"Prompt gốc sẽ hiện ở đây\"", 18, FontStyles.Italic, TextAlignmentOptions.Center);
            LayoutEl(promptLabel.gameObject, preferredHeight: 30);
            var promptAuthorLabel = MakeLabel(promptCard, "PromptAuthorLabel",
                "— Tác giả", 13, FontStyles.Normal, TextAlignmentOptions.Right);
            LayoutEl(promptAuthorLabel.gameObject, preferredHeight: 22);
            LayoutEl(promptCard, preferredHeight: 76);

            // Drawing slots
            var drawingSlots = new RevealAlbumPanel.DrawingLinkSlot[MaxDrawingSlots];
            for (int i = 0; i < MaxDrawingSlots; i++)
            {
                var slotRoot = MakeDrawingCard(scrollContent, i,
                    out var rawImg, out var authorLbl);
                slotRoot.SetActive(false);
                drawingSlots[i] = new RevealAlbumPanel.DrawingLinkSlot
                {
                    root = slotRoot,
                    drawingImage = rawImg,
                    authorLabel = authorLbl,
                };
            }

            // Guess card (last link)
            var guessCard = MakeCard(scrollContent, "GuessContainer", new Color(0.4f, 0.2f, 0.05f, 0.9f));
            guessCard.SetActive(false);
            var finalGuessLabel = MakeLabel(guessCard, "FinalGuessLabel",
                "\"Câu đoán cuối cùng\"", 18, FontStyles.Italic, TextAlignmentOptions.Center);
            LayoutEl(finalGuessLabel.gameObject, preferredHeight: 30);
            var guessAuthorLabel = MakeLabel(guessCard, "GuessAuthorLabel",
                "— Người đoán", 13, FontStyles.Normal, TextAlignmentOptions.Right);
            LayoutEl(guessAuthorLabel.gameObject, preferredHeight: 22);
            LayoutEl(guessCard, preferredHeight: 76);

            // Footer: status (non-host) + host controls
            var statusLabel = MakeLabel(footer, "StatusLabel",
                "Chờ host mở...", 14, FontStyles.Normal, TextAlignmentOptions.Center);
            statusLabel.color = new Color(0.75f, 0.75f, 0.75f);
            var statusLE = statusLabel.gameObject.AddComponent<LayoutElement>();
            statusLE.flexibleWidth = 1;

            var hostControls = new GameObject("HostControls");
            hostControls.transform.SetParent(footer.transform, false);
            hostControls.AddComponent<RectTransform>();
            var hcLE = hostControls.AddComponent<LayoutElement>();
            hcLE.preferredWidth = 220;
            hcLE.flexibleWidth = 0;

            var nextBtn = MakeButton(hostControls, "NextButton", "Tiếp theo ▶",
                new Color(0.18f, 0.5f, 0.9f), out var nextBtnLabel);
            // stretch button to fill hostControls
            var btnRect = nextBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.sizeDelta = Vector2.zero;
            btnRect.anchoredPosition = Vector2.zero;

            // Wire all references via SerializedObject
            var so = new SerializedObject(comp);
            so.FindProperty("titleLabel").objectReferenceValue = titleLabel;
            so.FindProperty("promptContainer").objectReferenceValue = promptCard;
            so.FindProperty("promptLabel").objectReferenceValue = promptLabel;
            so.FindProperty("promptAuthorLabel").objectReferenceValue = promptAuthorLabel;
            so.FindProperty("guessContainer").objectReferenceValue = guessCard;
            so.FindProperty("finalGuessLabel").objectReferenceValue = finalGuessLabel;
            so.FindProperty("guessAuthorLabel").objectReferenceValue = guessAuthorLabel;
            so.FindProperty("hostControlsRoot").objectReferenceValue = hostControls;
            so.FindProperty("nextButton").objectReferenceValue = nextBtn.GetComponent<Button>();
            so.FindProperty("nextButtonLabel").objectReferenceValue = nextBtnLabel;
            so.FindProperty("statusLabel").objectReferenceValue = statusLabel;

            var slotsProp = so.FindProperty("drawingSlots");
            slotsProp.arraySize = MaxDrawingSlots;
            for (int i = 0; i < MaxDrawingSlots; i++)
            {
                var elem = slotsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("root").objectReferenceValue = drawingSlots[i].root;
                elem.FindPropertyRelative("drawingImage").objectReferenceValue = drawingSlots[i].drawingImage;
                elem.FindPropertyRelative("authorLabel").objectReferenceValue = drawingSlots[i].authorLabel;
            }
            so.ApplyModifiedProperties();

            SavePrefab(root, RevealPanelPath);
        }

        // ─────────────────────────────────────────────────────────
        // 2. Mock (offline) panel
        // ─────────────────────────────────────────────────────────
        [MenuItem("InkEcho/Build UI/② Mock Reveal Panel (offline test)")]
        public static void BuildMockRevealPanel()
        {
            var root = BuildPanelRoot("MockRevealPanel", out var scrollContent, out var footer);
            var comp = root.AddComponent<MockRevealDriver>();

            // Header: album index + title
            var headerGo = root.transform.GetChild(0).gameObject;
            var albumIndexLabel = MakeLabel(headerGo, "AlbumIndexLabel",
                "Album 1 / 3", 13, FontStyles.Normal, TextAlignmentOptions.Center);
            albumIndexLabel.color = new Color(0.7f, 0.7f, 0.7f);
            LayoutEl(albumIndexLabel.gameObject, preferredHeight: 20);
            var titleLabel = MakeLabel(headerGo, "TitleLabel",
                "Album của [Player]", 24, FontStyles.Bold, TextAlignmentOptions.Center);
            LayoutEl(titleLabel.gameObject, preferredHeight: 38);

            // Prompt card
            var promptCard = MakeCard(scrollContent, "PromptContainer", new Color(0.15f, 0.35f, 0.15f, 0.9f));
            var promptLabel = MakeLabel(promptCard, "PromptLabel",
                "\"Prompt gốc\"", 18, FontStyles.Italic, TextAlignmentOptions.Center);
            LayoutEl(promptLabel.gameObject, preferredHeight: 30);
            var promptAuthorLabel = MakeLabel(promptCard, "PromptAuthorLabel",
                "— Chủ album", 13, FontStyles.Normal, TextAlignmentOptions.Right);
            LayoutEl(promptAuthorLabel.gameObject, preferredHeight: 22);
            LayoutEl(promptCard, preferredHeight: 76);

            // Drawing slots
            var drawingSlots = new MockRevealDriver.DrawingSlot[MaxDrawingSlots];
            for (int i = 0; i < MaxDrawingSlots; i++)
            {
                var slotRoot = MakeDrawingCard(scrollContent, i,
                    out var rawImg, out var authorLbl);
                slotRoot.SetActive(false);
                drawingSlots[i] = new MockRevealDriver.DrawingSlot
                {
                    root = slotRoot,
                    drawingImage = rawImg,
                    authorLabel = authorLbl,
                };
            }

            // Guess card
            var guessCard = MakeCard(scrollContent, "GuessContainer", new Color(0.4f, 0.2f, 0.05f, 0.9f));
            guessCard.SetActive(false);
            var finalGuessLabel = MakeLabel(guessCard, "FinalGuessLabel",
                "\"Câu đoán\"", 18, FontStyles.Italic, TextAlignmentOptions.Center);
            LayoutEl(finalGuessLabel.gameObject, preferredHeight: 30);
            var guessAuthorLabel = MakeLabel(guessCard, "GuessAuthorLabel",
                "— Người đoán", 13, FontStyles.Normal, TextAlignmentOptions.Right);
            LayoutEl(guessAuthorLabel.gameObject, preferredHeight: 22);
            LayoutEl(guessCard, preferredHeight: 76);

            // Footer: next button only (no status label, mock is always "host")
            var nextBtn = MakeButton(footer, "NextButton", "Tiếp theo ▶",
                new Color(0.18f, 0.5f, 0.9f), out var nextBtnLabel);
            var btnLE = nextBtn.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 220;
            btnLE.preferredHeight = 44;

            // Wire
            var so = new SerializedObject(comp);
            so.FindProperty("titleLabel").objectReferenceValue = titleLabel;
            so.FindProperty("albumIndexLabel").objectReferenceValue = albumIndexLabel;
            so.FindProperty("promptContainer").objectReferenceValue = promptCard;
            so.FindProperty("promptLabel").objectReferenceValue = promptLabel;
            so.FindProperty("promptAuthorLabel").objectReferenceValue = promptAuthorLabel;
            so.FindProperty("guessContainer").objectReferenceValue = guessCard;
            so.FindProperty("finalGuessLabel").objectReferenceValue = finalGuessLabel;
            so.FindProperty("guessAuthorLabel").objectReferenceValue = guessAuthorLabel;
            so.FindProperty("nextButton").objectReferenceValue = nextBtn.GetComponent<Button>();
            so.FindProperty("nextButtonLabel").objectReferenceValue = nextBtnLabel;

            var slotsProp = so.FindProperty("drawingSlots");
            slotsProp.arraySize = MaxDrawingSlots;
            for (int i = 0; i < MaxDrawingSlots; i++)
            {
                var elem = slotsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("root").objectReferenceValue = drawingSlots[i].root;
                elem.FindPropertyRelative("drawingImage").objectReferenceValue = drawingSlots[i].drawingImage;
                elem.FindPropertyRelative("authorLabel").objectReferenceValue = drawingSlots[i].authorLabel;
            }
            so.ApplyModifiedProperties();

            SavePrefab(root, MockPanelPath);
        }

        // ─────────────────────────────────────────────────────────
        // Shared helpers
        // ─────────────────────────────────────────────────────────

        // Creates the outer panel shell: dark bg + VerticalLayoutGroup,
        // header child, ScrollRect (content returned), footer child.
        private static GameObject BuildPanelRoot(string name,
            out GameObject scrollContent, out GameObject footer)
        {
            var root = new GameObject(name);
            var rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.sizeDelta = Vector2.zero;

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.97f);

            var vl = root.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(0, 0, 0, 0);
            vl.spacing = 0;
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            // Header
            var header = new GameObject("Header");
            header.transform.SetParent(root.transform, false);
            header.AddComponent<RectTransform>();
            var headerImg = header.AddComponent<Image>();
            headerImg.color = new Color(0.05f, 0.05f, 0.1f, 1f);
            var headerVL = header.AddComponent<VerticalLayoutGroup>();
            headerVL.padding = new RectOffset(16, 16, 10, 10);
            headerVL.childControlWidth = true;
            headerVL.childControlHeight = false;
            headerVL.childForceExpandWidth = true;
            headerVL.childForceExpandHeight = false;
            var headerLE = header.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 70;
            headerLE.flexibleHeight = 0;

            // ScrollView
            scrollContent = BuildScrollView(root, out _);

            // Footer
            footer = new GameObject("Footer");
            footer.transform.SetParent(root.transform, false);
            footer.AddComponent<RectTransform>();
            var footerImg = footer.AddComponent<Image>();
            footerImg.color = new Color(0.05f, 0.05f, 0.1f, 1f);
            var footerHL = footer.AddComponent<HorizontalLayoutGroup>();
            footerHL.padding = new RectOffset(16, 16, 8, 8);
            footerHL.spacing = 12;
            footerHL.childAlignment = TextAnchor.MiddleCenter;
            footerHL.childControlWidth = false;
            footerHL.childControlHeight = true;
            footerHL.childForceExpandWidth = false;
            footerHL.childForceExpandHeight = true;
            var footerLE = footer.AddComponent<LayoutElement>();
            footerLE.preferredHeight = 60;
            footerLE.flexibleHeight = 0;

            return root;
        }

        private static GameObject BuildScrollView(GameObject parent, out GameObject viewport)
        {
            var scrollGo = new GameObject("ScrollView");
            scrollGo.transform.SetParent(parent.transform, false);
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            var scrollLE = scrollGo.AddComponent<LayoutElement>();
            scrollLE.flexibleHeight = 1;
            scrollLE.flexibleWidth = 1;

            // Viewport
            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollGo.transform, false);
            var vpRect = vp.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            vp.AddComponent<RectMask2D>();
            viewport = vp;

            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = Vector2.zero;

            var vl = content.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(16, 16, 12, 12);
            vl.spacing = 10;
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = vpRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30;

            return content;
        }

        private static GameObject MakeCard(GameObject parent, string name, Color bgColor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var vl = go.AddComponent<VerticalLayoutGroup>();
            vl.padding = new RectOffset(14, 14, 10, 10);
            vl.spacing = 6;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            return go;
        }

        private static GameObject MakeDrawingCard(GameObject parent, int index,
            out RawImage rawImage, out TextMeshProUGUI authorLabel)
        {
            var card = new GameObject($"DrawingSlot_{index}");
            card.transform.SetParent(parent.transform, false);
            card.AddComponent<RectTransform>();
            var cardImg = card.AddComponent<Image>();
            cardImg.color = new Color(0.12f, 0.12f, 0.22f, 0.9f);
            var hl = card.AddComponent<HorizontalLayoutGroup>();
            hl.padding = new RectOffset(10, 10, 10, 10);
            hl.spacing = 14;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.childControlHeight = true;
            hl.childControlWidth = true;
            hl.childForceExpandHeight = true;
            hl.childForceExpandWidth = false;
            LayoutEl(card, preferredHeight: 160);

            // Drawing image (left, fixed width)
            var imgGo = new GameObject("DrawingImage");
            imgGo.transform.SetParent(card.transform, false);
            imgGo.AddComponent<RectTransform>();
            rawImage = imgGo.AddComponent<RawImage>();
            rawImage.color = Color.white;
            var imgLE = imgGo.AddComponent<LayoutElement>();
            imgLE.preferredWidth = 140;
            imgLE.flexibleWidth = 0;
            imgLE.flexibleHeight = 1;

            // Author label (right, fills remaining)
            var authorGo = new GameObject("AuthorLabel");
            authorGo.transform.SetParent(card.transform, false);
            authorGo.AddComponent<RectTransform>();
            var tmp = authorGo.AddComponent<TextMeshProUGUI>();
            tmp.text = $"Vẽ bởi: Player {index + 1}";
            tmp.fontSize = 14;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            var authorLE = authorGo.AddComponent<LayoutElement>();
            authorLE.flexibleWidth = 1;
            authorLabel = tmp;

            return card;
        }

        private static TextMeshProUGUI MakeLabel(GameObject parent, string name,
            string text, int fontSize, FontStyles style, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = align;
            return tmp;
        }

        private static GameObject MakeButton(GameObject parent, string name,
            string label, Color bgColor, out TextMeshProUGUI labelTmp)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.2f;
            colors.pressedColor = bgColor * 0.7f;
            colors.selectedColor = bgColor;
            btn.colors = colors;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            labelTmp = textGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 15;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.color = Color.white;
            labelTmp.alignment = TextAlignmentOptions.Center;

            return go;
        }

        private static void LayoutEl(GameObject go, float preferredHeight = -1, float preferredWidth = -1)
        {
            var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            if (preferredHeight >= 0) le.preferredHeight = preferredHeight;
            if (preferredWidth >= 0) le.preferredWidth = preferredWidth;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            System.IO.Directory.CreateDirectory(
                System.IO.Path.GetDirectoryName(
                    System.IO.Path.GetFullPath(path)));
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
            Debug.Log($"[RevealUIBuilder] Prefab saved → {path}");
        }
    }
}
