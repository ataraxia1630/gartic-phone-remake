using System.Collections.Generic;
using Fusion;
using InkEcho.Network.Phases;

namespace InkEcho.Network.GameModes.Modes
{
    public class SandwichGameMode : GameModeBase
    {
        public override GameModeType Type => GameModeType.Sandwich;
        public override int PlayersPerSlot => 1;

        public override int CalculateTotalRounds(int playerCount)
        {
            // Prompt(1) + Draw(1) + (Observe+Draw) * (playerCount-1) + FinalGuess(1)
            // 4 người: 1 + 1 + 2*3 + 1 = 9 rounds
            return 2 + (playerCount - 1) * 2 + 1;
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

        public override IReadOnlyList<PhaseAssignment> BuildAssignments(int roundIndex, IReadOnlyList<PlayerRef> orderedPlayers)
        {
            var n = orderedPlayers.Count;
            var list = new List<PhaseAssignment>(n);

            for (int origin = 0; origin < n; origin++)
            {
                int workerIndex = (origin + roundIndex) % n;
                list.Add(PhaseAssignment.Solo(orderedPlayers[workerIndex], (byte)origin, (byte)roundIndex));
            }

            return list;
        }
    }
}