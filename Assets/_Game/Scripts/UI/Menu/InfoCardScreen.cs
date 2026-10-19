using System.Collections;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SixtyOne.UI
{
    /// <summary>
    /// The "loading screen" between floors: a full-screen "Deprem Bilgisi" card shown on F4 → F3, F3 → F2, F2 → street
    /// and on every macro rewind (a checkpoint restart). The card is picked by InfoCardRules (preferred, else the next
    /// unseen; seen cards persist in InfoCardStore). Holds every timer (ScreenHold) while up; a progress bar fills over
    /// autoSeconds and then it continues by itself; a click / Space / Enter continue early after the guard. Unscaled time.
    /// </summary>
    public sealed class InfoCardScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private NodePlayer nodePlayer;

        [Header("Text")]
        [SerializeField] private TMP_Text floorLabel;
        [SerializeField] private TMP_Text counter;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private TMP_Text gameLine;

        [Tooltip("Progress bar fill (pivot on the left); its x scale grows 0 → 1 over autoSeconds.")]
        [SerializeField] private RectTransform progress;

        [Header("Timing (seconds)")]
        [SerializeField] private float autoSeconds = 6f;
        [SerializeField] private float guardSeconds = 1.5f;
        [SerializeField] private float fadeSeconds = 0.3f;

        [Tooltip("Played when a card appears.")]
        [SerializeField] private AudioCue showCue;

        private readonly RewindCardGate rewindGate = new RewindCardGate();
        private int lastStage;
        private bool showing;
        private bool holding;

        /// <summary>True while a card is up.</summary>
        public bool IsShowing => showing;

        private void Awake()
        {
            SetVisible(0f, false);
        }

        private void OnEnable()
        {
            GameEvents.NodeEntered += OnNodeEntered;
            GameEvents.Rewound += OnRewound;
        }

        private void OnDisable()
        {
            GameEvents.NodeEntered -= OnNodeEntered;
            GameEvents.Rewound -= OnRewound;
            ReleaseHold();
        }

        private void OnNodeEntered(string nodeId)
        {
            SceneNode node = nodePlayer != null ? nodePlayer.Current : null;
            if (node == null) return;
            int stage = InfoCardRules.StageFor(node.Act, node.RoomId);
            // Rewinds never raise the stage, so for them this only resets the baseline.
            if (InfoCardRules.IsTransition(lastStage, stage)) Show(stage, InfoCardRules.PreferredForStage(stage));
            lastStage = stage;
        }

        private void OnRewound(string nodeId)
        {
            GameSession session = GameManager.Instance != null ? GameManager.Instance.Session : null;
            if (session == null || !session.LastRewindWasMacro) return;
            // One card per checkpoint and death cause: dying the same way again never brings a card back.
            string cause = session.LastDeath != null ? session.LastDeath.TipId : null;
            if (!rewindGate.Allow(session.Checkpoint != null ? session.Checkpoint.NodeId : nodeId, cause)) return;
            SceneNode node = nodePlayer != null ? nodePlayer.Current : null;
            int stage = node != null ? InfoCardRules.StageFor(node.Act, node.RoomId) : lastStage;
            Show(stage, InfoCardRules.PreferredForDeath(cause));
        }

        /// <summary>Shows the card for arriving on a stage (1..4); preferred 0 = the next unseen.</summary>
        public void Show(int stage, int preferred)
        {
            if (showing) return;
            int card = InfoCardRules.Pick(preferred, InfoCardStore.Load(), InfoCardStore.LastShown);
            InfoCardStore.MarkSeen(card);
            Fill(stage, card);
            showing = true;
            ScreenHold.Begin();
            holding = true;
            StartCoroutine(Play());
        }

        private void Fill(int stage, int card)
        {
            if (floorLabel != null) floorLabel.text = TextDB.GetDisplay(UiKeys.FloorLabel(stage));
            if (counter != null) counter.text = TextDB.Format(UiKeys.InfoCounter, card, InfoCardRules.CardCount);
            if (title != null) title.text = TextDB.GetDisplay(UiKeys.InfoTitle(card));
            if (body != null) body.text = TextDB.GetDisplay(UiKeys.InfoBody(card));
            if (gameLine != null) gameLine.text = TextDB.Format(UiKeys.InfoGamePrefix, TextDB.GetDisplay(UiKeys.InfoGame(card)));
        }

        private IEnumerator Play()
        {
            AudioService.PlayUiCue(showCue);
            SetProgress(0f);
            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                SetVisible(t / fadeSeconds, true);
                yield return null;
            }

            SetVisible(1f, true);
            bool early = false;
            float shown = 0f;
            while (shown < autoSeconds && !early)
            {
                // ESC is blocked while the card is up; stay safe if something else paused.
                if (!PauseState.IsPaused) shown += Time.unscaledDeltaTime;
                SetProgress(shown / autoSeconds);
                if (shown >= guardSeconds && Pressed()) early = true;
                yield return null;
            }

            for (float t = 0f; t < fadeSeconds; t += Time.unscaledDeltaTime)
            {
                SetVisible(1f - t / fadeSeconds, true);
                yield return null;
            }

            SetVisible(0f, false);
            ReleaseHold();
            showing = false;
        }

        private static bool Pressed()
        {
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            return (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
                || (m != null && m.leftButton.wasPressedThisFrame);
        }

        private void SetProgress(float fraction)
        {
            if (progress != null) progress.localScale = new Vector3(Mathf.Clamp01(fraction), 1f, 1f);
        }

        private void ReleaseHold()
        {
            if (holding) ScreenHold.End();
            holding = false;
        }

        private void SetVisible(float alpha, bool blocking)
        {
            if (root == null) return;
            root.alpha = alpha;
            root.interactable = false;
            root.blocksRaycasts = blocking;
        }
    }
}
