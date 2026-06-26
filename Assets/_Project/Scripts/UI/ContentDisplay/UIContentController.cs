using UnityEngine;
using InkEcho.UI.ContentDisplay.States;

namespace InkEcho.UI.ContentDisplay
{
    /// <summary>
    /// Controller chính quản lý UI Content State Machine
    /// Điều phối hiển thị/ẩn nội dung theo lượt chơi
    /// </summary>
    public class UIContentController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] public GameObject PromptPanel;
        [SerializeField] public GameObject DrawingPanel;

        [Header("Settings")]
        [SerializeField] public float RevealDuration = 1f;
        [SerializeField] public ContentDisplayType ContentType = ContentDisplayType.Prompt;
        [SerializeField] public RevealAnimationType RevealAnimationType = RevealAnimationType.FadeIn;

        // State machine
        private IUIContentState _currentState;
        private IUIContentState _hiddenState;
        private IUIContentState _displayingState;

        // Dữ liệu lượt chơi
        public int CurrentRound { get; private set; }
        public int CurrentChainIndex { get; private set; }

        private void Awake()
        {
            InitializeStates();
            TransitionToState(_hiddenState);
        }

        private void InitializeStates()
        {
            _hiddenState = new HiddenContentState();
            _displayingState = new DisplayingContentState();
        }

        private void Update()
        {
            if (_currentState != null)
                _currentState.Tick(this);
        }

        /// <summary>
        /// Chuyển sang trạng thái mới
        /// </summary>
        public void TransitionToState(IUIContentState newState)
        {
            if (_currentState != null)
                _currentState.OnExit(this);

            _currentState = newState;
            Debug.Log($"[UIContent] Transitioned to state: {_currentState.StateName}");
            _currentState.OnEnter(this);
        }

        /// <summary>
        /// Hiển thị nội dung từ người chơi trước
        /// </summary>
        public void DisplayPreviousContent(int round, int chainIndex, bool useRevealAnimation = true)
        {
            CurrentRound = round;
            CurrentChainIndex = chainIndex;

            if (useRevealAnimation)
            {
                var revealState = UIContentStateFactory.CreateRevealState(RevealAnimationType);
                TransitionToState(revealState);
            }
            else
                TransitionToState(_displayingState);
        }

        /// <summary>
        /// Ẩn nội dung
        /// </summary>
        public void HideContent()
        {
            TransitionToState(_hiddenState);
        }

        /// <summary>
        /// Set loại nội dung cần hiển thị
        /// </summary>
        public void SetContentType(ContentDisplayType type)
        {
            ContentType = type;
        }

        /// <summary>
        /// Set loại animation
        /// </summary>
        public void SetRevealAnimation(RevealAnimationType animationType)
        {
            RevealAnimationType = animationType;
        }
    }
}