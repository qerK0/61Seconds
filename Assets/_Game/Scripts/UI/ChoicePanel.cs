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

            if (choices == null || buttonTemplate == null) return;
            foreach (ChoiceView view in choices)
            {
                ChoiceButton button = Instantiate(buttonTemplate, buttonContainer);
                button.gameObject.SetActive(true);
                int index = view.Index;
                button.Bind(view, () =>
                {
                    if (picked || SixtyOne.Core.InputGate.IsBlocked) return;
                    picked = true;
                    onChosen?.Invoke(index);
                });
                spawned.Add(button);
            }
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
