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

        public override int CalculateTotalRounds(int playerCount)
        {
            int pairs = Mathf.Max(1, playerCount / 2);
            return Mathf.Max(3, pairs + 1);
        }

        public override PhaseType GetPhaseForRound(int roundIndex, int totalRounds)
        {
            if (totalRounds <= 0) return PhaseType.None;
            if (roundIndex == 0) return PhaseType.Prompt;
            if (roundIndex == totalRounds - 1) return PhaseType.Guess;

            return PhaseType.Draw;
        }

        public override IReadOnlyList<PhaseAssignment> BuildAssignments(int roundIndex, IReadOnlyList<PlayerRef> orderedPlayers)
        {
            var n = orderedPlayers.Count;
            var list = new List<PhaseAssignment>();
            int pairs = Mathf.Max(1, n / 2);

            for (int p = 0; p < pairs; p++)
            {
                PlayerRef playerA = orderedPlayers[p * 2];
                PlayerRef playerB = (p * 2 + 1 < n) ? orderedPlayers[p * 2 + 1] : PlayerRef.None;

                int albumOriginSlot = (p + roundIndex) % pairs;

                list.Add(PhaseAssignment.Pair(playerA, playerB, (byte)albumOriginSlot, (byte)roundIndex));
                if (playerB.IsRealPlayer)
                {
                    list.Add(PhaseAssignment.Pair(playerB, playerA, (byte)albumOriginSlot, (byte)roundIndex));
                }
            }

            return list;
        }
    }
}