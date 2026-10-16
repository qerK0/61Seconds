using System;
using System.Collections;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Prep
{
    /// <summary>
    /// Drives a PrepPhaseNode: runs the countdown, timed actions (input blocked, countdown keeps running)
    /// and room travel, then hands off to the linked PlacementNode at the placement window. The countdown
    /// keeps ticking through the placement window so the big display and last-10s ticks continue.
    /// </summary>
    public sealed class PrepController : MonoBehaviour, INodeHandler
    {
        [Tooltip("Owner of the game state.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Switches rooms during prep.")]
        [SerializeField] private RoomSwitcher rooms;

        private Action<SceneNode> onFinished;
        private Action onActionComplete;
        private bool handedOff;
        private bool gateHeld;

        public PrepCountdown Countdown { get; } = new PrepCountdown();
        public PrepAction Action { get; } = new PrepAction();
        public PrepPhaseNode Node { get; private set; }

        /// <summary>Label key of the running action (for the progress bar).</summary>
        public string ActionLabelKey { get; private set; }

        /// <summary>Prep running and not yet handed off to placement.</summary>
        public bool IsPrepActive => Node != null && Countdown.Running && !handedOff;
        public bool Busy => Action.IsActive;
        public bool CanInteract => IsPrepActive && !Busy && gameManager != null && !gameManager.IsDead;

        private GameState State => gameManager.State;

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            Countdown.PlacementWindowReached += OnPlacementWindow;
            Countdown.SecondTick += OnSecondTick;
            Countdown.Zero += GameEvents.RaisePrepZero;
        }

        private void OnEnable()
        {
            GameEvents.Died += OnDied;
            GameEvents.Rewound += OnRewound;
        }

        private void OnDisable()
        {
            GameEvents.Died -= OnDied;
            GameEvents.Rewound -= OnRewound;
            CancelAction();
        }

        private void Update()
        {
            if (Node == null || gameManager == null || gameManager.IsDead) return;

            float dt = Time.deltaTime;
            if (Action.IsActive && Action.Tick(dt))
            {
                CompleteAction();
            }

            Countdown.Tick(dt);
        }

        public bool CanHandle(SceneNode node) => node is PrepPhaseNode;

        public void Begin(SceneNode node, Action<SceneNode> done)
        {
            CancelAction();
            StopAllCoroutines();
            Node = (PrepPhaseNode)node;
            onFinished = done;
            handedOff = false;

            if (rooms != null)
            {
                rooms.SnapMembers();
                foreach (string roomId in Node.RoomIds)
                {
                    Room room = rooms.FindRoom(roomId);
                    if (room != null) room.ResetProps();
                }
            }

            if (!Node.HasPrep)
            {
                // Zero prep (later acts): straight to placement, next frame to avoid re-entering NodePlayer.Enter.
                Countdown.Stop();
                StartCoroutine(HandOffNextFrame());
                return;
            }

            GameEvents.RaiseAlert(Node.AlertTitleKey.Key, Node.AlertBodyKey.Key, Mathf.CeilToInt(Node.Duration).ToString());
            AudioService.PlayCue(Node.AlarmCue);
            Countdown.Start(Node.Duration, Node.PlacementWindowSeconds, Node.TickFromSeconds);
            GameEvents.RaisePrepStarted(Node.Duration);
        }

        public void OnTimeout()
        {
            // Prep nodes are untimed in NodePlayer; the countdown drives the handoff.
        }

        public void Cancel()
        {
            CancelAction();
            StopAllCoroutines();
            if (!handedOff) Countdown.Stop();
            onFinished = null;
        }

        /// <summary>Starts the interactable's timed action if possible (called by PrepInteractable).</summary>
        public void RequestInteract(PrepInteractable target)
        {
            if (!CanInteract || target == null) return;

            if (target.RequiredItem != null && !State.Inventory.Has(target.RequiredItem.Id))
            {
                GameEvents.RaiseMessage(target.MissingRequirementKey.IsEmpty ? UiKeys.PrepNeedsItem : target.MissingRequirementKey.Key);
                return;
            }

            if (target.GrantsItem != null && State.Inventory.IsFull)
            {
                // Bag full: refuse before wasting time.
                GameEvents.RaiseMessage(UiKeys.BagFull);
                return;
            }

            StartAction(target.LabelKey.Key, target.TimeCost, () => target.Apply(State));
        }

        /// <summary>Walks to another prep room (costs RoomTravelSeconds).</summary>
        public void RequestTravel(string roomId)
        {
            if (!CanInteract || rooms == null || !Contains(Node.RoomIds, roomId)) return;
            if (rooms.Current != null && rooms.Current.RoomId == roomId) return;
            StartAction(UiKeys.PrepTravel, Node.RoomTravelSeconds, () => rooms.ShowRoom(roomId));
        }

        private static bool Contains(System.Collections.Generic.IReadOnlyList<string> list, string value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == value) return true;
            }

            return false;
        }

        private void StartAction(string labelKey, float seconds, Action complete)
        {
            ActionLabelKey = labelKey;
            if (seconds <= 0f)
            {
                complete?.Invoke();
                return;
            }

            onActionComplete = complete;
            Action.Begin(seconds);
            InputGate.Block();
            gateHeld = true;
            GameEvents.RaiseActionStarted(labelKey, seconds);
        }

        private void CompleteAction()
        {
            Action complete = onActionComplete;
            onActionComplete = null;
            ReleaseGate();
            GameEvents.RaiseActionFinished(true);
            complete?.Invoke();
        }

        private void CancelAction()
        {
            if (!Action.IsActive && !gateHeld) return;
            Action.Cancel();
            onActionComplete = null;
            ReleaseGate();
            GameEvents.RaiseActionFinished(false);
        }

        private void ReleaseGate()
        {
            if (!gateHeld) return;
            InputGate.Unblock();
            gateHeld = false;
        }

        private void OnPlacementWindow()
        {
            CancelAction();
            handedOff = true;
            GameEvents.RaisePlacementWindowStarted();
            GameEvents.RaiseAlert(Node.PlacementAlertTitleKey.Key, Node.PlacementAlertBodyKey.Key, null);
            AudioService.PlayCue(Node.PlacementAlertCue);
            HandOff();
        }

        private IEnumerator HandOffNextFrame()
        {
            yield return null;
            handedOff = true;
            HandOff();
        }

        private void HandOff()
        {
            Action<SceneNode> done = onFinished;
            onFinished = null;
            done?.Invoke(Node.PlacementNode);
        }

        private void OnSecondTick(int secondsLeft)
        {
            GameEvents.RaisePrepTick(secondsLeft);
            if (Node != null) AudioService.PlayCue(Node.TickCue);
        }

        private void OnDied(DeathInfo info)
        {
            CancelAction();
            Countdown.Stop();
        }

        private void OnRewound(string nodeId)
        {
            if (Node == null || nodeId == Node.Id)
            {
                // Rewinding to the prep node itself: NodePlayer re-enters it and Begin restarts the countdown.
                return;
            }

            if (Node.HasPrep && Node.PlacementNode != null && nodeId == Node.PlacementNode.Id)
            {
                // Micro rewind into the placement: resume the countdown at the window.
                handedOff = true;
                Countdown.Start(Node.Duration, Node.PlacementWindowSeconds, Node.TickFromSeconds);
                Countdown.JumpTo(Node.PlacementWindowSeconds);
                return;
            }

            Countdown.Stop();
        }
    }
}
