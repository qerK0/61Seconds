using System;
using System.Collections;
using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Dialogue
{
    /// <summary>
    /// Plays RobberNodes: the reply line(s) (each costs its building seconds, like any dialogue), then the take
    /// outcomes, one queued toast per item taken, the closing or bitter line, his run down the stairs (sprite
    /// tween + fade), and finally the next node (the street) without any choice. Rewinds put him back.
    /// </summary>
    public sealed class RobberController : MonoBehaviour, INodeHandler
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Component implementing IDialoguePresenter (DialoguePresenter).")]
        [SerializeField] private MonoBehaviour presenter;

        [Tooltip("Current room (finds the man's sprite).")]
        [SerializeField] private RoomSwitcher rooms;

        [Tooltip("Item registry (names for the toasts).")]
        [SerializeField] private GameContent content;

        [Tooltip("Seconds of his run down the stairs.")]
        [Min(0.1f)]
        [SerializeField] private float runOffSeconds = 0.9f;

        private RobberNode node;
        private Action<SceneNode> onFinished;
        private int lineIndex;
        private Transform actor;
        private Vector3 actorRest;
        private Coroutine running;

        private IDialoguePresenter Presenter => presenter as IDialoguePresenter;

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
        }

        private void OnEnable()
        {
            GameEvents.Rewound += OnRewound;
        }

        private void OnDisable()
        {
            GameEvents.Rewound -= OnRewound;
        }

        public bool CanHandle(SceneNode sceneNode) => sceneNode is RobberNode;

        public void Begin(SceneNode sceneNode, Action<SceneNode> done)
        {
            node = (RobberNode)sceneNode;
            onFinished = done;
            lineIndex = -1;
            RestoreActor();
            Advance();
        }

        public void OnTimeout()
        {
        }

        public void Cancel()
        {
            if (running != null) StopCoroutine(running);
            running = null;
            node = null;
            onFinished = null;
        }

        private void Advance()
        {
            if (node == null) return;
            lineIndex++;
            if (lineIndex < node.Lines.Count)
            {
                DialogueLine line = node.Lines[lineIndex];
                ActionCostRouter.SpendTime(line.TimeCost);
                if (gameManager != null && gameManager.IsDead) return;
                Say(line.SpeakerKey, line.TextKey, Advance);
                return;
            }

            Rob();
        }

        private void Rob()
        {
            OutcomeResult result = OutcomeExecutor.Execute(node.TakeOutcomes, gameManager.State);
            List<string> taken = result.Taken ?? new List<string>();
            foreach (string itemId in taken)
            {
                Item item = content != null ? content.FindItem(itemId) : null;
                string itemName = item != null ? item.NameKey.Display : itemId;
                GameEvents.RaiseQueuedMessage(TextDB.Format(node.TakenItemKey.Key, itemName));
            }

            Say(node.SpeakerKey, node.ClosingLine(taken.Count), RunOff);
        }

        private void Say(TextKey speaker, TextKey line, Action next)
        {
            Presenter?.ShowLine(speaker.IsEmpty ? null : speaker.Key, line.Display, 0f, next, new List<ChoiceView>(), null);
        }

        private void RunOff()
        {
            if (node == null) return;
            Presenter?.Clear();
            running = StartCoroutine(RunOffRoutine());
        }

        private IEnumerator RunOffRoutine()
        {
            Room room = rooms != null ? rooms.Current : null;
            actor = room != null ? room.transform.Find("Hotspot_" + node.ActorName) : null;
            if (actor != null)
            {
                actorRest = actor.position;
                Vector3 target = room.GridToWorld(node.RunOffGrid, actor.position.z);
                SpriteRenderer[] sprites = actor.GetComponentsInChildren<SpriteRenderer>(true);
                var colors = new Color[sprites.Length];
                for (int i = 0; i < sprites.Length; i++) colors[i] = sprites[i].color;
                yield return Tween.Run(runOffSeconds, k =>
                {
                    if (actor == null) return;
                    actor.position = Vector3.Lerp(actorRest, target, k);
                    float fade = Mathf.Clamp01((k - 0.5f) * 2f);
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        if (sprites[i] == null) continue;
                        Color c = colors[i];
                        c.a *= 1f - fade;
                        sprites[i].color = c;
                    }
                }, Ease.InQuad);
                if (actor != null) actor.gameObject.SetActive(false);
            }

            running = null;
            SceneNode next = node != null ? node.NextNode : null;
            Action<SceneNode> done = onFinished;
            onFinished = null;
            node = null;
            done?.Invoke(next);
        }

        // A rewind (to Floor 2 or the stairs) brings him back where he stood.
        private void RestoreActor()
        {
            if (actor == null) return;
            actor.gameObject.SetActive(true);
            actor.position = actorRest;
            foreach (SpriteRenderer r in actor.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Color c = r.color;
                c.a = 1f;
                r.color = c;
            }

            actor = null;
        }

        private void OnRewound(string nodeId) => RestoreActor();
    }
}
