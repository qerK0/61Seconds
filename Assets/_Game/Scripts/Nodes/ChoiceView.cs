using System.Collections.Generic;
using SixtyOne.Core;

namespace SixtyOne.Nodes
{
    /// <summary>Which choices of a dialogue node to list.</summary>
    public enum DialogueFilter
    {
        /// <summary>Every choice (normal nodes).</summary>
        All,
        /// <summary>Only choices marked DuringDialogue (while lines play).</summary>
        DuringDialogue,
        /// <summary>Only the ending choices (after the last line).</summary>
        AfterDialogue
    }

    /// <summary>A choice as the UI should show it.</summary>
    public readonly struct ChoiceView
    {
        /// <summary>Index into SceneNode.Choices.</summary>
        public readonly int Index;
        public readonly Choice Choice;
        public readonly bool Locked;

        /// <summary>Locked but clickable (ShowLockedFatal): clicking kills.</summary>
        public readonly bool Fatal;

        /// <summary>Text key that replaces the choice's own locked reason (e.g. exhausted). Null = use the choice's.</summary>
        public readonly string ReasonOverride;

        /// <summary>True if the button reacts to clicks.</summary>
        public bool Clickable => !Locked || Fatal;

        public ChoiceView(int index, Choice choice, bool locked, bool fatal = false, string reasonOverride = null)
        {
            Index = index;
            Choice = choice;
            Locked = locked;
            Fatal = locked && fatal;
            ReasonOverride = reasonOverride;
        }

        /// <summary>Stamina at or below this counts as exhausted (heavy choices lock).</summary>
        public const float ExhaustedStamina = 0.01f;

        /// <summary>
        /// Builds the visible list for a node: hides timeout-only/hidden choices, marks locked ones.
        /// Heavy choices lock (with the exhausted reason) when stamina is 0.
        /// </summary>
        public static List<ChoiceView> Build(SceneNode node, GameState state, DialogueFilter filter = DialogueFilter.All)
        {
            var views = new List<ChoiceView>();
            for (int i = 0; i < node.Choices.Count; i++)
            {
                Choice choice = node.Choices[i];
                if (choice == null || choice.TimeoutOnly || !ConditionEvaluator.AllMet(choice.ShowIf, state))
                {
                    continue;
                }

                if (filter == DialogueFilter.DuringDialogue && !choice.DuringDialogue) continue;
                if (filter == DialogueFilter.AfterDialogue && choice.DuringDialogue) continue;

                bool met = ConditionEvaluator.AllMet(choice.Conditions, state);
                if (!met && choice.FailBehaviour == FailBehaviour.Hide)
                {
                    continue;
                }

                if (met && IsExhaustedFor(choice, state))
                {
                    views.Add(new ChoiceView(i, choice, true, false, SixtyOne.Text.UiKeys.StaminaExhausted));
                    continue;
                }

                views.Add(new ChoiceView(i, choice, !met, choice.FailBehaviour == FailBehaviour.ShowLockedFatal));
            }

            return views;
        }

        /// <summary>True if the choice is heavy and the player has no stamina left.</summary>
        public static bool IsExhaustedFor(Choice choice, GameState state)
        {
            return choice != null && choice.Heavy && state.Stamina <= ExhaustedStamina;
        }
    }
}
