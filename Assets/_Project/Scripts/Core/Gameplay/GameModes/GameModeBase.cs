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

        public virtual PhaseType GetPhaseForRound(int roundIndex, int totalRounds)
        {
            if (totalRounds <= 0) return PhaseType.None;
            if (roundIndex == 0) return PhaseType.Prompt;
            if (roundIndex >= totalRounds) return PhaseType.Reveal;
            return roundIndex % 2 == 1 ? PhaseType.Draw : PhaseType.Guess;
        }

        public abstract IReadOnlyList<PhaseAssignment> BuildAssignments(int roundIndex, IReadOnlyList<PlayerRef> orderedPlayers);
    }
}
