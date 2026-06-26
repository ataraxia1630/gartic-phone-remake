#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using InkEcho.UI.ContentDisplay;

namespace InkEcho.UI.Tests
{
    /// <summary>
    /// Unit Tests cho UIContentController
    /// Kiểm tra state transitions và content display
    /// </summary>
    public class UIContentControllerTests
    {
        private UIContentController _controller;
        private GameObject _promptPanel;
        private GameObject _drawingPanel;

        [SetUp]
        public void Setup()
        {
            // Tạo controller GameObject
            var controllerGO = new GameObject("UIContentController");
            _controller = controllerGO.AddComponent<UIContentController>();

            // Tạo prompt panel
            _promptPanel = new GameObject("PromptPanel");
            _promptPanel.AddComponent<CanvasGroup>();
            _promptPanel.SetActive(false);

            // Tạo drawing panel
            _drawingPanel = new GameObject("DrawingPanel");
            _drawingPanel.AddComponent<CanvasGroup>();
            _drawingPanel.SetActive(false);

            // Assign references
            _controller.PromptPanel = _promptPanel;
            _controller.DrawingPanel = _drawingPanel;
        }

        [TearDown]
        public void Teardown()
        {
            Object.Destroy(_controller.gameObject);
            Object.Destroy(_promptPanel);
            Object.Destroy(_drawingPanel);
        }

        // ========== DISPLAY TESTS ==========

        [Test]
        public void DisplayPrompt_SetsPromptPanelActive()
        {
            // Arrange
            _controller.SetContentType(ContentDisplayType.Prompt);

            // Act
            _controller.DisplayPreviousContent(0, 0, useRevealAnimation: false);

            // Assert
            Assert.IsTrue(_promptPanel.activeInHierarchy, "PromptPanel should be active");
            Assert.IsFalse(_drawingPanel.activeInHierarchy, "DrawingPanel should be inactive");
        }

        [Test]
        public void DisplayDrawing_SetsDrawingPanelActive()
        {
            // Arrange
            _controller.SetContentType(ContentDisplayType.Drawing);

            // Act
            _controller.DisplayPreviousContent(0, 0, useRevealAnimation: false);

            // Assert
            Assert.IsFalse(_promptPanel.activeInHierarchy, "PromptPanel should be inactive");
            Assert.IsTrue(_drawingPanel.activeInHierarchy, "DrawingPanel should be active");
        }

        // ========== HIDE TESTS ==========

        [Test]
        public void HideContent_HidesBothPanels()
        {
            // Arrange
            _controller.SetContentType(ContentDisplayType.Prompt);
            _controller.DisplayPreviousContent(0, 0, useRevealAnimation: false);

            // Act
            _controller.HideContent();

            // Assert
            Assert.IsFalse(_promptPanel.activeInHierarchy, "PromptPanel should be hidden");
            Assert.IsFalse(_drawingPanel.activeInHierarchy, "DrawingPanel should be hidden");
        }

        // ========== STATE TESTS ==========

        [Test]
        public void DisplayWithAnimation_InitiatesRevealState()
        {
            // Arrange
            _controller.SetContentType(ContentDisplayType.Prompt);
            _controller.SetRevealAnimation(RevealAnimationType.FadeIn);

            // Act
            _controller.DisplayPreviousContent(0, 0, useRevealAnimation: true);

            // Assert - Panel should be active during animation
            Assert.IsTrue(_promptPanel.activeInHierarchy, "Panel should be active during reveal");
        }

        [Test]
        public void DisplayWithoutAnimation_ShowsImmediately()
        {
            // Arrange
            _controller.SetContentType(ContentDisplayType.Prompt);

            // Act
            _controller.DisplayPreviousContent(0, 0, useRevealAnimation: false);

            // Assert - Panel should be fully visible
            var canvasGroup = _promptPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Assert.AreEqual(1f, canvasGroup.alpha, 0.01f, "Alpha should be 1.0 (fully visible)");
            }
        }

        // ========== CONTENT TYPE TESTS ==========

        [Test]
        public void SetContentType_ToPrompt_StoresPromptType()
        {
            // Act
            _controller.SetContentType(ContentDisplayType.Prompt);

            // Assert
            Assert.AreEqual(ContentDisplayType.Prompt, _controller.ContentType);
        }

        [Test]
        public void SetContentType_ToDrawing_StoresDrawingType()
        {
            // Act
            _controller.SetContentType(ContentDisplayType.Drawing);

            // Assert
            Assert.AreEqual(ContentDisplayType.Drawing, _controller.ContentType);
        }

        // ========== ANIMATION TYPE TESTS ==========

        [Test]
        public void SetRevealAnimation_FadeIn_StoreFadeInType()
        {
            // Act
            _controller.SetRevealAnimation(RevealAnimationType.FadeIn);

            // Assert
            Assert.AreEqual(RevealAnimationType.FadeIn, _controller.RevealAnimationType);
        }

        [Test]
        public void SetRevealAnimation_SlideInUp_StoreSlideInType()
        {
            // Act
            _controller.SetRevealAnimation(RevealAnimationType.SlideInUp);

            // Assert
            Assert.AreEqual(RevealAnimationType.SlideInUp, _controller.RevealAnimationType);
        }

        [Test]
        public void SetRevealAnimation_ScaleIn_StoreScaleInType()
        {
            // Act
            _controller.SetRevealAnimation(RevealAnimationType.ScaleIn);

            // Assert
            Assert.AreEqual(RevealAnimationType.ScaleIn, _controller.RevealAnimationType);
        }

        // ========== ROUND TRACKING TESTS ==========

        [Test]
        public void DisplayPreviousContent_StoresRoundAndChainIndex()
        {
            // Act
            _controller.DisplayPreviousContent(2, 5, useRevealAnimation: false);

            // Assert
            Assert.AreEqual(2, _controller.CurrentRound, "Round should be stored");
            Assert.AreEqual(5, _controller.CurrentChainIndex, "ChainIndex should be stored");
        }

        // ========== REVEAL DURATION TESTS ==========

        [Test]
        public void RevealDuration_CanBeSet()
        {
            // Act
            _controller.RevealDuration = 2.5f;

            // Assert
            Assert.AreEqual(2.5f, _controller.RevealDuration);
        }

        [Test]
        public void RevealDuration_DefaultIsOne()
        {
            // Assert
            Assert.AreEqual(1f, _controller.RevealDuration, 0.01f);
        }
    }
}
#endif