using System;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Puzzles
{
    /// <summary>Plays FusePuzzleNodes: toggles, attempts (wrong = time cost + spark message), solve → next node.</summary>
    public sealed class FusePuzzleController : MonoBehaviour, INodeHandler
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Component implementing IFusePanel.")]
        [SerializeField] private MonoBehaviour panel;

        private FusePuzzleNode node;
        private Action<SceneNode> onFinished;
        private bool[] switches;

        private IFusePanel Panel => panel as IFusePanel;

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
        }

        public bool CanHandle(SceneNode sceneNode) => sceneNode is FusePuzzleNode;

        public void Begin(SceneNode sceneNode, Action<SceneNode> done)
        {
            node = (FusePuzzleNode)sceneNode;
            onFinished = done;
            switches = new bool[node.Solution.Length];
            Panel?.Show(switches, Toggle, Try);
        }

        public void OnTimeout()
        {
        }

        public void Cancel()
        {
            node = null;
            onFinished = null;
            Panel?.Hide();
        }

        private void Toggle(int index)
        {
            if (node == null || gameManager.IsDead) return;
            FusePuzzle.Toggle(switches, index);
            Panel?.Refresh(switches);
        }

        private void Try()
        {
            if (node == null || gameManager.IsDead) return;
            if (!FusePuzzle.Matches(switches, node.Solution))
            {
                ActionCostRouter.SpendTime(node.WrongAttemptSeconds);
                GameEvents.RaiseMessage(UiKeys.FuseWrong);
                return;
            }

            OutcomeExecutor.Execute(node.SolvedOutcomes, gameManager.State);
            GameEvents.RaiseMessage(UiKeys.FuseSolved);
            Panel?.Hide();
            Action<SceneNode> done = onFinished;
            SceneNode next = node.NextNode;
            node = null;
            onFinished = null;
            done?.Invoke(next);
        }
    }
}
