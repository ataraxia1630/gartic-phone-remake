using System;
using System.Collections.Generic;
using Fusion;
using InkEcho.Network.Phases;

namespace InkEcho.Network.GameModes.Modes
{
    public class SandwichGameMode : GameModeBase
    {
        public override GameModeType Type => GameModeType.Sandwich;
        public override int PlayersPerSlot => 1;

        // Network rounds: 1 Prompt + (N-1) Draws + (N-2) Observes + 1 FinalGuess = 2N - 1.
        // First draw has no preceding Observe because workers read the text prompt directly.
        public override int CalculateTotalRounds(int playerCount)
        {
            if (playerCount < 2) return 0;
            return 2 * playerCount - 1;
        }

        public override PhaseType GetPhaseForRound(int roundIndex, int totalRounds)
        {
            if (totalRounds <= 0) return PhaseType.None;
            if (roundIndex == 0) return PhaseType.Prompt;
            if (roundIndex >= totalRounds) return PhaseType.Reveal;
            if (roundIndex == totalRounds - 1) return PhaseType.FinalGuess;
            return roundIndex % 2 == 1 ? PhaseType.Draw : PhaseType.Observe;
        }

        public override IReadOnlyList<PhaseAssignment> BuildAssignments(int roundIndex, IReadOnlyList<PlayerRef> orderedPlayers)
        {
            var n = orderedPlayers.Count;
            var list = new List<PhaseAssignment>(n);
            if (n < 2) return list;

            int totalRounds = CalculateTotalRounds(n);
            var phase = GetPhaseForRound(roundIndex, totalRounds);

            int chainLink;
            switch (phase)
            {
                case PhaseType.Prompt: chainLink = 0; break;
                case PhaseType.Draw: chainLink = (roundIndex + 1) / 2; break;
                case PhaseType.Observe: chainLink = roundIndex / 2 + 1; break;
                case PhaseType.FinalGuess: chainLink = n; break;
                default: return list;
            }

            int guesserOffset = Math.Max(1, n / 2);

            for (int origin = 0; origin < n; origin++)
            {
                int workerIndex;
                switch (phase)
                {
                    case PhaseType.Prompt:
                        workerIndex = origin;
                        break;
                    case PhaseType.Draw:
                    case PhaseType.Observe:
                        workerIndex = (origin + chainLink) % n;
                        break;
                    case PhaseType.FinalGuess:
                        workerIndex = (origin + guesserOffset) % n;
                        break;
                    default:
                        return list;
                }

                list.Add(PhaseAssignment.Solo(
                    worker: orderedPlayers[workerIndex],
                    originSlot: (byte)origin,
                    chainLink: (byte)chainLink));
            }

            return list;
        }
    }
}
