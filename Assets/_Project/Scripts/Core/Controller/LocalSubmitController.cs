using UnityEngine;
using InkEcho.Network.Phases;
using InkEcho.Network.Core;
using InkEcho.Network.Data;

public class LocalSubmitController : MonoBehaviour
{
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

    public void ForceSubmitCurrentWork()
    {
        if (_hasSubmittedThisPhase) return;
        _hasSubmittedThisPhase = true;

        var phaseManager = ServiceLocator.Get<PhaseManager>();
        var albumStore = ServiceLocator.Get<AlbumStore>();
        if (phaseManager == null || albumStore == null) return;

        if (phaseManager.TryGetAssignment(phaseManager.Runner.LocalPlayer, out var assignment))
        {
            switch (phaseManager.CurrentPhase)
            {
                case PhaseType.Prompt:
                    // TODO: Lấy string từ Input Field
                    string promptText = "Time's up prompt!";
                    albumStore.Rpc_SubmitPrompt(assignment.AlbumOriginSlotIndex, promptText);
                    break;

                case PhaseType.Draw:
                    // TODO: Đóng gói hình ảnh
                    ulong imageHash = 12345UL; // Demo
                    ushort strokes = 10;       // Demo
                    albumStore.Rpc_SubmitDrawing(assignment.AlbumOriginSlotIndex, imageHash, strokes);
                    break;

                case PhaseType.Guess:
                    // TODO: Lấy string từ Input Field
                    string guessText = "Time's up guess!";
                    albumStore.Rpc_SubmitGuess(assignment.AlbumOriginSlotIndex, guessText);
                    break;
            }
            Debug.Log($"[LocalSubmit] Ép nộp bài thành công cho phase {phaseManager.CurrentPhase}");
        }
    }
}