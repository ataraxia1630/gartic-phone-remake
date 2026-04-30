using System.Collections.Generic;
using Fusion;
using GarticPhone.Network.Core;
using GarticPhone.Network.GameModes;
using GarticPhone.Network.Players;
using GarticPhone.Network.Data;

namespace GarticPhone.Network.Phases
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
        private IPhaseStrategy _currentStrategy;
        private IGameMode _mode;
        private IReadOnlyList<PhaseAssignment> _assignments;
        private GameModeConfig _modeConfigCache;
        private GameModeType _modeConfigCachedFor;

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
            album?.Init(playerCount, TotalRounds);

            _assignments = _mode.BuildAssignments(RoundIndex, ordered);

            CurrentPhase = PhaseType.Prompt;
            InstallStrategyFor(CurrentPhase);
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
            _currentStrategy = null;
            _mode = null;
            _assignments = null;
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

            if (nextPhase == PhaseType.Draw || nextPhase == PhaseType.Guess || nextPhase == PhaseType.Prompt)
            {
                var ordered = ServiceLocator.Get<PlayerRegistry>()?.GetOrderedPlayers() ?? new List<PlayerRef>();
                _assignments = _mode.BuildAssignments(RoundIndex, ordered);
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
                case PhaseType.Reveal: _currentStrategy = new Strategies.RevealPhase(); break;
                default: _currentStrategy = null; break;
            }
            _currentStrategy?.OnEnter(this);
        }

        public IReadOnlyList<PhaseAssignment> GetCurrentAssignments() => _assignments;

        public bool TryGetAssignment(PlayerRef worker, out PhaseAssignment assignment)
        {
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
    }
}
