using SixtyOne.Core;
using SixtyOne.Text;
using TMPro;
using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>"LOOP #n" counter. Punches when a rewind happens.</summary>
    public sealed class LoopCounterUI : MonoBehaviour
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Counter label.")]
        [SerializeField] private TMP_Text label;

        private void OnEnable()
        {
            GameEvents.Rewound += OnRewound;
            TextDB.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            GameEvents.Rewound -= OnRewound;
            TextDB.Changed -= Refresh;
        }

        private void Start()
        {
            Refresh();
        }

        public void Refresh()
        {
            GameManager gm = gameManager != null ? gameManager : GameManager.Instance;
            if (label != null) label.text = TextDB.Format(UiKeys.Loop, gm != null ? gm.State.LoopCount : 0);
        }

        private void OnRewound(string nodeId)
        {
            Refresh();
            StopAllCoroutines();
            transform.localScale = Vector3.one * 1.4f;
            StartCoroutine(Tween.Scale(transform, Vector3.one, 0.4f, Ease.OutBack, unscaled: true));
        }
    }
}
