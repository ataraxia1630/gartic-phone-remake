using System.Collections.Generic;
using UnityEngine;
using Fusion;
using InkEcho.Network.Phases;

namespace InkEcho.Network.GameModes.Modes
{
    public class CoopGameMode : GameModeBase
    {
        public override GameModeType Type => GameModeType.Coop;
        public override int PlayersPerSlot => 2;

        //public override int CalculateTotalRounds(int playerCount)
        //{
        //    int pairs = Mathf.Max(1, playerCount / 2);
        //    return Mathf.Max(3, pairs + 1);
        //}

        public override int CalculateTotalRounds(int playerCount)
        {
            int pairs = Mathf.Max(1, playerCount / 2);
            // Giống Sandwich: Prompt(1) + Draw(1) + (Observe+Draw)*(pairs-1) + FinalGuess(1)
            return 2 + (pairs - 1) * 2 + 1;
        }

        public override PhaseType GetPhaseForRound(int roundIndex, int totalRounds)
        {
            if (totalRounds <= 0) return PhaseType.None;

            if (roundIndex == 0) return PhaseType.Prompt;
            if (roundIndex == 1) return PhaseType.Draw;
            if (roundIndex == totalRounds - 1) return PhaseType.FinalGuess;

            // Round 2 trở đi: Observe → Draw → Observe → Draw...
            int middleIndex = roundIndex - 2;
            return middleIndex % 2 == 0 ? PhaseType.Observe : PhaseType.Draw;
        }

        public override IReadOnlyList<PhaseAssignment> BuildAssignments(
            int roundIndex,
            IReadOnlyList<PlayerRef> orderedPlayers)
        {
            var n = orderedPlayers.Count;
            var list = new List<PhaseAssignment>();
            int pairs = Mathf.Max(1, n / 2);
            int totalRounds = CalculateTotalRounds(n);
            var phase = GetPhaseForRound(roundIndex, totalRounds);

            for (int p = 0; p < pairs; p++)
            {
                PlayerRef playerA = orderedPlayers[p * 2];
                PlayerRef playerB = (p * 2 + 1 < n)
                    ? orderedPlayers[p * 2 + 1]
                    : PlayerRef.None;

                int albumOriginSlot = (p + roundIndex) % pairs;

                if (phase == PhaseType.FinalGuess)
                {
                    // Tách ra: mỗi player đoán riêng → dùng Solo thay vì Pair
                    list.Add(PhaseAssignment.Solo(playerA, (byte)albumOriginSlot, (byte)roundIndex));
                    if (playerB.IsRealPlayer)
                        list.Add(PhaseAssignment.Solo(playerB, (byte)albumOriginSlot, (byte)roundIndex));
                }
                else
                {
                    // Prompt, Draw, Observe → 2 người làm cùng nhau theo cặp
                    list.Add(PhaseAssignment.Pair(playerA, playerB, (byte)albumOriginSlot, (byte)roundIndex, 0));
                    if (playerB.IsRealPlayer)
                        list.Add(PhaseAssignment.Pair(playerB, playerA, (byte)albumOriginSlot, (byte)roundIndex, 1));
                }
            }

            return list;
        }
    }
}