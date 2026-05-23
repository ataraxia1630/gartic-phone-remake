using UnityEngine;
using System.Collections.Generic;
using Fusion;
using InkEcho.Network.GameModes.Modes;
using InkEcho.Network.Phases;

public class DebugTestRunner : MonoBehaviour
{
    void Start()
    {
        var mode = new CoopGameMode();
        int total = mode.CalculateTotalRounds(6);

        // Tạo 6 fake player
        var players = new List<PlayerRef>();
        for (int i = 1; i <= 6; i++)
            players.Add(PlayerRef.FromEncoded(i));

        Debug.Log("====== PHASE SEQUENCE ======");
        TestPhaseSequence(mode, total);

        Debug.Log("====== ROUND 0: PROMPT ======");
        TestRound(mode, 0, players, expectedAlbums: new[] { 0, 0, 1, 1, 2, 2 });

        Debug.Log("====== ROUND 1: DRAW ======");
        TestRound(mode, 1, players, expectedAlbums: new[] { 1, 1, 2, 2, 0, 0 });

        Debug.Log("====== ROUND 2: OBSERVE ======");
        TestRound(mode, 2, players, expectedAlbums: new[] { 2, 2, 0, 0, 1, 1 });

        Debug.Log("====== ROUND 6: FINALGUESS ======");
        TestFinalGuess(mode, total - 1, players);
    }

    // ── Phase Sequence ──
    void TestPhaseSequence(CoopGameMode mode, int total)
    {
        Check(mode.GetPhaseForRound(0, total), PhaseType.Prompt, "Round 0");
        Check(mode.GetPhaseForRound(1, total), PhaseType.Draw, "Round 1");
        Check(mode.GetPhaseForRound(2, total), PhaseType.Observe, "Round 2");
        Check(mode.GetPhaseForRound(3, total), PhaseType.Draw, "Round 3");
        Check(mode.GetPhaseForRound(4, total), PhaseType.Observe, "Round 4");
        Check(mode.GetPhaseForRound(5, total), PhaseType.Draw, "Round 5");
        Check(mode.GetPhaseForRound(6, total), PhaseType.FinalGuess, "Round 6");
    }

    // ── Kiểm tra album assignment từng player ──
    void TestRound(CoopGameMode mode, int roundIndex, List<PlayerRef> players, int[] expectedAlbums)
    {
        var assignments = mode.BuildAssignments(roundIndex, players);

        for (int i = 0; i < players.Count; i++)
        {
            var assignment = GetAssignment(assignments, players[i]);
            if (assignment == null)
            {
                Debug.LogError($"❌ Player {i}: Không có assignment!");
                continue;
            }

            int actual = assignment.Value.AlbumOriginSlotIndex; // .Value vì là nullable
            int expected = expectedAlbums[i];

            if (actual == expected)
                Debug.Log($"✅ Player {i} (Pair {i / 2}): album {actual}");
            else
                Debug.LogError($"❌ Player {i} (Pair {i / 2}): Expected album {expected}, Got {actual}");
        }
    }

    // ── Kiểm tra FinalGuess là Solo (không có SecondaryWorker) ──
    void TestFinalGuess(CoopGameMode mode, int roundIndex, List<PlayerRef> players)
    {
        var assignments = mode.BuildAssignments(roundIndex, players);

        foreach (var a in assignments)
        {
            bool isSolo = a.SecondaryWorker == PlayerRef.None;
            if (isSolo)
                Debug.Log($"✅ Player {a.Worker}: Solo assignment ✓");
            else
                Debug.LogError($"❌ Player {a.Worker}: Vẫn còn Pair assignment ở FinalGuess!");
        }
    }

    // ── Helpers ──
    PhaseAssignment? GetAssignment(IReadOnlyList<PhaseAssignment> list, PlayerRef player)
    {
        foreach (var a in list)
            if (a.Worker == player) return a;
        //return null
        return null;
    }

    void Check(PhaseType actual, PhaseType expected, string label)
    {
        if (actual == expected)
            Debug.Log($"✅ {label}: {actual}");
        else
            Debug.LogError($"❌ {label}: Expected {expected}, Got {actual}");
    }
}