using System.Collections.Generic;
using Fusion;
using GarticPhone.Network.Phases;

namespace GarticPhone.Network.GameModes
{
    public interface IGameMode
    {
        GameModeType Type { get; }
        int PlayersPerSlot { get; }

        int CalculateTotalRounds(int playerCount);

        PhaseType GetPhaseForRound(int roundIndex, int totalRounds);

        IReadOnlyList<PhaseAssignment> BuildAssignments(int roundIndex, IReadOnlyList<PlayerRef> orderedPlayers);
    }
}
