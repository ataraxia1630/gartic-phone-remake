using System.Collections.Generic;
using Fusion;
using InkEcho.Network.Phases;

namespace InkEcho.Network.GameModes
{
    public abstract class GameModeBase : IGameMode
    {
        public abstract GameModeType Type { get; }
        public abstract int PlayersPerSlot { get; }

        public abstract int CalculateTotalRounds(int playerCount);

        public abstract PhaseType GetPhaseForRound(int roundIndex, int totalRounds);

        public abstract IReadOnlyList<PhaseAssignment> BuildAssignments(int roundIndex, IReadOnlyList<PlayerRef> orderedPlayers);
    }
}