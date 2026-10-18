using System;
using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Nodes
{
    /// <summary>
    /// Plays SceneNodes: enter -> on-enter outcomes -> bomb checks -> snapshot -> choices/timer.
    /// Re-enters the snapshot node after a rewind.
    /// </summary>
    public sealed class NodePlayer : MonoBehaviour
    {
        [Tooltip("Owner of the game state.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Node registry and start node.")]
        [SerializeField] private GameContent content;

        [Tooltip("Component implementing IDialoguePresenter (DialoguePresenter).")]
        [SerializeField] private MonoBehaviour presenter;

        [Tooltip("Component implementing IRoomPresenter (RoomSwitcher).")]
        [SerializeField] private MonoBehaviour roomPresenter;

        [Tooltip("Components implementing INodeHandler (PlacementController).")]
        [SerializeField] private List<MonoBehaviour> nodeHandlers = new List<MonoBehaviour>();

        [Tooltip("Enter the start node on Start.")]
        [SerializeField] private bool autoStart = true;

        [Tooltip("Component implementing IChoiceFilter (RoomNavigator): hides choices doors and hotspots offer. Optional.")]
        [SerializeField] private MonoBehaviour choiceFilter;

        private IDialoguePresenter choicePresenter;
        private IRoomPresenter rooms;
        private IChoiceFilter filter;
        private INodeHandler activeHandler;

        /// <summary>Timer of the current node (TimerBar reads it).</summary>
        public CountdownTimer Timer { get; } = new CountdownTimer();

        public SceneNode Current { get; private set; }
        public GameContent Content => content;

        private GameState State => gameManager.State;

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            choicePresenter = presenter as IDialoguePresenter;
            rooms = roomPresenter as IRoomPresenter;
            filter = choiceFilter as IChoiceFilter;
            if (choicePresenter == null)
            {
                Debug.LogWarning("[NodePlayer] No IDialoguePresenter assigned; choices will not show.", this);
            }
        }

        private void OnEnable()
        {
            GameEvents.Died += OnDied;
            GameEvents.Rewound += OnRewound;
            GameEvents.ItemAdded += OnInventoryChanged;
            GameEvents.ItemRemoved += OnInventoryChanged;
        }

        private void OnDisable()
        {
            GameEvents.Died -= OnDied;
            GameEvents.Rewound -= OnRewound;
            GameEvents.ItemAdded -= OnInventoryChanged;
            GameEvents.ItemRemoved -= OnInventoryChanged;
        }

        private void OnInventoryChanged(string itemId)
        {
            if (!InputGate.IsBlocked) RefreshCurrentChoices();
        }

        private void Start()
        {
            if (!autoStart) return;
            if (content == null || content.StartNode == null)
            {
                Debug.LogError("[NodePlayer] GameContent or its start node is missing.", this);
                return;
            }

            Enter(content.StartNode);
        }

        private void Update()
        {
            if (gameManager.IsDead || !Timer.Tick(Time.deltaTime))
            {
                return;
            }

            if (activeHandler != null)
            {
                activeHandler.OnTimeout();
                return;
            }

            Choice timeout = Current != null ? Current.GetTimeoutChoice() : null;
            if (timeout != null)
            {
                Execute(timeout);
            }
        }

        /// <summary>Enters a node. fromRewind skips on-enter effects, bomb checks and the new snapshot.</summary>
        public void Enter(SceneNode node, bool fromRewind = false)
        {
            if (node == null)
            {
                Debug.LogWarning("[NodePlayer] Tried to enter a null node.", this);
                return;
            }

            StopActive();
            // A rewind restored the room the player stood in at this node's snapshot (a sub-room of a flat).
            string restoredRoom = fromRewind ? State.CurrentRoomId : null;
            Current = node;
            State.CurrentNodeId = node.Id;
            State.Act = node.Act;
            State.CurrentRoomId = node.RoomId;
            rooms?.ShowRoom(node.RoomId, restoredRoom);

            if (!fromRewind)
            {
                OutcomeExecutor.Execute(node.OnEnter, State);

                // Bombs run before the snapshot so a death rewinds to the decision that armed it.
                BombCheck bomb = BombCheckRunner.Run(node.BombChecks, State);
                if (bomb != null)
                {
                    choicePresenter?.ShowNarration(node.BodyKey.IsEmpty ? string.Empty : node.BodyKey.Display);
                    gameManager.Die(bomb.Death.ToInfo());
                    return;
                }

                if (node.IsCheckpoint) gameManager.TakeCheckpoint(node.Id);
                else gameManager.TakeSnapshot(node.Id);
            }

            GameEvents.RaiseNodeEntered(node.Id);
            if (node.AfadAlertOnEnter)
            {
                GameEvents.RaiseAfadAlert(node.AlertMagnitude);
            }

            // Timer first: a handler may finish synchronously and enter the next node.
            Timer.Start(node.TimerSeconds);
            PresentChoices();
            activeHandler = FindHandler(node);
            ExplorationGate.Allowed = activeHandler == null;
            activeHandler?.Begin(node, OnHandlerFinished);
        }

        /// <summary>Picks a choice by its index in Current.Choices (UI callback).</summary>
        public void Choose(int index)
        {
            if (Current == null || gameManager.IsDead || index < 0 || index >= Current.Choices.Count)
            {
                return;
            }

            if (InputGate.IsBlocked)
            {
                return;
            }

            Choice choice = Current.Choices[index];
            if (!ConditionEvaluator.AllMet(choice.Conditions, State))
            {
                if (choice.FailBehaviour == FailBehaviour.ShowLockedFatal)
                {
                    // "Keşke yanıma alsaydım...": macro rewind to the prep start.
                    choicePresenter?.Clear();
                    GameEvents.RaiseChoiceMade(Current.Id, choice.LabelKey.Key);
                    Timer.Stop();
                    gameManager.Die(choice.BuildMissingItemDeath(State));
                }

                return;
            }

            if (ChoiceView.IsExhaustedFor(choice, State))
            {
                GameEvents.RaiseMessage(SixtyOne.Text.UiKeys.StaminaExhausted);
                return;
            }

            Execute(choice);
        }

        private void Execute(Choice choice)
        {
            choicePresenter?.Clear();
            GameEvents.RaiseChoiceMade(Current.Id, choice.LabelKey.Key);
            AudioService.PlayCue(choice.Cue);

            // Costs first: running out of building time while acting still kills (collapse).
            ActionCostRouter.SpendStamina(choice.StaminaCost);
            ActionCostRouter.SpendTime(choice.TimeCost, choice.IsMove);
            if (gameManager.IsDead)
            {
                return;
            }

            OutcomeResult result = OutcomeExecutor.Execute(choice.Outcomes, State);
            if (result.Died || choice.Type == ChoiceType.Trap)
            {
                Timer.Stop();
                gameManager.Die(choice.Death.ToInfo());
                return;
            }

            if (result.NextNode != null)
            {
                Enter(result.NextNode);
                return;
            }

            // Same node (e.g. "close the gas valve"): refresh choices, timer keeps running.
            if (activeHandler is IChoiceRefresher refresher) refresher.RefreshChoices();
            else PresentChoices();
        }

        /// <summary>Re-lists the current node's choices (e.g. after a bag item changed what is available).</summary>
        public void RefreshCurrentChoices()
        {
            if (Current == null || gameManager.IsDead) return;
            if (activeHandler is IChoiceRefresher refresher) refresher.RefreshChoices();
            else if (activeHandler == null) PresentChoices();
        }

        private void PresentChoices()
        {
            List<ChoiceView> views = ChoiceView.Build(Current, State);
            if (filter != null) views.RemoveAll(v => filter.Hides(Current, v.Choice));
            PresentNode(Current, views);
        }

        /// <summary>
        /// A node with choices the world does not offer = a decision: Mehmet's thought bubble (the body) + rows.
        /// Without such choices the body is narration (top card) and the room is explored freely.
        /// </summary>
        private void PresentNode(SceneNode node, List<ChoiceView> views)
        {
            if (choicePresenter == null) return;
            string body = node.BodyKey.IsEmpty ? string.Empty : node.BodyKey.Display;
            if (views.Count > 0) choicePresenter.ShowThought(body, views, Choose);
            else choicePresenter.ShowNarration(body);
        }

        private void OnHandlerFinished(SceneNode next)
        {
            activeHandler = null;
            Timer.Stop();
            if (next != null)
            {
                Enter(next);
            }
        }

        private INodeHandler FindHandler(SceneNode node)
        {
            foreach (MonoBehaviour behaviour in nodeHandlers)
            {
                if (behaviour is INodeHandler handler && handler.CanHandle(node))
                {
                    return handler;
                }
            }

            return null;
        }

        private void StopActive()
        {
            Timer.Stop();
            if (activeHandler != null)
            {
                INodeHandler handler = activeHandler;
                activeHandler = null;
                handler.Cancel();
            }
        }

        private void OnDied(DeathInfo info)
        {
            ExplorationGate.Allowed = false;
            StopActive();
            choicePresenter?.Clear();
        }

        private void OnRewound(string nodeId)
        {
            SceneNode node = content != null ? content.FindNode(nodeId) : null;
            if (node == null)
            {
                Debug.LogError($"[NodePlayer] Rewind target '{nodeId}' is not in GameContent.", this);
                return;
            }

            Enter(node, fromRewind: true);
        }
    }
}
