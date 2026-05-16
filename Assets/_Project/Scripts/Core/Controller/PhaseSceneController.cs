using UnityEngine;
using UnityEngine.SceneManagement;
using InkEcho.Network.Phases;
using InkEcho.Network.Core;

public class PhaseSceneController : MonoBehaviour
{
    private PhaseType _lastObservedPhase = PhaseType.None;
    private string _currentLoadedScene = "";

    void Update()
    {
        var phaseManager = ServiceLocator.Get<PhaseManager>();
        if (phaseManager == null) return;

        if (phaseManager.CurrentPhase != _lastObservedPhase)
        {
            _lastObservedPhase = phaseManager.CurrentPhase;
            SwitchPhaseScene(_lastObservedPhase, phaseManager);
        }
    }

    private void SwitchPhaseScene(PhaseType newPhase, PhaseManager manager)
    {
        if (!string.IsNullOrEmpty(_currentLoadedScene))
        {
            SceneManager.UnloadSceneAsync(_currentLoadedScene);
            _currentLoadedScene = "";
        }

        var modeConfig = manager.GetActiveModeConfig();
        if (modeConfig != null)
        {
            string sceneToLoad = modeConfig.GetSceneNameForPhase(newPhase);
            
            if (!string.IsNullOrEmpty(sceneToLoad))
            {
                SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
                _currentLoadedScene = sceneToLoad;
            }
        }
    }
}