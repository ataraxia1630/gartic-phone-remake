using UnityEngine;
using System.Collections;
using InkEcho.UI.ContentDisplay;

namespace InkEcho.UI
{
    /// <summary>
    /// Manual Test Script - Chạy trong Editor để test round transitions
    /// 
    /// Cách sử dụng:
    /// 1. Tạo empty GameObject "DebugTest"
    /// 2. Attach script này
    /// 3. Assign references trong Inspector
    /// 4. Bấm Play
    /// 5. Nhấn phím theo hướng dẫn trong Console
    /// </summary>
    public class UIContentDebugTester : MonoBehaviour
    {
        [SerializeField] private UIContentController uiContentController;
        [SerializeField] private float autoPlayDuration = 3f;

        private int currentRound = 0;
        private int currentChain = 0;
        private bool isAutoPlaying = false;

        private void Start()
        {
            PrintInstructions();
        }

        private void Update()
        {
            // Round transitions
            if (Input.GetKeyDown(KeyCode.R))
                TestShowPrompt();

            if (Input.GetKeyDown(KeyCode.D))
                TestShowDrawing();

            if (Input.GetKeyDown(KeyCode.H))
                TestHideContent();

            if (Input.GetKeyDown(KeyCode.F))
                TestFadeInAnimation();

            if (Input.GetKeyDown(KeyCode.S))
                TestSlideInAnimation();

            if (Input.GetKeyDown(KeyCode.C))
                TestScaleInAnimation();

            if (Input.GetKeyDown(KeyCode.A))
                TestAutoPlay();

            if (Input.GetKeyDown(KeyCode.N))
                NextRound();

            if (Input.GetKeyDown(KeyCode.P))
                PreviousRound();

            if (Input.GetKeyDown(KeyCode.I))
                PrintInstructions();

            if (Input.GetKeyDown(KeyCode.Escape))
                StopAutoPlay();
        }

        // ========== TEST METHODS ==========

        private void TestShowPrompt()
        {
            Debug.Log($"[TEST] Showing PROMPT - Round {currentRound}, Chain {currentChain}");
            uiContentController.SetContentType(ContentDisplayType.Prompt);
            uiContentController.DisplayPreviousContent(currentRound, currentChain, useRevealAnimation: false);

            VerifyPanelState(true, false);
        }

        private void TestShowDrawing()
        {
            Debug.Log($"[TEST] Showing DRAWING - Round {currentRound}, Chain {currentChain}");
            uiContentController.SetContentType(ContentDisplayType.Drawing);
            uiContentController.DisplayPreviousContent(currentRound, currentChain, useRevealAnimation: false);

            VerifyPanelState(false, true);
        }

        private void TestHideContent()
        {
            Debug.Log("[TEST] Hiding ALL content");
            uiContentController.HideContent();

            VerifyPanelState(false, false);
        }

        private void TestFadeInAnimation()
        {
            Debug.Log("[TEST] Testing FADE-IN animation");
            uiContentController.SetRevealAnimation(RevealAnimationType.FadeIn);
            uiContentController.SetContentType(ContentDisplayType.Prompt);
            uiContentController.DisplayPreviousContent(currentRound, currentChain, useRevealAnimation: true);

            Debug.Log($"[TEST] Animation starting (duration: {uiContentController.RevealDuration}s)");
        }

        private void TestSlideInAnimation()
        {
            Debug.Log("[TEST] Testing SLIDE-IN animation");
            uiContentController.SetRevealAnimation(RevealAnimationType.SlideInUp);
            uiContentController.SetContentType(ContentDisplayType.Prompt);
            uiContentController.DisplayPreviousContent(currentRound, currentChain, useRevealAnimation: true);

            Debug.Log($"[TEST] Animation starting (duration: {uiContentController.RevealDuration}s)");
        }

        private void TestScaleInAnimation()
        {
            Debug.Log("[TEST] Testing SCALE-IN animation");
            uiContentController.SetRevealAnimation(RevealAnimationType.ScaleIn);
            uiContentController.SetContentType(ContentDisplayType.Prompt);
            uiContentController.DisplayPreviousContent(currentRound, currentChain, useRevealAnimation: true);

            Debug.Log($"[TEST] Animation starting (duration: {uiContentController.RevealDuration}s)");
        }

        private void TestAutoPlay()
        {
            if (isAutoPlaying)
            {
                StopAutoPlay();
                return;
            }

            Debug.Log("[TEST] Starting AUTO PLAY - Full game simulation");
            isAutoPlaying = true;
            currentRound = 0;
            currentChain = 0;

            StartCoroutine(AutoPlaySequence());
        }

        private System.Collections.IEnumerator AutoPlaySequence()
        {
            // Round 0: Show prompt
            Debug.Log($"\n[AUTO] Round 0: Show PROMPT");
            uiContentController.SetContentType(ContentDisplayType.Prompt);
            uiContentController.DisplayPreviousContent(0, 0, useRevealAnimation: true);
            yield return new WaitForSeconds(autoPlayDuration);

            // Round 0: Hide
            Debug.Log($"[AUTO] Round 0: Hide content");
            uiContentController.HideContent();
            yield return new WaitForSeconds(1f);

            // Round 1: Show drawing
            Debug.Log($"[AUTO] Round 1: Show DRAWING");
            uiContentController.SetRevealAnimation(RevealAnimationType.SlideInUp);
            uiContentController.SetContentType(ContentDisplayType.Drawing);
            uiContentController.DisplayPreviousContent(1, 0, useRevealAnimation: true);
            yield return new WaitForSeconds(autoPlayDuration);

            // Round 1: Hide
            Debug.Log($"[AUTO] Round 1: Hide content");
            uiContentController.HideContent();
            yield return new WaitForSeconds(1f);

            // Round 2: Show prompt again
            Debug.Log($"[AUTO] Round 2: Show PROMPT");
            uiContentController.SetRevealAnimation(RevealAnimationType.ScaleIn);
            uiContentController.SetContentType(ContentDisplayType.Prompt);
            uiContentController.DisplayPreviousContent(2, 0, useRevealAnimation: true);
            yield return new WaitForSeconds(autoPlayDuration);

            // End
            Debug.Log($"[AUTO] End: Hide content");
            uiContentController.HideContent();

            isAutoPlaying = false;
            Debug.Log("[AUTO] Sequence completed!");
        }

        private void NextRound()
        {
            currentRound++;
            Debug.Log($"[TEST] Round advanced to: {currentRound}");
        }

        private void PreviousRound()
        {
            currentRound = Mathf.Max(0, currentRound - 1);
            Debug.Log($"[TEST] Round reverted to: {currentRound}");
        }

        // ========== VERIFICATION METHODS ==========

        private void VerifyPanelState(bool promptExpected, bool drawingExpected)
        {
            bool promptActive = uiContentController.PromptPanel?.activeInHierarchy ?? false;
            bool drawingActive = uiContentController.DrawingPanel?.activeInHierarchy ?? false;

            string status = (promptActive == promptExpected && drawingActive == drawingExpected)
                ? "✅ PASS"
                : "❌ FAIL";

            Debug.Log($"{status} | Prompt: {promptActive} (expected {promptExpected}) | Drawing: {drawingActive} (expected {drawingExpected})");
        }

        private void StopAutoPlay()
        {
            if (isAutoPlaying)
            {
                isAutoPlaying = false;
                StopAllCoroutines();
                Debug.Log("[TEST] Auto play stopped");
            }
        }

        private void PrintInstructions()
        {
            string instructions = string.Format(@"
╔════════════════════════════════════════════════════════════╗
║   UI CONTENT DISPLAY - DEBUG TEST CONTROLLER              ║
╠════════════════════════════════════════════════════════════╣
║                                                            ║
║  DISPLAY TESTS:                                           ║
║    R - Show PROMPT (no animation)                         ║
║    D - Show DRAWING (no animation)                        ║
║    H - HIDE all content                                   ║
║                                                            ║
║  ANIMATION TESTS:                                         ║
║    F - Test FADE-IN animation                            ║
║    S - Test SLIDE-IN animation                           ║
║    C - Test SCALE-IN animation                           ║
║                                                            ║
║  ROUND NAVIGATION:                                        ║
║    N - Next round                                         ║
║    P - Previous round                                     ║
║    A - Start AUTO PLAY (full sequence)                   ║
║                                                            ║
║  OTHER:                                                    ║
║    I - Print this help                                    ║
║    ESC - Stop auto play                                   ║
║                                                            ║
║  Current: Round {0}, Chain {1}                           ║
║                                                            ║
╚════════════════════════════════════════════════════════════╝
", currentRound, currentChain);

            Debug.Log(instructions);
        }
    }
}
