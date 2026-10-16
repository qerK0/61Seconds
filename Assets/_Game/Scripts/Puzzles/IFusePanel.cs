using System;

namespace SixtyOne.Puzzles
{
    /// <summary>UI for a fuse puzzle (implemented by UI.FusePuzzlePanel).</summary>
    public interface IFusePanel
    {
        /// <summary>Shows the switches. onToggle(index), onTry().</summary>
        void Show(bool[] switches, Action<int> onToggle, Action onTry);

        /// <summary>Redraws switch states.</summary>
        void Refresh(bool[] switches);

        void Hide();
    }
}
