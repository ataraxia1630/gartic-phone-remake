using UnityEngine;
using InkEcho.Network.Phases;
using InkEcho.Network.Core;
using InkEcho.Network.Data;
using InkEcho.Network.Players;
using Fusion;

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
                Debug.Log($"[LocalSubmit] Auto-trigger from Update: phase={phaseManager.CurrentPhase}, timeLeft={timeLeft}");

                ForceSubmitCurrentWork();
            }
        }
    }

    public void ForceSubmitCurrentWork()
    {
        Debug.Log($"[LocalSubmit] ForceSubmitCurrentWork called: _hasSubmittedThisPhase={_hasSubmittedThisPhase}");

        if (_hasSubmittedThisPhase) return;
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
                    // TODO: Đóng gói hình ảnh
                    ulong imageHash = 12345UL; // Demo
                    ushort strokes = 10;       // Demo
                    albumStore.Rpc_SubmitDrawing(assignment.AlbumOriginSlotIndex, imageHash, strokes);
                    break;

                case PhaseType.Guess:
                    // TODO: Lấy string từ Input Field
                    string guessText = "Time's up guess!";
                    //albumStore.Rpc_SubmitGuess(assignment.AlbumOriginSlotIndex, guessText);
                    break;
            }
            Debug.Log($"[LocalSubmit] Ép nộp bài thành công cho phase {phaseManager.CurrentPhase}");
        }
    }
}