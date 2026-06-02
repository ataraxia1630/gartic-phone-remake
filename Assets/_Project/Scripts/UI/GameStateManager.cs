using UnityEngine;

public enum GameState
{
    Lobby,
    Writing,  
    Drawing,
    Waiting,   
    Result
}
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    [Header("UI Panels")]
    public GameObject lobbyPanel;
    public GameObject writingPanel;
    public GameObject drawingPanel;
    public GameObject waitingPanel;
    public GameObject resultPanel;

    private GameState currentState;

    private void Awake()
    {
        Instance = this;
    }

    public void ChangeState(GameState state)
    {
        currentState = state;
        HideAllPanels();

        switch (state)
        {
            case GameState.Lobby:
                lobbyPanel.SetActive(true);
                break;
            case GameState.Writing:
                writingPanel.SetActive(true);
                //writingPanel.GetInstanceID().Set
                break;
            case GameState.Drawing:
                drawingPanel.SetActive(true);
                break;
            case GameState.Waiting:
                waitingPanel.SetActive(true);
                break;
            case GameState.Result:
                resultPanel.SetActive(true);
                break;
        }
    }
    void HideAllPanels()
    {
        lobbyPanel.SetActive(false);
        writingPanel.SetActive(false);
        drawingPanel.SetActive(false);
        waitingPanel.SetActive(false);
        resultPanel.SetActive(false);
    }
}
