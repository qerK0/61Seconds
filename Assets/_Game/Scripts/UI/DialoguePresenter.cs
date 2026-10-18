using System;
using System.Collections.Generic;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// IDialoguePresenter with speech bubbles: lines go on the speaker (SpeakerAnchor), decisions on Mehmet as a
    /// thought, narration on the top card. While a line plays, a full-screen catcher takes the click: click anywhere
    /// / Space / Enter first completes the line, then advances. Keys 1-4 pick rows. Every pick goes through the
    /// panic input delay (instant ClickFeedback ring). Pause, room transitions and the player running hide the
    /// bubble without losing it; rewinds and deaths clear it.
    /// </summary>
    public sealed class DialoguePresenter : MonoBehaviour, IDialoguePresenter, IPointerClickHandler
    {
        [Tooltip("The speech bubble.")]
        [SerializeField] private SpeechBubble bubble;

        [Tooltip("Top narration card.")]
        [SerializeField] private NarrationCard narration;

        [Tooltip("Full-screen transparent image on this object: catches 'click anywhere' while a line plays.")]
        [SerializeField] private Image catcher;

        [Tooltip("Building timer state for the line-cost chip colour. Optional.")]
        [SerializeField] private SurvivalController survival;

        [Tooltip("Mehmet's motor: his thought bubble hides while he runs. Optional.")]
        [SerializeField] private SixtyOne.Placement.CharacterMotor runner;

        [Tooltip("Cost chip colours: normal / pulse / alarm (same as the building timer).")]
        [SerializeField] private Color[] costColors =
        {
            new Color(0.298f, 1f, 0.416f, 1f), new Color(1f, 0.69f, 0.125f, 1f), new Color(1f, 0.231f, 0.188f, 1f)
        };

        [Tooltip("Accent when a speaker has no anchor.")]
        [SerializeField] private Color fallbackAccent = new Color(0.2f, 0.2f, 0.2f, 1f);

        private Action onAdvance;
        private bool picked;
        private bool inTransition;

        private void Awake()
        {
            SetCatcher(false);
        }

        private void OnEnable()
        {
            NavigationEvents.OnTransitionStart += OnTransitionStart;
            NavigationEvents.OnTransitionComplete += OnTransitionComplete;
            GameEvents.Died += OnDied;
        }

        private void OnDisable()
        {
            NavigationEvents.OnTransitionStart -= OnTransitionStart;
            NavigationEvents.OnTransitionComplete -= OnTransitionComplete;
            GameEvents.Died -= OnDied;
        }

        public void ShowLine(string speakerKey, string text, float timeCost, Action onContinue, IReadOnlyList<ChoiceView> pills, Action<int> onChosen)
        {
            picked = false;
            onAdvance = onContinue;
            SpeakerAnchor anchor = SpeakerAnchor.Find(speakerKey);
            Color accent = anchor != null ? anchor.Accent : fallbackAccent;
            string name = string.IsNullOrEmpty(speakerKey) ? string.Empty : TextDB.GetDisplay(speakerKey);
            if (narration != null) narration.Hide();
            bubble.Present(speakerKey, name, accent, false, text, true, timeCost, CostColor(), onContinue != null);
            bubble.SetPills(pills, accent, i => Pick(() => onChosen?.Invoke(i)));
            SetCatcher(onContinue != null);
        }

        public void ShowNarration(string text, bool persistent = false)
        {
            onAdvance = null;
            SetCatcher(false);
            bubble.Hide();
            if (narration != null) narration.Show(text, persistent);
        }

        public void ShowThought(string text, IReadOnlyList<ChoiceView> choices, Action<int> onChosen)
        {
            picked = false;
            onAdvance = null;
            SetCatcher(false);
            if (narration != null) narration.Hide();
            SpeakerAnchor me = SpeakerAnchor.Find(SpeakerAnchor.PlayerKey);
            Color accent = me != null ? me.Accent : fallbackAccent;
            bubble.Present(SpeakerAnchor.PlayerKey, null, accent, true, string.IsNullOrEmpty(text) ? "…" : text, false, 0f, CostColor(), false);
            bubble.SetRows(choices, accent, i => Pick(() => onChosen?.Invoke(i)));
        }

        public void ShowChoices(string speakerKey, IReadOnlyList<ChoiceView> choices, Action<int> onChosen)
        {
            picked = false;
            onAdvance = null;
            SetCatcher(false);
            SpeakerAnchor anchor = SpeakerAnchor.Find(speakerKey);
            bubble.StopWaiting();
            bubble.CompleteTyping();
            bubble.SetRows(choices, anchor != null ? anchor.Accent : fallbackAccent, i => Pick(() => onChosen?.Invoke(i)));
        }

        public void Clear()
        {
            onAdvance = null;
            SetCatcher(false);
            bubble.Hide();
            if (narration != null) narration.Hide();
        }

        public void OnPointerClick(PointerEventData eventData) => Advance();

        private void Advance()
        {
            if (PauseState.IsPaused || inTransition || InputGate.IsBlocked) return;
            if (bubble.IsTyping)
            {
                bubble.CompleteTyping();
                return;
            }

            if (onAdvance == null) return;
            Action next = onAdvance;
            onAdvance = null;
            SetCatcher(false);
            Pick(next);
        }

        /// <summary>One pick per presentation, through the panic input delay (with the instant feedback ring).</summary>
        private void Pick(Action action)
        {
            if (picked || InputGate.IsBlocked || action == null) return;
            picked = true;
            Pointer pointer = Pointer.current;
            Vector2 position = pointer != null ? pointer.position.ReadValue() : Vector2.zero;
            InputDelayRouter.Run(() =>
            {
                picked = false;
                action();
            }, position);
        }

        private void Update()
        {
            bool running = bubble.IsThought && runner != null && runner.IsMoving;
            bubble.SetSuppressed(PauseState.IsPaused || inTransition || running);
            if (PauseState.IsPaused || inTransition || InputGate.IsBlocked) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (onAdvance != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                Advance();
                return;
            }

            if (!bubble.IsVisible || running) return;
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) bubble.PressRow(1);
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) bubble.PressRow(2);
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) bubble.PressRow(3);
            else if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) bubble.PressRow(4);
        }

        private Color CostColor()
        {
            if (survival == null || survival.Config == null || costColors.Length < 3) return costColors.Length > 0 ? costColors[0] : Color.green;
            GameManager gm = GameManager.Instance;
            float time = gm != null ? gm.State.BuildingTime : float.MaxValue;
            if (time <= survival.Config.AlarmAtSeconds) return costColors[2];
            return time <= survival.Config.PulseAtSeconds ? costColors[1] : costColors[0];
        }

        private void SetCatcher(bool on)
        {
            if (catcher != null) catcher.raycastTarget = on;
        }

        private void OnTransitionStart(string from, string to) => inTransition = true;

        private void OnTransitionComplete(string room) => inTransition = false;

        private void OnDied(DeathInfo info) => Clear();
    }
}
