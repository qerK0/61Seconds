using System.Collections;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Black fade, one-line death text, the Deprem Notu and a rewind button.
    /// The wife's death is a hard cut to black (tone rule).
    /// </summary>
    public sealed class DeathCard : MonoBehaviour
    {
        [Tooltip("State owner (Rewind).")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Tip registry.")]
        [SerializeField] private GameContent content;

        [Tooltip("Full-screen black group.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("Death line.")]
        [SerializeField] private TMP_Text deathText;

        [Tooltip("Container of the tip block.")]
        [SerializeField] private GameObject tipRoot;

        [Tooltip("'Deprem Notu' header.")]
        [SerializeField] private TMP_Text tipHeader;

        [Tooltip("Tip title + text.")]
        [SerializeField] private TMP_Text tipText;

        [Tooltip("Rewind button.")]
        [SerializeField] private Button rewindButton;

        [Tooltip("Rewind button label.")]
        [SerializeField] private TMP_Text rewindLabel;

        [Tooltip("Fade-in seconds (not used for the wife: hard cut).")]
        [SerializeField] private float fadeSeconds = 0.6f;

        [Tooltip("Delay before the rewind button appears.")]
        [SerializeField] private float buttonDelay = 0.8f;

        private void Awake()
        {
            UIVisibility.Set(group, false);
            if (rewindButton != null) rewindButton.onClick.AddListener(OnRewindClicked);
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
        }

        private void OnDied(DeathInfo info)
        {
            StopAllCoroutines();
            StartCoroutine(Show(info));
        }

        private IEnumerator Show(DeathInfo info)
        {
            if (deathText != null) deathText.text = string.IsNullOrEmpty(info.TextKey) ? string.Empty : TextDB.GetDisplay(info.TextKey);
            if (rewindLabel != null) rewindLabel.text = TextDB.GetDisplay(UiKeys.Rewind);

            Tip tip = content != null && !string.IsNullOrEmpty(info.TipId) ? content.FindTip(info.TipId) : null;
            if (tipRoot != null) tipRoot.SetActive(tip != null);
            if (tip != null)
            {
                if (tipHeader != null) tipHeader.text = TextDB.GetDisplay(UiKeys.TipHeader);
                if (tipText != null) tipText.text = $"<b>{tip.TitleKey.Display}</b>\n{tip.TextKey.Display}";
            }

            if (rewindButton != null) rewindButton.gameObject.SetActive(false);
            group.alpha = 0f;
            group.blocksRaycasts = true;
            group.interactable = true;

            if (info.Victim == MemberType.Wife)
            {
                group.alpha = 1f;
            }
            else
            {
                yield return Tween.Fade(group, 1f, fadeSeconds);
            }

            yield return new WaitForSecondsRealtime(buttonDelay);
            if (rewindButton != null) rewindButton.gameObject.SetActive(true);
        }

        private void OnRewindClicked()
        {
            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            if (gm != null) gm.Rewind();
        }

        private void OnRewound(string nodeId)
        {
            StopAllCoroutines();
            UIVisibility.Set(group, false);
        }
    }
}
