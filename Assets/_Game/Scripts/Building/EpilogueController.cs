using System;
using System.Collections;
using SixtyOne.Core;
using SixtyOne.Nodes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SixtyOne.Building
{
    /// <summary>
    /// Shows the epilogue text for EpilogueNodes (no buttons; the run is over). Once it has been read (any key or
    /// click after a short minimum, or a timeout) it raises GameEvents.EpilogueDone: the end screen takes over.
    /// </summary>
    public sealed class EpilogueController : MonoBehaviour, INodeHandler
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Component implementing IDialoguePresenter (DialoguePresenter).")]
        [SerializeField] private MonoBehaviour presenter;

        [Tooltip("Seconds the epilogue stays before a key or click can move on (unscaled).")]
        [SerializeField] private float minReadSeconds = 4f;

        [Tooltip("Seconds after which the end screen comes anyway (unscaled).")]
        [SerializeField] private float maxReadSeconds = 20f;

        private Coroutine waiting;

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
            if (waiting != null) StopCoroutine(waiting);
            waiting = StartCoroutine(WaitForRead());
        }

        private IEnumerator WaitForRead()
        {
            float t = 0f;
            while (t < maxReadSeconds)
            {
                if (!PauseState.IsPaused) t += Time.unscaledDeltaTime;
                if (t >= minReadSeconds && AnyInput()) break;
                yield return null;
            }

            waiting = null;
            GameEvents.RaiseEpilogueDone();
        }

        private static bool AnyInput()
        {
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            Gamepad g = Gamepad.current;
            return (k != null && k.anyKey.wasPressedThisFrame && !k.escapeKey.wasPressedThisFrame)
                || (m != null && m.leftButton.wasPressedThisFrame)
                || (g != null && g.buttonSouth.wasPressedThisFrame);
        }

        public void OnTimeout()
        {
        }

        public void Cancel()
        {
            if (waiting != null) StopCoroutine(waiting);
            waiting = null;
        }
    }
}
