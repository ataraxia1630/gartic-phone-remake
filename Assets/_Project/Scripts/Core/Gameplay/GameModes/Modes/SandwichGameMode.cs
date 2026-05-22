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

        // Chain variant: Prompt + (Draw, Observe) * (N-1) + FinalGuess
        // step 0          : Prompt        (chainLink 0)
        // step 2k-1       : Draw at chainLink k         k = 1..N-1
        // step 2k         : Observe targeting link k    k = 1..N-2 (panel hiển thị link k = ChainLinkIndex-1)
        // step 2N-2       : FinalGuess    (chainLink N)
        public override int CalculateTotalRounds(int playerCount)
        {
            if (playerCount <= 1) return 1;
            return 2 * playerCount - 1;
        }

        public override PhaseType GetPhaseForRound(int step, int totalSteps)
        {
            if (totalSteps <= 0) return PhaseType.None;
            if (step <= 0) return PhaseType.Prompt;
            if (step >= totalSteps - 1) return PhaseType.FinalGuess;
            return (step & 1) == 1 ? PhaseType.Draw : PhaseType.Observe;
        }

        public override IReadOnlyList<PhaseAssignment> BuildAssignments(int step, IReadOnlyList<PlayerRef> orderedPlayers)
        {
            var n = orderedPlayers.Count;
            var list = new List<PhaseAssignment>(n);
            if (n <= 0) return list;

            var totalSteps = CalculateTotalRounds(n);
            var phase = GetPhaseForRound(step, totalSteps);

            switch (phase)
            {
                case PhaseType.Prompt:
                    for (int origin = 0; origin < n; origin++)
                    {
                        list.Add(PhaseAssignment.Solo(orderedPlayers[origin], (byte)origin, 0));
                    }
                    break;

                case PhaseType.Draw:
                {
                    int chainLink = (step + 1) / 2;
                    for (int origin = 0; origin < n; origin++)
                    {
                        int workerIdx = (origin + chainLink) % n;
                        list.Add(PhaseAssignment.Solo(orderedPlayers[workerIdx], (byte)origin, (byte)chainLink));
                    }
                    break;
                }

                case PhaseType.Observe:
                {
                    // Người chuẩn bị vẽ link kế tiếp xem link vừa được vẽ.
                    int nextDrawLink = step / 2 + 1;
                    for (int origin = 0; origin < n; origin++)
                    {
                        int workerIdx = (origin + nextDrawLink) % n;
                        list.Add(PhaseAssignment.Solo(orderedPlayers[workerIdx], (byte)origin, (byte)nextDrawLink));
                    }
                    break;
                }

                case PhaseType.FinalGuess:
                {
                    // 1 guesser / chain, khác origin. Dùng shift k random nhưng deterministic
                    // (mọi client cùng PlayOrderSlotIndices sẽ ra cùng k). Shift k ∈ [1, n-1].
                    int shift = ComputeFinalGuessShift(orderedPlayers, n);
                    // FinalGuessPanel hiển thị (ChainLinkIndex - 1, slot) → ChainLinkIndex = n để show link cuối cùng (n-1).
                    byte chainLink = (byte)n;
                    for (int origin = 0; origin < n; origin++)
                    {
                        int workerIdx = (origin + shift) % n;
                        list.Add(PhaseAssignment.Solo(orderedPlayers[workerIdx], (byte)origin, chainLink));
                    }
                    break;
                }
            }

            return list;
        }

        // PlayerId được Fusion gán cố định và đồng bộ giữa các client → đủ deterministic.
        private static int ComputeFinalGuessShift(IReadOnlyList<PlayerRef> orderedPlayers, int n)
        {
            if (n <= 1) return 0;
            int seed = 17;
            for (int i = 0; i < n; i++)
            {
                seed = unchecked(seed * 31 + orderedPlayers[i].PlayerId);
            }
            int mod = n - 1;
            int shift = Math.Abs(seed) % mod;
            return shift + 1;
        }
    }
}
