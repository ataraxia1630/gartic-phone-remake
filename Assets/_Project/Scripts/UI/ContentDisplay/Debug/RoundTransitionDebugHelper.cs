using UnityEngine;
using InkEcho.UI.ContentDisplay;

namespace InkEcho.UI
{
    /// <summary>
    /// Simple round transition test - Copy code này vào RoundManager của bạn để test
    /// Nhấn phím T để debug current round
    /// </summary>
    public class RoundTransitionDebugHelper : MonoBehaviour
    {
        [SerializeField] private UIContentController uiContentController;
        [SerializeField] private float displayDuration = 3f;

        private int currentRound = 0;
        private int maxRounds = 3;

        #if UNITY_EDITOR

        private void Update()
        {
            // Nhấn T để xem debug info
            if (Input.GetKeyDown(KeyCode.T))
                PrintDebugInfo();

            // Nhấn M để simulate một round
            if (Input.GetKeyDown(KeyCode.M))
                SimulateRoundTransition();

            // Nhấn B để simulate full game
            if (Input.GetKeyDown(KeyCode.B))
                SimulateFullGame();
        }

        private void PrintDebugInfo()
        {
            string info = string.Format(
                "Current Round: {0}\nMax Rounds: {1}\n" +
                "UIController: {2}\nPromptPanel: {3}\nDrawingPanel: {4}\n" +
                "Content Type: {5}\nAnimation: {6}\n" +
                "UI Round: {7}\nUI Chain: {8}",
                currentRound, maxRounds,
                uiContentController != null ? "✅" : "❌",
                uiContentController?.PromptPanel != null ? "✅" : "❌",
                uiContentController?.DrawingPanel != null ? "✅" : "❌",
                uiContentController?.ContentType,
                uiContentController?.RevealAnimationType,
                uiContentController?.CurrentRound,
                uiContentController?.CurrentChainIndex
            );

            Debug.Log("[DEBUG INFO]\n" + info);
        }

        private void SimulateRoundTransition()
        {
            Debug.Log($"[ROUND TEST] Simulating Round {currentRound}");

            if (currentRound == 0)
            {
                // Round 0: Show Prompt
                Debug.Log("[ROUND TEST] → Showing Prompt (Round 0)");
                uiContentController.SetContentType(ContentDisplayType.Prompt);
                uiContentController.SetRevealAnimation(RevealAnimationType.FadeIn);
                uiContentController.DisplayPreviousContent(0, 0, useRevealAnimation: true);
            }
            else if (currentRound == 1)
            {
                // Round 1: Show Drawing
                Debug.Log("[ROUND TEST] → Showing Drawing (Round 1)");
                uiContentController.SetContentType(ContentDisplayType.Drawing);
                uiContentController.SetRevealAnimation(RevealAnimationType.SlideInUp);
                uiContentController.DisplayPreviousContent(1, 0, useRevealAnimation: true);
            }
            else if (currentRound == 2)
            {
                // Round 2: Show Prompt again
                Debug.Log("[ROUND TEST] → Showing Prompt (Round 2)");
                uiContentController.SetContentType(ContentDisplayType.Prompt);
                uiContentController.SetRevealAnimation(RevealAnimationType.ScaleIn);
                uiContentController.DisplayPreviousContent(2, 0, useRevealAnimation: true);
            }

            currentRound = (currentRound + 1) % maxRounds;
        }

        private void SimulateFullGame()
        {
            Debug.Log("[FULL GAME TEST] Starting full game simulation...");
            StartCoroutine(SimulateGameLoop());
        }

        private System.Collections.IEnumerator SimulateGameLoop()
        {
            string header = @"
╔════════════════════════════════════════════════╗
║     SIMULATING FULL GAME LOOP                  ║
╚════════════════════════════════════════════════╝";

            Debug.Log(header);

            // Round 0: Prompt
            yield return SimulateRound(
                round: 0,
                contentType: ContentDisplayType.Prompt,
                animation: RevealAnimationType.FadeIn,
                description: "WRITING PHASE"
            );

            // Round 1: Drawing
            yield return SimulateRound(
                round: 1,
                contentType: ContentDisplayType.Drawing,
                animation: RevealAnimationType.SlideInUp,
                description: "DRAWING PHASE"
            );

            // Round 2: Guess
            yield return SimulateRound(
                round: 2,
                contentType: ContentDisplayType.Prompt,
                animation: RevealAnimationType.ScaleIn,
                description: "GUESSING PHASE"
            );

            Debug.Log("\n✅ FULL GAME SIMULATION COMPLETE!\n");
        }

        private System.Collections.IEnumerator SimulateRound(
            int round, 
            ContentDisplayType contentType,
            RevealAnimationType animation,
            string description)
        {
            Debug.Log($"\n[ROUND {round}] {description}");
            Debug.Log($"[ROUND {round}] Showing content...");

            // Show content
            uiContentController.SetContentType(contentType);
            uiContentController.SetRevealAnimation(animation);
            uiContentController.DisplayPreviousContent(round, 0, useRevealAnimation: true);

            // Wait for animation + display time
            yield return new WaitForSeconds(uiContentController.RevealDuration);
            yield return new WaitForSeconds(displayDuration);

            // Hide content
            Debug.Log($"[ROUND {round}] Hiding content...");
            uiContentController.HideContent();

            yield return new WaitForSeconds(1f);
        }

        #endif

        /// <summary>
        /// Call này từ RoundManager khi chuyển round
        /// </summary>
        public void OnRoundChanged(int newRound, ContentDisplayType contentType)
        {
            if (uiContentController == null) return;

            Debug.Log($"[TRANSITION] Round changed to: {newRound}, Type: {contentType}");

            uiContentController.SetContentType(contentType);
            uiContentController.DisplayPreviousContent(newRound, 0, useRevealAnimation: true);
        }
    }
}
