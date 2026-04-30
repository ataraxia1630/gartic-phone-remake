using System.Collections.Generic;
using Fusion;
using GarticPhone.Network.Phases;

namespace GarticPhone.Network.GameModes.Modes
{
    public class SandwichGameMode : GameModeBase
    {
        public override GameModeType Type => GameModeType.Sandwich;
        public override int PlayersPerSlot => 1;

        public override int CalculateTotalRounds(int playerCount) => playerCount;

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
