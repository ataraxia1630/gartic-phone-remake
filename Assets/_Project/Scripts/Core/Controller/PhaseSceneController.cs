using UnityEngine;
using UnityEngine.SceneManagement;
using InkEcho.Network.Phases;
using InkEcho.Network.Core;

public class PhaseSceneController : MonoBehaviour
{
    private PhaseType _lastObservedPhase = PhaseType.None;
    private string _currentLoadedScene = "";
    private bool _loggedAlive;
    private bool _loggedMissingPhaseManager;

    void OnEnable()
    {
        _loggedAlive = false;
        Debug.Log($"[PhaseSceneController] OnEnable on GameObject '{gameObject.name}' (scene: {gameObject.scene.name})");
    }
    void Update()
    {
        if (!_loggedAlive)
        {
            _loggedAlive = true;
            Debug.Log($"[PhaseSceneController] Running Update on '{gameObject.name}' (scene: {gameObject.scene.name})");
        }
        var phaseManager = ServiceLocator.Get<PhaseManager>();
        if (phaseManager == null)
        {
            if (!_loggedMissingPhaseManager)
            {
                _loggedMissingPhaseManager = true;
                Debug.LogWarning("[PhaseSceneController] ServiceLocator.Get<PhaseManager>() returned NULL");
            }
            return;
        }
        _loggedMissingPhaseManager = false;
        if (phaseManager.CurrentPhase != _lastObservedPhase)
        {
            Debug.Log($"[PhaseSceneController] Phase changed: {_lastObservedPhase} -> {phaseManager.CurrentPhase} (HasStateAuthority={phaseManager.HasStateAuthority})");
            _lastObservedPhase = phaseManager.CurrentPhase;
            SwitchPhaseScene(_lastObservedPhase, phaseManager);
        }
    }

    private void SwitchPhaseScene(PhaseType newPhase, PhaseManager manager)
    {
        if (!string.IsNullOrEmpty(_currentLoadedScene))
        {
            Debug.Log($"[PhaseSceneController] Unloading previous scene: {_currentLoadedScene}");

            SceneManager.UnloadSceneAsync(_currentLoadedScene);
            _currentLoadedScene = "";
        }

        var modeConfig = manager.GetActiveModeConfig();
        Debug.Log($"[PhaseSceneController] SwitchPhaseScene: newPhase={newPhase}, ActiveMode={manager.ActiveMode}, modeConfig={(modeConfig == null ? "NULL" : modeConfig.name)}");

        if (modeConfig != null)
        {
            string sceneToLoad = modeConfig.GetSceneNameForPhase(newPhase);
            Debug.Log($"[PhaseSceneController] sceneToLoad resolved to: \"{sceneToLoad}\"");

            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
                _currentLoadedScene = sceneToLoad;
                Debug.Log($"[PhaseSceneController] Loading scene additively: {sceneToLoad}");
            }
        }
    }
}