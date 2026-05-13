using System;
using System.Collections.Generic;
using Fusion;
using InkEcho.Network.Core;
using InkEcho.Network.GameModes;
using InkEcho.Network.Players;
using InkEcho.Network.Data;

namespace InkEcho.Network.Phases
{
    public class PhaseManager : NetworkBehaviour
    {
        [Networked] public PhaseType CurrentPhase { get; set; }
        [Networked] public TickTimer PhaseTimer { get; set; }
        [Networked] public byte RoundIndex { get; set; }
        [Networked] public byte TotalRounds { get; set; }
        [Networked] public byte RevealAlbumIndex { get; set; }
        [Networked] public GameModeType ActiveMode { get; set; }
        [Networked] public NetworkBool IsGameFinished { get; set; }
        [Networked, Capacity(PlayerRegistry.MaxPlayers)]
        public NetworkArray<byte> PlayOrderSlotIndices => default;
        [Networked] public byte PlayOrderCount { get; set; }
        private IPhaseStrategy _currentStrategy;
        private IGameMode _mode;
        private IReadOnlyList<PhaseAssignment> _assignments;
        private GameModeConfig _modeConfigCache;
        private GameModeType _modeConfigCachedFor;
        private PhaseType _cachedAssignmentPhase = PhaseType.None;
        private byte _cachedAssignmentRound = byte.MaxValue;
        private GameModeType _cachedAssignmentMode;
        private byte _cachedAssignmentOrderCount;

        public override void Spawned()
        {
            ServiceLocator.Register<PhaseManager>(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ServiceLocator.Unregister<PhaseManager>(this);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;
            _currentStrategy?.Tick(this);
        }

        public void StartGame(GameModeType modeType)
        {
            if (!HasStateAuthority) return;
            var registry = ServiceLocator.Get<PlayerRegistry>();
            if (registry == null) return;

            var ordered = registry.GetOrderedPlayers();
            var playerCount = (byte)ordered.Count;

            ActiveMode = modeType;
            _mode = GameModeFactory.Create(modeType);
            TotalRounds = (byte)_mode.CalculateTotalRounds(playerCount);
            RoundIndex = 0;
            RevealAlbumIndex = 0;
            IsGameFinished = false;

            // initialize album store
            var album = ServiceLocator.Get<Data.AlbumStore>();
            album?.Init(playerCount);

            CurrentPhase = PhaseType.Prompt;
            InstallStrategyFor(CurrentPhase);
            ApplyRandomPlayOrder(ordered);
            RefreshAssignments();
        }

        public void ResetForLobby()
        {
            if (!HasStateAuthority) return;
            CurrentPhase = PhaseType.None;
            PhaseTimer = TickTimer.None;
            RoundIndex = 0;
            TotalRounds = 0;
            RevealAlbumIndex = 0;
            IsGameFinished = false;
            PlayOrderCount = 0;
            _currentStrategy = null;
            _mode = null;
            _assignments = null;
            _cachedAssignmentPhase = PhaseType.None;
            _cachedAssignmentRound = byte.MaxValue;
            _cachedAssignmentMode = default;
            _cachedAssignmentOrderCount = 0;
        }

        public GameModeConfig GetActiveModeConfig()
        {
            if (_modeConfigCache != null && _modeConfigCachedFor == ActiveMode) return _modeConfigCache;
            var bootstrap = ServiceLocator.Get<NetworkBootstrap>();
            var network = bootstrap != null ? bootstrap.Config : null;
            _modeConfigCache = network != null ? network.GetModeConfig(ActiveMode) : null;
            _modeConfigCachedFor = ActiveMode;
            return _modeConfigCache;
        }

        public float ResolveDuration(PhaseType phase)
        {
            var bootstrap = ServiceLocator.Get<NetworkBootstrap>();
            var network = bootstrap != null ? bootstrap.Config : null;
            if (network == null) return 0f;
            var modeCfg = GetActiveModeConfig();
            switch (phase)
            {
                case PhaseType.Prompt:
                    return modeCfg != null ? modeCfg.ResolvePromptDuration(network.PromptPhaseDuration) : network.PromptPhaseDuration;
                case PhaseType.Draw:
                    return modeCfg != null ? modeCfg.ResolveDrawDuration(network.DrawPhaseDuration) : network.DrawPhaseDuration;
                case PhaseType.Guess:
                    return modeCfg != null ? modeCfg.ResolveGuessDuration(network.GuessPhaseDuration) : network.GuessPhaseDuration;
                case PhaseType.Observe:
                    return modeCfg != null ? modeCfg.ResolveObserveDuration(network.ObservePhaseDuration) : network.ObservePhaseDuration;
                case PhaseType.FinalGuess:
                    return modeCfg != null ? modeCfg.ResolveFinalGuessDuration(network.FinalGuessPhaseDuration) : network.FinalGuessPhaseDuration;
                case PhaseType.Reveal:
                    return modeCfg != null ? modeCfg.ResolveRevealDuration(network.RevealPerAlbumDuration) : network.RevealPerAlbumDuration;
                default: return 0f;
            }
        }

        public void SetRevealAlbumIndex(byte index)
        {
            if (!HasStateAuthority) return;
            RevealAlbumIndex = index;
        }

        public void AdvancePhase()
        {
            if (!HasStateAuthority) return;

            if (_mode == null)
            {
                IsGameFinished = true;
                return;
            }

            if (CurrentPhase == PhaseType.Reveal)
            {
                IsGameFinished = true;
                CurrentPhase = PhaseType.None;
                PhaseTimer = TickTimer.None;
                _currentStrategy = null;
                return;
            }

            var nextRound = (byte)(RoundIndex + 1);
            var nextPhase = _mode.GetPhaseForRound(nextRound, TotalRounds);
            RoundIndex = nextRound;
            CurrentPhase = nextPhase;

            if (nextPhase != PhaseType.None && nextPhase != PhaseType.Reveal)
            {
                RefreshAssignments();
            }

            InstallStrategyFor(CurrentPhase);
        }

        private void InstallStrategyFor(PhaseType phase)
        {
            switch (phase)
            {
                case PhaseType.Prompt: _currentStrategy = new Strategies.PromptPhase(); break;
                case PhaseType.Draw: _currentStrategy = new Strategies.DrawPhase(); break;
                case PhaseType.Guess: _currentStrategy = new Strategies.GuessPhase(); break;
                case PhaseType.Observe: _currentStrategy = new Strategies.ObservePhase(); break;
                case PhaseType.FinalGuess: _currentStrategy = new Strategies.FinalGuessPhase(); break;
                case PhaseType.Reveal: _currentStrategy = new Strategies.RevealPhase(); break;
                default: _currentStrategy = null; break;
            }
            _currentStrategy?.OnEnter(this);
        }

        public IReadOnlyList<PhaseAssignment> GetCurrentAssignments()
        {
            RefreshAssignments();
            return _assignments;
        }

        public bool TryGetAssignment(PlayerRef worker, out PhaseAssignment assignment)
        {
            RefreshAssignments();
            if (_assignments != null)
            {
                for (int i = 0; i < _assignments.Count; i++)
                {
                    if (_assignments[i].Worker == worker)
                    {
                        assignment = _assignments[i];
                        return true;
                    }
                }
            }

            assignment = default;
            return false;
        }

        private void ApplyRandomPlayOrder(IReadOnlyList<PlayerRef> orderedPlayers)
        {
            PlayOrderCount = 0;

            if (orderedPlayers == null || orderedPlayers.Count == 0)
            {
                return;
            }

            var registry = ServiceLocator.Get<PlayerRegistry>();
            if (registry == null)
            {
                return;
            }

            var shuffled = new List<PlayerRef>(orderedPlayers);
            var random = new Random(unchecked((int)DateTime.UtcNow.Ticks));
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                var swapIndex = random.Next(i + 1);
                var temp = shuffled[i];
                shuffled[i] = shuffled[swapIndex];
                shuffled[swapIndex] = temp;
            }

            for (byte i = 0; i < shuffled.Count; i++)
            {
                if (!registry.TryGetSlotIndex(shuffled[i], out var slotIndex))
                {
                    continue;
                }

                PlayOrderSlotIndices.Set(i, slotIndex);
                PlayOrderCount++;
            }

            _cachedAssignmentOrderCount = 0;
        }

        private void RefreshAssignments()
        {
            if (_mode == null)
            {
                _assignments = null;
                return;
            }

            if (CurrentPhase == PhaseType.None || CurrentPhase == PhaseType.Reveal)
            {
                return;
            }

            if (_assignments != null &&
                _cachedAssignmentPhase == CurrentPhase &&
                _cachedAssignmentRound == RoundIndex &&
                _cachedAssignmentMode == ActiveMode &&
                _cachedAssignmentOrderCount == PlayOrderCount)
            {
                return;
            }

            var orderedPlayers = ResolvePlayOrder();
            _assignments = _mode.BuildAssignments(RoundIndex, orderedPlayers);
            _cachedAssignmentPhase = CurrentPhase;
            _cachedAssignmentRound = RoundIndex;
            _cachedAssignmentMode = ActiveMode;
            _cachedAssignmentOrderCount = PlayOrderCount;
        }

        private List<PlayerRef> ResolvePlayOrder()
        {
            var registry = ServiceLocator.Get<PlayerRegistry>();
            var orderedPlayers = new List<PlayerRef>(PlayOrderCount);

            if (registry == null || PlayOrderCount == 0)
            {
                return registry != null ? registry.GetOrderedPlayers() : orderedPlayers;
            }

            for (int i = 0; i < PlayOrderCount; i++)
            {
                var slotIndex = PlayOrderSlotIndices.Get(i);
                if (registry.TryGetPlayerBySlotIndex(slotIndex, out var player))
                {
                    orderedPlayers.Add(player);
                }
            }

            return orderedPlayers;
        }
    }
}
