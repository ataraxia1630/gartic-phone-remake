using System.Collections.Generic;
using Fusion;
using InkEcho.Network.GameModes.Modes;
using InkEcho.Network.Phases;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

[TestFixture]
public class CoopGameModeTests
{
    private CoopGameMode _mode;
    private List<PlayerRef> _players;

    [SetUp]
    public void Setup()
    {
        _mode = new CoopGameMode();
        _players = new List<PlayerRef>();
        for (int i = 1; i <= 6; i++)
            _players.Add(PlayerRef.FromEncoded(i));
    }

    // ════════════════════════════════
    //  ROUNDS & PHASES
    // ════════════════════════════════
    [Test]
    public void TotalRounds_6Players_Is7()
    {
        // pairs = 3, totalRounds = 2 + (3-1)*2 + 1 = 7
        Assert.AreEqual(7, _mode.CalculateTotalRounds(6));
    }

    [Test]
    public void TotalRounds_4Players_Is5()
    {
        // pairs = 2, totalRounds = 2 + (2-1)*2 + 1 = 5
        Assert.AreEqual(5, _mode.CalculateTotalRounds(4));
    }

    [Test]
    public void TotalRounds_8Players_Is9()
    {
        // pairs = 4, totalRounds = 2 + (4-1)*2 + 1 = 9
        Assert.AreEqual(9, _mode.CalculateTotalRounds(8));
    }

    [Test]
    public void PhaseSequence_6Players_IsCorrect()
    {
        int total = _mode.CalculateTotalRounds(6); // 7

        Assert.AreEqual(PhaseType.Prompt, _mode.GetPhaseForRound(0, total));
        Assert.AreEqual(PhaseType.Draw, _mode.GetPhaseForRound(1, total));
        Assert.AreEqual(PhaseType.Observe, _mode.GetPhaseForRound(2, total));
        Assert.AreEqual(PhaseType.Draw, _mode.GetPhaseForRound(3, total));
        Assert.AreEqual(PhaseType.Observe, _mode.GetPhaseForRound(4, total));
        Assert.AreEqual(PhaseType.Draw, _mode.GetPhaseForRound(5, total));
        Assert.AreEqual(PhaseType.FinalGuess, _mode.GetPhaseForRound(6, total));
    }

    // ════════════════════════════════
    //  ASSIGNMENTS - PAIR PHASES
    // ════════════════════════════════
    [Test]
    public void Round0_Prompt_ReturnsPairAssignments()
    {
        var assignments = _mode.BuildAssignments(0, _players);

        // 3 cặp × 2 role = 6 assignments
        Assert.AreEqual(6, assignments.Count);

        // Đều là Pair (có SecondaryWorker)
        foreach (var a in assignments)
            Assert.AreNotEqual(PlayerRef.None, a.SecondaryWorker,
                "Prompt phase phải là Pair assignment");
    }

    [Test]
    public void Round0_Prompt_EachPairWritesOwnAlbum()
    {
        var assignments = _mode.BuildAssignments(0, _players);

        Assert.AreEqual(0, GetAssignment(assignments, _players[0]).AlbumOriginSlotIndex);
        Assert.AreEqual(0, GetAssignment(assignments, _players[1]).AlbumOriginSlotIndex);
        Assert.AreEqual(1, GetAssignment(assignments, _players[2]).AlbumOriginSlotIndex);
        Assert.AreEqual(1, GetAssignment(assignments, _players[3]).AlbumOriginSlotIndex);
        Assert.AreEqual(2, GetAssignment(assignments, _players[4]).AlbumOriginSlotIndex);
        Assert.AreEqual(2, GetAssignment(assignments, _players[5]).AlbumOriginSlotIndex);
    }

    [Test]
    public void Round1_Draw_EachPairDrawsTogether()
    {
        var assignments = _mode.BuildAssignments(1, _players);

        Assert.AreEqual(6, assignments.Count);

        // Cặp 0 → album 1
        Assert.AreEqual(1, GetAssignment(assignments, _players[0]).AlbumOriginSlotIndex);
        Assert.AreEqual(1, GetAssignment(assignments, _players[1]).AlbumOriginSlotIndex);

        // Cặp 1 → album 2
        Assert.AreEqual(2, GetAssignment(assignments, _players[2]).AlbumOriginSlotIndex);
        Assert.AreEqual(2, GetAssignment(assignments, _players[3]).AlbumOriginSlotIndex);

        // Cặp 2 → album 0
        Assert.AreEqual(0, GetAssignment(assignments, _players[4]).AlbumOriginSlotIndex);
        Assert.AreEqual(0, GetAssignment(assignments, _players[5]).AlbumOriginSlotIndex);
    }

    [Test]
    public void Roles_AreCorrect_WithinPair()
    {
        var assignments = _mode.BuildAssignments(0, _players);

        // PlayerA = role 0, PlayerB = role 1
        Assert.AreEqual(0, GetAssignment(assignments, _players[0]).PairRole);
        Assert.AreEqual(1, GetAssignment(assignments, _players[1]).PairRole);
        Assert.AreEqual(0, GetAssignment(assignments, _players[2]).PairRole);
        Assert.AreEqual(1, GetAssignment(assignments, _players[3]).PairRole);
    }

    // ════════════════════════════════
    //  ASSIGNMENTS - FINALGUESS
    // ════════════════════════════════
    [Test]
    public void FinalGuess_ReturnsSoloAssignments()
    {
        int total = _mode.CalculateTotalRounds(6); // 7
        var assignments = _mode.BuildAssignments(total - 1, _players);

        // 6 players → 6 solo assignments
        Assert.AreEqual(6, assignments.Count);

        // Solo: SecondaryWorker phải là None
        foreach (var a in assignments)
            Assert.AreEqual(PlayerRef.None, a.SecondaryWorker,
                "FinalGuess phải là Solo assignment");
    }

    [Test]
    public void FinalGuess_EachPlayerGuessesIndependently()
    {
        int total = _mode.CalculateTotalRounds(6);
        var assignments = _mode.BuildAssignments(total - 1, _players);

        // Mỗi player có assignment riêng
        foreach (var player in _players)
        {
            var a = GetAssignment(assignments, player);
            Assert.IsNotNull(a, $"Player {player} không có assignment ở FinalGuess");
        }
    }

    // ════════════════════════════════
    //  TÍNH ĐÚNG ĐẮN TỔNG THỂ
    // ════════════════════════════════
    [Test]
    public void NoAlbumHandledByTwoPairsInSameRound()
    {
        int total = _mode.CalculateTotalRounds(6);
        for (int round = 0; round < total - 1; round++) // bỏ qua FinalGuess
        {
            var assignments = _mode.BuildAssignments(round, _players);
            var albumsThisRound = new HashSet<int>();

            for (int p = 0; p < 3; p++)
            {
                int slot = GetAssignment(assignments, _players[p * 2]).AlbumOriginSlotIndex;
                Assert.IsFalse(albumsThisRound.Contains(slot),
                    $"Round {round}: album {slot} bị assign 2 cặp!");
                albumsThisRound.Add(slot);
            }
        }
    }

    [Test]
    public void ObserveRounds_AlwaysReturnPairAssignments()
    {
        int total = _mode.CalculateTotalRounds(6);
        for (int round = 0; round < total; round++)
        {
            var phase = _mode.GetPhaseForRound(round, total);
            if (phase != PhaseType.Observe) continue;

            var assignments = _mode.BuildAssignments(round, _players);
            foreach (var a in assignments)
                Assert.AreNotEqual(PlayerRef.None, a.SecondaryWorker,
                    $"Round {round} Observe phải là Pair");
        }
    }

    // ════════════════════════════════
    //  HELPER
    // ════════════════════════════════
    private PhaseAssignment GetAssignment(IReadOnlyList<PhaseAssignment> list, PlayerRef player)
    {
        foreach (var a in list)
            if (a.Worker == player) return a;
        throw new System.Exception($"Không tìm thấy assignment cho {player}");
    }
}