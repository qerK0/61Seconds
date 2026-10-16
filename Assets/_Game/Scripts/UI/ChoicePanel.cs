using System;
using System.Collections.Generic;
using SixtyOne.Nodes;
using SixtyOne.Text;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>Shows a node's title/body and builds choice buttons from a template.</summary>
    public sealed class ChoicePanel : MonoBehaviour, IChoicePresenter
    {
        [Tooltip("Panel root group.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Node title.")]
        [SerializeField] private TMP_Text titleText;

        [Tooltip("Node body.")]
        [SerializeField] private TMP_Text bodyText;

        [Tooltip("Parent for spawned buttons (has a layout group).")]
        [SerializeField] private RectTransform buttonContainer;

        [Tooltip("Inactive template button cloned per choice.")]
        [SerializeField] private ChoiceButton buttonTemplate;

        private readonly List<ChoiceButton> spawned = new List<ChoiceButton>();
        private SceneNode shownNode;
        private bool picked;

        private void Awake()
        {
            if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);
            if (shownNode == null) UIVisibility.Set(group, false);
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
            UIVisibility.Set(group, true);
            RefreshText();
            ClearButtons();

            AddChoices(choices, onChosen);
        }

        public void PresentText(string title, string body, string continueLabel, Action onContinue, IReadOnlyList<ChoiceView> choices, Action<int> onChosen)
        {
            shownNode = null;
            picked = false;
            UIVisibility.Set(group, true);
            if (titleText != null) titleText.text = title ?? string.Empty;
            if (bodyText != null) bodyText.text = body ?? string.Empty;
            ClearButtons();

            if (continueLabel != null && buttonTemplate != null)
            {
                ChoiceButton button = NewButton();
                button.BindCustom(continueLabel, () => Click(() => onContinue?.Invoke()));
            }

            AddChoices(choices, onChosen);
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

        public void Clear()
        {
            ClearButtons();
            shownNode = null;
            UIVisibility.Set(group, false);
        }

        private void RefreshText()
        {
            if (shownNode == null) return;
            if (titleText != null) titleText.text = shownNode.TitleKey.IsEmpty ? string.Empty : shownNode.TitleKey.Display;
            if (bodyText != null) bodyText.text = shownNode.BodyKey.IsEmpty ? string.Empty : shownNode.BodyKey.Display;
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
