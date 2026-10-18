using System;
using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Nodes;
using UnityEngine;

namespace SixtyOne.Building
{
    /// <summary>Shows the epilogue text for EpilogueNodes (no buttons; the run is over).</summary>
    public sealed class EpilogueController : MonoBehaviour, INodeHandler
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Component implementing IDialoguePresenter (DialoguePresenter).")]
        [SerializeField] private MonoBehaviour presenter;

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
        }

        public bool CanHandle(SceneNode node) => node is EpilogueNode;

        public void Begin(SceneNode node, Action<SceneNode> done)
        {
            var epilogue = (EpilogueNode)node;
            string title = epilogue.TitleKey.IsEmpty ? string.Empty : epilogue.TitleKey.Display;
            string text = EpilogueText.Compose(epilogue, gameManager.State);
            (presenter as IDialoguePresenter)?.ShowNarration(string.IsNullOrEmpty(title) ? text : $"<b>{title}</b>\n{text}", true);
        }

        public void OnTimeout()
        {
        }

        public void Cancel()
        {
        }
    }
}
