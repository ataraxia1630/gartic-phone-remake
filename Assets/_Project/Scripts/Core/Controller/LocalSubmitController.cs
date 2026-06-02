using UnityEngine;
using InkEcho.Network.Phases;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using TMPro;

public class LocalSubmitController : MonoBehaviour
{
    [Header("Phase Input Bindings")]
    [Tooltip("InputField cho Prompt phase (kéo từ UI_Prompt nếu cùng scene, hoặc null nếu auto-find).")]
    [SerializeField] private TMP_InputField promptInput;
    [Tooltip("InputField cho FinalGuess phase.")]
    [SerializeField] private TMP_InputField finalGuessInput;
    [Tooltip("DrawingManager đang chạy trong UI_Draw (kéo từ scene khi đã load, hoặc null để auto-find).")]
    [SerializeField] private DrawingManager drawingManager;

    private PhaseType _lastObservedPhase = PhaseType.None;
    private bool _hasSubmittedThisPhase = false;

    void Update()
    {
        var phaseManager = ServiceLocator.Get<PhaseManager>();
        if (phaseManager == null) return;

        if (phaseManager.CurrentPhase != _lastObservedPhase)
        {
            _lastObservedPhase = phaseManager.CurrentPhase;
            _hasSubmittedThisPhase = false;
        }

        if (phaseManager.PhaseTimer.IsRunning)
        {
            float timeLeft = phaseManager.PhaseTimer.RemainingTime(phaseManager.Runner).GetValueOrDefault();
            if (timeLeft <= 0.1f && !_hasSubmittedThisPhase)
            {
                ForceSubmitCurrentWork();
            }
        }
    }

    // Có thể bind nút Submit trong UI gọi vào đây (chạy ngay không đợi timeout).
    public void SubmitNow() => ForceSubmitCurrentWork();

    public void ForceSubmitCurrentWork()
    {
        if (_hasSubmittedThisPhase) return;
        _hasSubmittedThisPhase = true;

        var phaseManager = ServiceLocator.Get<PhaseManager>();
        var albumStore = ServiceLocator.Get<AlbumStore>();
        if (phaseManager == null || albumStore == null) return;

        if (!phaseManager.TryGetAssignment(phaseManager.Runner.LocalPlayer, out var assignment))
        {
            Debug.Log($"[LocalSubmit] Local player không có assignment trong phase {phaseManager.CurrentPhase}, skip.");
            return;
        }

        switch (phaseManager.CurrentPhase)
        {
            case PhaseType.Prompt:
                string promptText = promptInput != null && !string.IsNullOrWhiteSpace(promptInput.text)
                    ? promptInput.text.Trim() : "(empty)";
                albumStore.Rpc_SubmitPrompt(assignment.AlbumOriginSlotIndex, promptText, assignment.PairRole);
                break;

            case PhaseType.FinalGuess:
                string guessText = finalGuessInput != null && !string.IsNullOrWhiteSpace(finalGuessInput.text)
                    ? finalGuessInput.text.Trim() : "(empty)";
                albumStore.Rpc_SubmitFinalGuess(assignment.AlbumOriginSlotIndex, guessText, assignment.PairRole);
                break;

            case PhaseType.Draw:
                // Set WorkerPlayer cho entry (= người vẽ). Ảnh PNG được broadcast riêng qua DrawingManager
                // nên hash/strokes để 0 — reveal chỉ dùng WorkerPlayer (tên tác giả) + texture.
                albumStore.Rpc_SubmitDrawing(assignment.AlbumOriginSlotIndex, 0UL, 0);
                break;
        }
        Debug.Log($"[LocalSubmit] Đã nộp bài cho phase {phaseManager.CurrentPhase} (chain {assignment.AlbumOriginSlotIndex}, link {assignment.ChainLinkIndex})");
    }

}
