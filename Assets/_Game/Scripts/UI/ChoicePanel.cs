using System;
using System.Collections.Generic;
using SixtyOne.Nodes;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SixtyOne.UI
{
    /// <summary>
    /// Bottom overlay: a node's title/body (typewriter) and choice buttons cloned from a template. It slides up
    /// when it has something to show and away when cleared. With no buttons (rooms are left through doors) it
    /// sinks to a title-only peek strip after a reading delay; clicking the strip opens it again. Clicking the
    /// panel while text types completes it (also the first click on "Devam").
    /// </summary>
    public sealed class ChoicePanel : MonoBehaviour, IChoicePresenter, IPointerClickHandler
    {
        private enum Mode { Hidden, Peek, Full }

        [Tooltip("Panel root group.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("The sliding panel (bottom pivot, anchored to the bottom edge).")]
        [SerializeField] private RectTransform panel;

        [Tooltip("Node title / speaker name.")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("Node body / dialogue line.")]
        [SerializeField] private TMP_Text bodyText;

        [Tooltip("Typewriter on the body. Optional.")]
        [SerializeField] private TypewriterText typewriter;

        [Tooltip("Parent for spawned buttons (has a layout group).")]
        [SerializeField] private RectTransform buttonContainer;

        [Tooltip("Inactive template button cloned per choice.")]
        [SerializeField] private ChoiceButton buttonTemplate;

        [Tooltip("Visible height (px) of the peek strip.")]
        [SerializeField] private float peekHeight = 64f;

        [Tooltip("Gap (px) between the screen bottom and the open panel.")]
        [SerializeField] private float bottomMargin = 16f;

        [Tooltip("Slide time in seconds (unscaled).")]
        [SerializeField] private float slideSeconds = 0.22f;

        [Tooltip("Seconds before a panel without buttons sinks to the peek strip (plus reading time).")]
        [SerializeField] private float peekDelay = 3f;

        [Tooltip("The player's motor: node choices sink to the peek strip while running. Optional.")]
        [SerializeField] private SixtyOne.Placement.CharacterMotor runner;

        [Tooltip("Panel height (px) for title + body alone; each row of buttons adds rowHeight.")]
        [SerializeField] private float baseHeight = 168f;

        [Tooltip("Height (px) of one row of choice buttons, spacing included.")]
        [SerializeField] private float rowHeight = 66f;

        [Tooltip("Buttons per row (matches the grid layout).")]
        [Min(1)]
        [SerializeField] private int columns = 2;

        private readonly List<ChoiceButton> spawned = new List<ChoiceButton>();
        private SceneNode shownNode;
        private bool picked;
        private Mode mode = Mode.Hidden;
        private float peekAt = -1f;
        private float slideVelocity;
        private bool sunkWhileRunning;
        private float bodyMinHeight;
        private float containerTop;

        [Tooltip("The body never grows past this height (px); longer text auto-sizes down.")]
        [SerializeField] private float bodyMaxHeight = 420f;

        private void Awake()
        {
            if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);
            if (shownNode == null) SetMode(Mode.Hidden, true);
        }

        private void OnEnable()
        {
            TextDB.Changed += RefreshText;
        }

        private void OnDisable()
        {
            TextDB.Changed -= RefreshText;
        }

        public void Present(SceneNode node, IReadOnlyList<ChoiceView> choices, Action<int> onChosen)
        {
            shownNode = node;
            picked = false;
            ClearButtons();
            RefreshText();
            AddChoices(choices, onChosen);
            Open();
        }

        public void PresentText(string title, string body, string continueLabel, Action onContinue, IReadOnlyList<ChoiceView> choices, Action<int> onChosen)
        {
            shownNode = null;
            picked = false;
            if (titleText != null) titleText.text = title ?? string.Empty;
            SetBody(body, true);
            ClearButtons();

            if (continueLabel != null && buttonTemplate != null)
            {
                ChoiceButton button = NewButton();
                button.BindCustom(continueLabel, () =>
                {
                    // First click while the line is still typing just completes it.
                    if (typewriter != null && typewriter.IsRevealing)
                    {
                        typewriter.Complete();
                        return;
                    }

                    Click(() => onContinue?.Invoke());
                });
            }

            AddChoices(choices, onChosen);
            Open();
        }

        public void Clear()
        {
            ClearButtons();
            shownNode = null;
            SetMode(Mode.Hidden, false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (typewriter != null && typewriter.IsRevealing) typewriter.Complete();
            if (mode == Mode.Peek) Open();
        }

        private void Open()
        {
            // Fit the content: a text-only panel stays short so it covers as little of the room as possible.
            if (panel != null)
            {
                int rows = (spawned.Count + columns - 1) / columns;
                float extraBody = FitBody();
                panel.sizeDelta = new Vector2(panel.sizeDelta.x, baseHeight + extraBody + rows * rowHeight + (rows > 0 ? 8f : 0f));
            }

            SetMode(Mode.Full, false);
            int length = bodyText != null ? bodyText.text.Length : 0;
            peekAt = spawned.Count == 0 ? Time.unscaledTime + peekDelay + length / 18f : -1f;
        }

        /// <summary>
        /// Grows the body (top-anchored, authored at bodyMinHeight) to its text and moves the buttons below it.
        /// Returns the extra height added to the panel.
        /// </summary>
        private float FitBody()
        {
            if (bodyText == null || buttonContainer == null) return 0f;
            RectTransform rect = bodyText.rectTransform;
            if (bodyMinHeight <= 0f)
            {
                bodyMinHeight = rect.sizeDelta.y;
                containerTop = buttonContainer.offsetMax.y;
            }

            float width = Mathf.Max(100f, rect.rect.width);
            float preferred = bodyText.GetPreferredValues(bodyText.text, width, 0f).y + 6f;
            float height = Mathf.Clamp(preferred, bodyMinHeight, bodyMaxHeight);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
            float extra = height - bodyMinHeight;
            buttonContainer.offsetMax = new Vector2(buttonContainer.offsetMax.x, containerTop - extra);
            return extra;
        }

        private void Update()
        {
            if (peekAt > 0f && mode == Mode.Full && Time.unscaledTime >= peekAt)
            {
                peekAt = -1f;
                SetMode(Mode.Peek, false);
            }

            // Node choices are not needed while the player runs: sink to the strip, come back on arrival.
            bool running = runner != null && runner.IsMoving && shownNode != null;
            if (running && mode == Mode.Full)
            {
                SetMode(Mode.Peek, false);
                sunkWhileRunning = true;
            }
            else if (!running && sunkWhileRunning)
            {
                sunkWhileRunning = false;
                if (mode == Mode.Peek && spawned.Count > 0) SetMode(Mode.Full, false);
            }

            if (panel == null) return;
            Vector2 position = panel.anchoredPosition;
            float target = TargetY(mode);
            position.y = Mathf.SmoothDamp(position.y, target, ref slideVelocity, Mathf.Max(0.01f, slideSeconds * 0.5f), Mathf.Infinity, Time.unscaledDeltaTime);
            panel.anchoredPosition = position;
            if (mode == Mode.Hidden && group != null && Mathf.Abs(position.y - target) < 2f) group.alpha = 0f;
        }

        private float TargetY(Mode m)
        {
            float height = panel != null ? panel.rect.height : 300f;
            switch (m)
            {
                case Mode.Full: return bottomMargin;
                case Mode.Peek: return -(height - peekHeight);
                default: return -(height + 40f);
            }
        }

        private void SetMode(Mode next, bool instant)
        {
            mode = next;
            bool visible = next != Mode.Hidden;
            if (group != null)
            {
                if (visible) group.alpha = 1f;
                group.interactable = next == Mode.Full;
                group.blocksRaycasts = visible;
            }

            if (instant && panel != null)
            {
                Vector2 position = panel.anchoredPosition;
                position.y = TargetY(next);
                panel.anchoredPosition = position;
                slideVelocity = 0f;
                if (!visible && group != null) group.alpha = 0f;
            }
        }

        private void AddChoices(IReadOnlyList<ChoiceView> choices, Action<int> onChosen)
        {
            if (choices == null || buttonTemplate == null) return;
            foreach (ChoiceView view in choices)
            {
                int index = view.Index;
                NewButton().Bind(view, () => Click(() => onChosen?.Invoke(index)));
            }
        }

        private ChoiceButton NewButton()
        {
            ChoiceButton button = Instantiate(buttonTemplate, buttonContainer);
            button.gameObject.SetActive(true);
            spawned.Add(button);
            return button;
        }

        /// <summary>One click per presentation, routed through the panic input delay (with instant feedback).</summary>
        private void Click(Action action)
        {
            if (picked || SixtyOne.Core.InputGate.IsBlocked) return;
            picked = true;
            UnityEngine.InputSystem.Pointer pointer = UnityEngine.InputSystem.Pointer.current;
            Vector2 position = pointer != null ? pointer.position.ReadValue() : Vector2.zero;
            SixtyOne.Core.InputDelayRouter.Run(action, position);
        }

        private void RefreshText()
        {
            if (shownNode == null) return;
            if (titleText != null) titleText.text = shownNode.TitleKey.IsEmpty ? string.Empty : shownNode.TitleKey.Display;
            SetBody(shownNode.BodyKey.IsEmpty ? string.Empty : shownNode.BodyKey.Display, false);
        }

        private void SetBody(string text, bool reveal)
        {
            if (typewriter != null) typewriter.Play(text, reveal);
            else if (bodyText != null) bodyText.text = text ?? string.Empty;
        }

        private void ClearButtons()
        {
            foreach (ChoiceButton button in spawned)
            {
                if (button != null) Destroy(button.gameObject);
            }

            spawned.Clear();
        }
    }
}
