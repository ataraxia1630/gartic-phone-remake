using System.Collections.Generic;
using Fusion;
using GarticPhone.Network.Phases;

namespace GarticPhone.Network.GameModes.Modes
{
    public class CoopGameMode : GameModeBase
    {
        public override GameModeType Type => GameModeType.Coop;
        public override int PlayersPerSlot => 2;

        public override int CalculateTotalRounds(int playerCount)
        {
            // Coop not implemented in v1 — fallback to Sandwich behaviour
            return playerCount;
        }

        public override IReadOnlyList<PhaseAssignment> BuildAssignments(int roundIndex, IReadOnlyList<PlayerRef> orderedPlayers)
        {
            // Fallback: build solo assignments like Sandwich so mode picker doesn't break
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
