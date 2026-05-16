using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using InkEcho.Network.Data;
using InkEcho.Network.Phases;
using InkEcho.Network.Players;
using InkEcho.Network.StateMachine;
using UnityEngine;

namespace InkEcho.Network.Core
{
    public class NetworkBootstrap : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private NetworkRunner runnerPrefab;
        [SerializeField] private NetworkConfig config;

        private NetworkRunner _runner;
        private SessionService _session;

        public NetworkConfig Config => config;
        public SessionService Session => _session;
        public NetworkRunner Runner => _runner;

        public event Action<PlayerRef> OnPlayerJoinedEvent;
        public event Action<PlayerRef> OnPlayerLeftEvent;
        public event Action<ShutdownReason> OnShutdownEvent;
        public event Action<NetworkRunner> OnSceneLoadStartEvent;
        public event Action<NetworkRunner> OnSceneLoadDoneEvent;

        public static NetworkBootstrap Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register<NetworkBootstrap>(this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ServiceLocator.Unregister<NetworkBootstrap>(this);
                Instance = null;
            }
        }

        public Task<bool> Host()
        {
            EnsureRunner();
            return _session.HostAsync();
        }

        public Task<bool> Join(string code)
        {
            EnsureRunner();
            return _session.JoinAsync(code);
        }

        public void Leave() => _session?.Leave();

        private void EnsureRunner()
        {
            if (_runner != null) return;
            if (runnerPrefab == null)
            {
                Debug.LogError("[NetworkBootstrap] runnerPrefab not assigned");
                return;
            }
            if (config == null)
            {
                Debug.LogError("[NetworkBootstrap] config not assigned");
                return;
            }

            _runner = Instantiate(runnerPrefab);
            _runner.AddCallbacks(this);
            _runner.ProvideInput = false;
            _session = new SessionService(_runner, config);
            ServiceLocator.Register<SessionService>(_session);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
            => OnPlayerJoinedEvent?.Invoke(player);

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            OnPlayerLeftEvent?.Invoke(player);
            TryReclaimAuthorityIfMaster();
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            OnShutdownEvent?.Invoke(shutdownReason);
            if (_runner == runner)
            {
                ServiceLocator.Unregister<SessionService>(_session);
                _session = null;
                _runner = null;
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            _session?.UpdateAvailableSessions(sessionList);
        }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) => OnSceneLoadDoneEvent?.Invoke(runner);
        public void OnSceneLoadStart(NetworkRunner runner) => OnSceneLoadStartEvent?.Invoke(runner);
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        private void TryReclaimAuthorityIfMaster()
        {
            if (_runner == null || !_runner.IsRunning || !_runner.IsSharedModeMasterClient) return;

            Reclaim(ServiceLocator.Get<PlayerRegistry>());
            Reclaim(ServiceLocator.Get<GameStateMachine>());
            Reclaim(ServiceLocator.Get<PhaseManager>());
            Reclaim(ServiceLocator.Get<AlbumStore>());
        }

        private void Reclaim(NetworkBehaviour behaviour)
        {
            if (behaviour == null || behaviour.HasStateAuthority || behaviour.Object == null) return;
            behaviour.Object.RequestStateAuthority();
        }
    }
}
