using System.Collections.Generic;
using SixtyOne.Core;

namespace SixtyOne.Nodes
{
    /// <summary>A choice as the UI should show it.</summary>
    public readonly struct ChoiceView
    {
        /// <summary>Index into SceneNode.Choices.</summary>
        public readonly int Index;
        public readonly Choice Choice;
        public readonly bool Locked;

        /// <summary>Locked but clickable (ShowLockedFatal): clicking kills.</summary>
        public readonly bool Fatal;

        /// <summary>True if the button reacts to clicks.</summary>
        public bool Clickable => !Locked || Fatal;

        public ChoiceView(int index, Choice choice, bool locked, bool fatal = false)
        {
            Index = index;
            Choice = choice;
            Locked = locked;
            Fatal = locked && fatal;
        }

        /// <summary>Builds the visible list for a node: hides timeout-only/hidden choices, marks locked ones.</summary>
        public static List<ChoiceView> Build(SceneNode node, GameState state)
        {
            var views = new List<ChoiceView>();
            for (int i = 0; i < node.Choices.Count; i++)
            {
                Choice choice = node.Choices[i];
                if (choice == null || choice.TimeoutOnly || !ConditionEvaluator.AllMet(choice.ShowIf, state))
                {
                    continue;
                }

                bool met = ConditionEvaluator.AllMet(choice.Conditions, state);
                if (!met && choice.FailBehaviour == FailBehaviour.Hide)
                {
                    continue;
                }

                views.Add(new ChoiceView(i, choice, !met, choice.FailBehaviour == FailBehaviour.ShowLockedFatal));
            }

            return views;
        }
    }
}
