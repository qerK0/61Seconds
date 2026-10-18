using System;
using System.Collections.Generic;

namespace SixtyOne.Nodes
{
    /// <summary>
    /// UI for lines, narration and choices (speech bubbles, the narration card). Controllers only talk to this, so
    /// they never know about uGUI. Choice callbacks receive ChoiceView.Index.
    /// </summary>
    public interface IDialoguePresenter
    {
        /// <summary>
        /// A spoken line in the speaker's bubble (speakerKey = name text key, e.g. npc.sevim.name). onContinue runs
        /// when the player advances; pills = during-dialogue choices ("Kısa kes"); timeCost = building seconds the
        /// line costs (shown as a chip, 0 = none).
        /// </summary>
        void ShowLine(string speakerKey, string text, float timeCost, Action onContinue, IReadOnlyList<ChoiceView> pills, Action<int> onChosen);

        /// <summary>Text with no speaker (node body, hints, epilogue). persistent = stays until cleared.</summary>
        void ShowNarration(string text, bool persistent = false);

        /// <summary>A player thought / decision on Mehmet (thought-bubble style) with its choice rows.</summary>
        void ShowThought(string text, IReadOnlyList<ChoiceView> choices, Action<int> onChosen);

        /// <summary>Choice rows under the speaker's bubble (it keeps its last line).</summary>
        void ShowChoices(string speakerKey, IReadOnlyList<ChoiceView> choices, Action<int> onChosen);

        /// <summary>Hides everything.</summary>
        void Clear();
    }
}
