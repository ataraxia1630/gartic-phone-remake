using UnityEngine;
using InkEcho.Network.Phases;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Players;
using Fusion;
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
                Debug.Log($"[LocalSubmit] Auto-trigger from Update: phase={phaseManager.CurrentPhase}, timeLeft={timeLeft}");

                DoSubmit(isAutoTimeout: true);
            }
        }
    }

    // Có thể bind nút Submit trong UI gọi vào đây (chạy ngay không đợi timeout).
    public void SubmitNow() => DoSubmit(isAutoTimeout: false);

    public void ForceSubmitCurrentWork() => DoSubmit(isAutoTimeout: false);

    private void DoSubmit(bool isAutoTimeout)
    {
        Debug.Log($"[LocalSubmit] ForceSubmitCurrentWork called: _hasSubmittedThisPhase={_hasSubmittedThisPhase}, isAutoTimeout={isAutoTimeout}");

        if (isAutoTimeout && _hasSubmittedThisPhase) return;
        _hasSubmittedThisPhase = true;

        var phaseManager = ServiceLocator.Get<PhaseManager>();
        var albumStore = ServiceLocator.Get<AlbumStore>();
        Debug.Log($"[LocalSubmit] phaseManager={(phaseManager == null ? "NULL" : "OK")}, albumStore={(albumStore == null ? "NULL" : "OK")}");

        if (phaseManager == null || albumStore == null) return;

        var localPlayer = phaseManager.Runner.LocalPlayer;
        bool gotAssignment = phaseManager.TryGetAssignment(localPlayer, out var assignment);
        Debug.Log($"[LocalSubmit] LocalPlayer={localPlayer}, CurrentPhase={phaseManager.CurrentPhase}, TryGetAssignment={gotAssignment}" +
                  (gotAssignment ? $", Worker={assignment.Worker}, AlbumOriginSlotIndex={assignment.AlbumOriginSlotIndex}, PairRole={assignment.PairRole}, ChainLinkIndex={assignment.ChainLinkIndex}" : ""));

        if (gotAssignment)
        {
            switch (phaseManager.CurrentPhase)
            {
                case PhaseType.Prompt:
                    if (ServiceLocator.TryGet<PromptInputController>(out var promptInput))
                    {
                        promptInput.ForceSubmit("Time's up prompt!");
                    }
                    else
                    {
                        albumStore.Rpc_SubmitPrompt(assignment.AlbumOriginSlotIndex, new NetworkString<_64>("Time's up prompt!"), assignment.PairRole);
                    }
                    break;

                case PhaseType.Draw:
                    albumStore.Rpc_SubmitDrawing(assignment.AlbumOriginSlotIndex, 0UL, 1);
                    break;

                case PhaseType.FinalGuess:
                    string guessText = finalGuessInput != null && !string.IsNullOrWhiteSpace(finalGuessInput.text)
                        ? finalGuessInput.text.Trim() : "(empty)";
                    albumStore.Rpc_SubmitFinalGuess(assignment.AlbumOriginSlotIndex, new NetworkString<_16>(guessText), assignment.PairRole);
                    break;
            }
            Debug.Log($"[LocalSubmit] Đã nộp bài cho phase {phaseManager.CurrentPhase} (chain {assignment.AlbumOriginSlotIndex}, link {assignment.ChainLinkIndex})");
        }
        Debug.Log($"[LocalSubmit] Đã nộp bài: phase={phaseManager.CurrentPhase}, originSlot={assignment.AlbumOriginSlotIndex}, link={assignment.ChainLinkIndex}");
    }

}
