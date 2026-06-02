using UnityEngine;
using InkEcho.UI.ContentDisplay.States;

namespace InkEcho.UI.ContentDisplay
{
    /// <summary>
    /// Factory để tạo các state dựa trên animation type
    /// </summary>
    public static class UIContentStateFactory
    {
        public static IUIContentState CreateRevealState(RevealAnimationType animationType)
        {
            return animationType switch
            {
                RevealAnimationType.FadeIn => new RevealingContentState(),
                RevealAnimationType.SlideInUp => new SlideInContentState(),
                RevealAnimationType.ScaleIn => new ScaleInContentState(),
                RevealAnimationType.None => new DisplayingContentState(),
                _ => new RevealingContentState()
            };
        }
    }
}
