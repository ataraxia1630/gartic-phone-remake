using InkEcho.UI.ContentDisplay;
using UnityEngine;
using System.Collections;

namespace InkEcho.UI
{
    /// <summary>
    /// Helper script để dễ dàng integrate UIContentController vào Phase scenes
    /// 
    /// Cách sử dụng:
    /// 1. Add component này vào phase GameObject
    /// 2. Assign UIContentController reference
    /// 3. Call ShowPreviousContent() trong OnEnter phase
    /// 4. Tự động hide sau duration
    /// </summary>
    public class PhaseUIHelper : MonoBehaviour
    {
        [SerializeField] private UIContentController uiContentController;
        [SerializeField] private float displayDuration = 3f;

        public void ShowPrompt(int roundIndex, int chainIndex = 0)
        {
            if (uiContentController == null)
            {
                Debug.LogWarning("[PhaseUIHelper] UIContentController not assigned!");
                return;
            }

            Debug.Log($"[PhaseUI] Showing Prompt - Round {roundIndex}");

            uiContentController.SetContentType(ContentDisplayType.Prompt);
            uiContentController.SetRevealAnimation(RevealAnimationType.FadeIn);
            uiContentController.DisplayPreviousContent(roundIndex, chainIndex, useRevealAnimation: true);

            StartCoroutine(HideAfterDelay());
        }

        public void ShowDrawing(int roundIndex, int chainIndex = 0)
        {
            if (uiContentController == null)
            {
                Debug.LogWarning("[PhaseUIHelper] UIContentController not assigned!");
                return;
            }

            Debug.Log($"[PhaseUI] Showing Drawing - Round {roundIndex}");

            uiContentController.SetContentType(ContentDisplayType.Drawing);
            uiContentController.SetRevealAnimation(RevealAnimationType.SlideInUp);
            uiContentController.DisplayPreviousContent(roundIndex, chainIndex, useRevealAnimation: true);

            StartCoroutine(HideAfterDelay());
        }

        public void ShowBoth(int promptRound, int drawingRound, int chainIndex = 0)
        {
            if (uiContentController == null)
            {
                Debug.LogWarning("[PhaseUIHelper] UIContentController not assigned!");
                return;
            }

            Debug.Log($"[PhaseUI] Showing Both - Prompt(Round {promptRound}), Drawing(Round {drawingRound})");

            StartCoroutine(ShowBothSequence(promptRound, drawingRound, chainIndex));
        }

        public void Hide()
        {
            if (uiContentController == null) return;

            Debug.Log("[PhaseUI] Hiding content");
            uiContentController.HideContent();
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(displayDuration);
            Hide();
        }

        private IEnumerator ShowBothSequence(int promptRound, int drawingRound, int chainIndex)
        {
            // Show prompt
            uiContentController.SetContentType(ContentDisplayType.Prompt);
            uiContentController.SetRevealAnimation(RevealAnimationType.FadeIn);
            uiContentController.DisplayPreviousContent(promptRound, chainIndex, useRevealAnimation: true);

            yield return new WaitForSeconds(displayDuration);

            // Hide
            uiContentController.HideContent();

            yield return new WaitForSeconds(1f);

            // Show drawing
            uiContentController.SetContentType(ContentDisplayType.Drawing);
            uiContentController.SetRevealAnimation(RevealAnimationType.SlideInUp);
            uiContentController.DisplayPreviousContent(drawingRound, chainIndex, useRevealAnimation: true);

            yield return new WaitForSeconds(displayDuration);

            // Hide
            uiContentController.HideContent();
        }

        /// <summary>
        /// For testing only - simulate all phase content displays
        /// </summary>
        #if UNITY_EDITOR

        public void SimulateDrawPhase()
        {
            Debug.Log("[PhaseUI SIMULATION] Draw Phase - Showing previous Prompt");
            ShowPrompt(roundIndex: 0, chainIndex: 0);
        }

        public void SimulateGuessPhase()
        {
            Debug.Log("[PhaseUI SIMULATION] Guess Phase - Showing previous Drawing");
            ShowDrawing(roundIndex: 1, chainIndex: 0);
        }

        public void SimulateFinalGuessPhase()
        {
            Debug.Log("[PhaseUI SIMULATION] Final Guess Phase - Showing both");
            ShowBoth(promptRound: 0, drawingRound: 1, chainIndex: 0);
        }

        #endif
    }
}
