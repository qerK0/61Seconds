using System;

namespace SixtyOne.QTE
{
    /// <summary>What QTEController needs from the QTE UI (QTEView).</summary>
    public interface IQTEView
    {
        /// <summary>Shows the view; onPress is called for on-screen key buttons (touch / mouse).</summary>
        void Show(QTEContext context, string prompt, Action<QTEKey> onPress);

        /// <summary>Redraws the target key, progress pips and countdown.</summary>
        void Refresh(QTERun run);

        /// <summary>Correct key feedback.</summary>
        void FlashHit(int keysDone);

        /// <summary>Miss feedback (shake + red flash).</summary>
        void FlashMiss(QTEFailReason reason);

        void Hide();
    }
}
