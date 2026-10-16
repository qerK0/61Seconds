using System;
using System.Collections.Generic;

namespace SixtyOne.Nodes
{
    /// <summary>UI that shows a node's text and choices. Keeps NodePlayer decoupled from uGUI.</summary>
    public interface IChoicePresenter
    {
        /// <summary>Shows node text and buttons. onChosen receives ChoiceView.Index. Empty list = text only.</summary>
        void Present(SceneNode node, IReadOnlyList<ChoiceView> choices, Action<int> onChosen);

        /// <summary>
        /// Shows free text (dialogue line, epilogue) with an optional continue button and extra choices.
        /// continueLabel null = no continue button.
        /// </summary>
        void PresentText(string title, string body, string continueLabel, Action onContinue, IReadOnlyList<ChoiceView> choices, Action<int> onChosen);

        /// <summary>Hides everything.</summary>
        void Clear();
    }
}
