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
    /// Plays RobberNodes (the mugger on the F2 stairs): the reply line(s) (each costs its building seconds, like any
    /// dialogue), then the take outcomes, one queued toast per item taken, his closing line, his run down the stairs
    /// (sprite tween toward the stairwell door + fade), Mehmet's "no one got hurt" line, and finally the next node
    /// (the street) without any choice. No violence is shown. Rewinds put him back.
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

        [Tooltip("World units of his menacing step toward the family (replies that step closer).")]
        [SerializeField] private float stepDistance = 0.35f;

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
            // What he does (sneers / grabs the bag / steps closer) goes to the narration card.
            if (!node.BodyKey.IsEmpty) Presenter?.ShowNarration(node.BodyKey.Display);
            if (node.StepsCloser && FindActor()) StartCoroutine(StepCloser());
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

        /// <summary>Finds his hotspot in the current room once per robbery and remembers where he stood.</summary>
        private bool FindActor()
        {
            if (actor != null) return true;
            Room room = rooms != null ? rooms.Current : null;
            actor = room != null && node != null ? room.transform.Find("Hotspot_" + node.ActorName) : null;
            if (actor != null) actorRest = actor.position;
            return actor != null;
        }

        /// <summary>A quick step toward the player with a small lunge, and the family flinches. No contact.</summary>
        private IEnumerator StepCloser()
        {
            CharacterMotor[] family = FindObjectsByType<CharacterMotor>(FindObjectsInactive.Exclude);
            Vector3 from = actor.position;
            Vector3 toward = from;
            foreach (CharacterMotor motor in family)
            {
                if (motor.GetComponent<PlayerMover>() != null) toward = motor.transform.position;
            }

            Vector3 dir = toward - from;
            dir.z = 0f;
            Vector3 to = dir.sqrMagnitude > 0.0001f ? from + dir.normalized * stepDistance * (rooms != null && rooms.Current != null ? rooms.Current.Scale : 1f) : from;
            Vector3 baseScale = actor.localScale;
            yield return Tween.Run(0.22f, k =>
            {
                if (actor == null) return;
                actor.position = Vector3.Lerp(from, to, k);
                actor.localScale = baseScale * (1f + 0.06f * Mathf.Sin(k * Mathf.PI));
            }, Ease.OutQuad);

            foreach (CharacterMotor motor in family)
            {
                SpriteRenderer body = motor != null ? motor.GetComponentInChildren<SpriteRenderer>() : null;
                if (body != null && body.transform != motor.transform) StartCoroutine(Tween.Shake(body.transform, 0.3f, 0.04f));
            }
        }

        private IEnumerator RunOffRoutine()
        {
            if (FindActor())
            {
                Room room = rooms.Current;
                Vector3 from = actor.position;
                Vector3 target = room.GridToWorld(node.RunOffGrid, actor.position.z);
                SpriteRenderer[] sprites = actor.GetComponentsInChildren<SpriteRenderer>(true);
                var colors = new Color[sprites.Length];
                for (int i = 0; i < sprites.Length; i++) colors[i] = sprites[i].color;
                yield return Tween.Run(runOffSeconds, k =>
                {
                    if (actor == null) return;
                    actor.position = Vector3.Lerp(from, target, k);
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
            // He is gone: one line from Mehmet, then the street.
            Presenter?.ShowLine(UiKeys.PlayerName, TextDB.GetDisplay(UiKeys.RobberAfter), 0f, Finish, new List<ChoiceView>(), null);
            if (Presenter == null) Finish();
        }

        private void Finish()
        {
            if (node == null) return;
            Presenter?.Clear();
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
