using System;
using System.Collections.Generic;
using Fusion;
using InkEcho.Network.Phases;
using UnityEngine;

namespace InkEcho.Network.GameModes.Modes
{
    public class SandwichGameMode : GameModeBase
    {
        public override GameModeType Type => GameModeType.Sandwich;
        public override int PlayersPerSlot => 1;


        public override int CalculateTotalRounds(int playerCount)
        {
            // Prompt(1) + Draw(1) + (Observe+Draw) * max(1, playerCount-2) + FinalGuess(1)
            // n=2: 5 (vẫn ép 1 cặp Observe+Draw dù chỉ 2 người, 2 người sẽ vẽ vòng lại cho nhau), n=3: 5, n=4: 7
            return 2 + Mathf.Max(1, playerCount - 2) * 2 + 1;
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
