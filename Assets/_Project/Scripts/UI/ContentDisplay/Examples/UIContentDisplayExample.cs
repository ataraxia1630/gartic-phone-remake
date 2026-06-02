using UnityEngine;
using InkEcho.UI.ContentDisplay;

namespace InkEcho.Examples
{
    /// <summary>
    /// Ví dụ về cách sử dụng UIContentController trong game flow
    /// </summary>
    public class UIContentDisplayExample : MonoBehaviour
    {
        private UIContentController _uiContentController;

        private void Start()
        {
            _uiContentController = GetComponent<UIContentController>();
        }

        /// <summary>
        /// Game Flow ví dụ (trong Gartic Phone remake):
        /// 1. Round 0: Prompt Writing
        /// 2. Trước Round 1: Show prompt (reveal animation)
        /// 3. Round 1: Drawing
        /// 4. Trước Round 2: Show drawing (reveal animation)
        /// 5. Round 2: Guess/Re-write prompt
        /// ... tiếp tục
        /// </summary>
        public void ExampleGameFlow()
        {
            // === Round 0: Người chơi viết prompt ===
            // (Prompt panel ẩn, người chơi gõ vào input field)

            // === Chuyển Round 1 - Drawing ===
            // Trước khi vào phase Drawing, hiển thị prompt từ Round 0
            _uiContentController.DisplayPreviousContent(round: 0, chainIndex: 0, useRevealAnimation: true);
            // → UIContentController sẽ:
            //   1. Chuyển sang RevealingState (fade in animation)
            //   2. Hiển thị prompt text từ Round 0
            //   3. Sau animation, chuyển sang DisplayingState
            //   4. Giữ hiển thị prompt cho người chơi xem trước khi vẽ

            // Sau timeout hoặc người chơi bấm "Start Drawing"
            _uiContentController.HideContent();
            // → Chuyển sang HiddenState, ẩn prompt
            // → Vẽ canvas hiển thị để người chơi vẽ

            // === Sau khi người chơi xong drawing ===
            // (Drawing lưu vào AlbumStore qua network)

            // === Chuyển Round 2 - Guessing ===
            // Trước khi vào phase Guessing, hiển thị drawing từ Round 1
            _uiContentController.DisplayPreviousContent(round: 1, chainIndex: 0, useRevealAnimation: true);
            // → Reveal drawing với animation
            // → Người chơi xem drawing rồi đoán

            // === Kết thúc ===
            // Sau khi hết tất cả rounds, show Result panel
        }

        /// <summary>
        /// Cách sử dụng các states riêng lẻ
        /// </summary>
        public void ManualStateControl()
        {
            // Ẩn nội dung
            _uiContentController.HideContent();

            // Hiển thị nội dung với reveal animation (1 giây)
            _uiContentController.DisplayPreviousContent(
                round: 0,
                chainIndex: 0,
                useRevealAnimation: true
            );

            // Hiển thị nội dung không animation
            _uiContentController.DisplayPreviousContent(
                round: 0,
                chainIndex: 0,
                useRevealAnimation: false
            );

            // Thay đổi loại nội dung
            _uiContentController.SetContentType(ContentDisplayType.Prompt);
            _uiContentController.SetContentType(ContentDisplayType.Drawing);
        }
    }
}
