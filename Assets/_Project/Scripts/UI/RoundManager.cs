using System.Collections.Generic;
using UnityEngine;
using InkEcho.UI.ContentDisplay;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance;

    private int currentRound = 0;
    private int totalRounds;
    private int currentChainIndex = 0;

    private List<object> chain = new List<object>();
    private string currentPrompt;

    [SerializeField] private UIContentController uiContentController;

    private void Awake()
    {
        Instance = this;
    }

    public void StartGame(int rounds)
    {
        totalRounds = rounds;
        currentRound = 0;
        currentChainIndex = 0;
        chain.Clear();

        if (uiContentController != null)
            uiContentController.HideContent();

        GameStateManager.Instance.ChangeState(GameState.Writing);
    }

    /// <summary>
    /// Gọi khi người chơi bấm done writing
    /// </summary>
    public void SubmitWriting(string prompt)
    {
        chain.Add(prompt);
        currentPrompt = prompt;

        // Ẩn prompt, chuyển sang state tiếp theo
        if (uiContentController != null)
            uiContentController.HideContent();

        AdvanceRound();
    }

    /// <summary>
    /// Hiển thị nội dung từ người chơi trước (trước khi người hiện tại thực hiện lượt của họ)
    /// </summary>
    public void ShowPreviousContent(int chainIndex)
    {
        currentChainIndex = chainIndex;

        if (uiContentController != null)
        {
            // Xác định loại nội dung cần hiển thị
            ContentDisplayType contentType = (currentRound % 2 == 0) 
                ? ContentDisplayType.Prompt 
                : ContentDisplayType.Drawing;

            uiContentController.SetContentType(contentType);
            uiContentController.DisplayPreviousContent(currentRound, chainIndex, useRevealAnimation: true);
        }
    }

    void AdvanceRound()
    {
        currentRound++;
        if (currentRound >= totalRounds)
        {
            GameStateManager.Instance.ChangeState(GameState.Result);
        }
        else
        {
            // Chuyển sang phase tiếp theo dựa trên round
            if (currentRound % 2 == 0)
                GameStateManager.Instance.ChangeState(GameState.Writing);
            else
                GameStateManager.Instance.ChangeState(GameState.Drawing);
        }
    }
}
