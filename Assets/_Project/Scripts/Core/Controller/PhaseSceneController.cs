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
        if (FindObjectOfType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem (gameplay-auto)");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        Debug.Log("[PhaseSceneController] No EventSystem in gameplay scenes — created one so UI buttons work.");
    }

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