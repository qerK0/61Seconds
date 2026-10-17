using System;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Dialogue
{
    /// <summary>Plays DialogueNodes through the choice presenter: line by line, then the ending choices.</summary>
    public sealed class DialogueController : MonoBehaviour, INodeHandler, IChoiceRefresher
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Routes choices back into the node flow.")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Component implementing IChoicePresenter (ChoicePanel).")]
        [SerializeField] private MonoBehaviour presenter;

        /// <summary>Speaker key of the line now shown, or null when no line is shown (dialogue voice blips).</summary>
        public static event Action<string> SpeakerChanged;

        private DialogueNode node;
        private int lineIndex;

        /// <summary>True while lines are still being shown (the building timer's real-time tick may pause).</summary>
        public bool IsPlayingLines => node != null && lineIndex < node.Lines.Count;

        private IChoicePresenter Presenter => presenter as IChoicePresenter;

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
        }

        public bool CanHandle(SceneNode sceneNode) => sceneNode is DialogueNode && !(sceneNode is RobberNode);

        public void Begin(SceneNode sceneNode, Action<SceneNode> done)
        {
            node = (DialogueNode)sceneNode;
            lineIndex = -1;
            Advance();
        }

        public void OnTimeout()
        {
        }

        public void Cancel()
        {
            node = null;
            SpeakerChanged?.Invoke(null);
        }

        public void RefreshChoices()
        {
            if (node == null) return;
            if (IsPlayingLines) ShowLine();
            else ShowEnding();
        }

        private void Advance()
        {
            if (node == null) return;
            lineIndex++;
            if (lineIndex < node.Lines.Count)
            {
                ActionCostRouter.SpendTime(node.Lines[lineIndex].TimeCost);
                if (gameManager != null && gameManager.IsDead) return;
                ShowLine();
            }
            else
            {
                ShowEnding();
            }
        }

        private void ShowLine()
        {
            DialogueLine line = node.Lines[lineIndex];
            SpeakerChanged?.Invoke(line.SpeakerKey.IsEmpty ? null : line.SpeakerKey.Key);
            Presenter?.PresentText(
                line.SpeakerKey.IsEmpty ? string.Empty : line.SpeakerKey.Display,
                line.TextKey.Display,
                TextDB.GetDisplay(UiKeys.DialogueContinue),
                Advance,
                ChoiceView.Build(node, gameManager.State, DialogueFilter.DuringDialogue),
                Choose);
        }

        private void ShowEnding()
        {
            SpeakerChanged?.Invoke(null);
            Presenter?.Present(node, ChoiceView.Build(node, gameManager.State, DialogueFilter.AfterDialogue), Choose);
        }

        private void Choose(int index)
        {
            if (nodePlayer != null) nodePlayer.Choose(index);
        }
    }
}
