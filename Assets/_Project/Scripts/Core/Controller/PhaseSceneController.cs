using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using InkEcho.Network.Phases;
using InkEcho.Network.Core;

public class PhaseSceneController : MonoBehaviour
{
    private PhaseType _lastObservedPhase = PhaseType.None;
    private string _currentLoadedScene = "";
    private bool _loggedAlive;

    void OnEnable()
    {
        _loggedAlive = false;
        Debug.Log($"[PhaseSceneController] OnEnable on GameObject '{gameObject.name}' (scene: {gameObject.scene.name})");
    }
    void Awake()
    {
        // BasePhase và các scene UI_* (load additive) không có EventSystem nào.
        // EventSystem duy nhất nằm ở scene lobby đã bị huỷ khi LoadScene(Single) sang BasePhase,
        // khiến mọi Button/Toggle/InputField không nhận được click trong suốt gameplay.
        // Tạo một EventSystem cho gameplay nếu chưa có (sẽ tự bị huỷ khi BasePhase unload sang ResultScene,
        // nơi đã có EventSystem riêng — nên không gây trùng).
        EnsureEventSystem();
    }

    private static void EnsureEventSystem()
    {
        var all = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (all.Length == 0)
        {
            var go = new GameObject("EventSystem (gameplay-auto)");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[PhaseSceneController] Created EventSystem for gameplay scenes.");
            return;
        }
        for (int i = 1; i < all.Length; i++)
        {
            Debug.LogWarning($"[PhaseSceneController] Destroying duplicate EventSystem: {all[i].gameObject.name}");
            Object.Destroy(all[i].gameObject);
        }
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
            return;
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
        string sceneToLoad = null;

        if (modeConfig != null)
            sceneToLoad = modeConfig.GetSceneNameForPhase(newPhase);

        // Fallback: nếu ModeConfig chưa assign hoặc trả empty, dùng default scene names
        if (string.IsNullOrEmpty(sceneToLoad))
            sceneToLoad = GetDefaultSceneForPhase(newPhase);

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log($"[PhaseSceneController] Loading phase scene: {sceneToLoad} for phase {newPhase}");
            SceneManager.LoadSceneAsync(sceneToLoad, LoadSceneMode.Additive);
            _currentLoadedScene = sceneToLoad;
        }
        else
        {
            Debug.LogWarning($"[PhaseSceneController] No scene mapped for phase {newPhase}");
        }
    }

    private static string GetDefaultSceneForPhase(PhaseType phase)
    {
        switch (phase)
        {
            case PhaseType.Prompt:     return "UI_Prompt";
            case PhaseType.Draw:       return "UI_Draw";
            case PhaseType.Guess:      return "UI_Guess";
            case PhaseType.Observe:    return "UI_Observe";
            case PhaseType.FinalGuess: return "UI_FinalGuess";
            case PhaseType.Reveal:     return "UI_Reveal";
            default:                   return null;
        }
    }
}