using System;
using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Nodes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// The one world-anchored speech bubble (screen-space overlay): warm paper card with a dark outline, a name tab
    /// in the speaker's colour, a typewritten line, a bobbing ▼ when complete, a "−4 sn" LED cost chip and a tail
    /// aimed at the speaker's head. Choice rows (or the "Kısa kes" pill) hang under it. Thought style (Mehmet's
    /// decisions): grey card, dotted tail, no name tab. It follows the head every LateUpdate and stays inside the
    /// safe area (BubbleLayout). Pops in (0.9 → 1, fade) over 0.15 s.
    /// </summary>
    public sealed class SpeechBubble : MonoBehaviour
    {
        [Header("Structure")]
        [Tooltip("Block root (bubble + rows), bottom-left pivot, positioned in the parent's space.")]
        [SerializeField] private RectTransform block;

        [Tooltip("Fades the whole block.")]
        [SerializeField] private CanvasGroup group;

        [Tooltip("The bubble card (top part of the block).")]
        [SerializeField] private RectTransform bubble;

        [Tooltip("Shadow + outline layer under the tail; follows the card.")]
        [SerializeField] private RectTransform bubbleBack;

        [Tooltip("Card fill (paper / thought grey).")]
        [SerializeField] private Image fill;

        [Tooltip("Name tab on the top-left edge.")]
        [SerializeField] private RectTransform nameTab;

        [Tooltip("Name tab label.")]
        [SerializeField] private TMP_Text nameLabel;

        [Tooltip("The line.")]
        [SerializeField] private TMP_Text body;

        [Tooltip("Typewriter on the line (voice blips listen to it).")]
        [SerializeField] private TypewriterText typewriter;

        [Tooltip("▼ continue mark (bottom-right inside the card).")]
        [SerializeField] private RectTransform continueMark;

        [Tooltip("LED cost chip hanging off the bottom-right corner.")]
        [SerializeField] private RectTransform costChip;

        [Tooltip("Cost chip label.")]
        [SerializeField] private TMP_Text costLabel;

        [Tooltip("Speech tail: dark outline triangle and paper fill triangle (pivot at the base, pointing +x).")]
        [SerializeField] private Image tailOutline;

        [Tooltip("Speech tail fill.")]
        [SerializeField] private Image tailFill;

        [Tooltip("Thought tail dots (big to small).")]
        [SerializeField] private Image[] thoughtDots = new Image[0];

        [Tooltip("Rows container under the card.")]
        [SerializeField] private RectTransform rows;

        [Tooltip("Inactive choice row template.")]
        [SerializeField] private BubbleChoiceRow rowTemplate;

        [Tooltip("Inactive pill template (during-dialogue choices).")]
        [SerializeField] private BubbleChoiceRow pillTemplate;

        [Header("Look")]
        [Tooltip("Speech card colour (#F7F3EA).")]
        [SerializeField] private Color paper = new Color(0.969f, 0.953f, 0.918f, 1f);

        [Tooltip("Thought card colour.")]
        [SerializeField] private Color thoughtPaper = new Color(0.87f, 0.86f, 0.84f, 1f);

        [Tooltip("Max line width (px), min card width (px), padding (x, y).")]
        [SerializeField] private Vector4 sizes = new Vector4(380f, 150f, 18f, 14f);

        [Tooltip("Min rows width (px) and gap between rows (px).")]
        [SerializeField] private Vector2 rowSizes = new Vector2(360f, 6f);

        [Tooltip("Gap from the head: x sideways, y up (px).")]
        [SerializeField] private Vector2 headOffset = new Vector2(22f, 18f);

        [Tooltip("Safe-area margin (px).")]
        [SerializeField] private float margin = 16f;

        [Tooltip("Bob height (px) and frequency (Hz) of the ▼ mark.")]
        [SerializeField] private Vector2 bob = new Vector2(3f, 1.4f);

        [Tooltip("Pop-in seconds.")]
        [SerializeField] private float popSeconds = 0.15f;

        [Header("Safe area")]
        [Tooltip("Bubbles stay below this rect's bottom (the panic meter under the timer).")]
        [SerializeField] private RectTransform topLimit;

        [Tooltip("Bubbles stay left of this rect's left edge (the bag column).")]
        [SerializeField] private RectTransform rightLimit;

        [Tooltip("World camera (Camera.main if empty).")]
        [SerializeField] private Camera worldCamera;

        private readonly List<BubbleChoiceRow> spawned = new List<BubbleChoiceRow>();
        private SpeakerAnchor anchor;
        private string speakerKey;
        private bool visible;
        private bool hiddenExternally;
        private bool thought;
        private bool waitingAdvance;
        private float popT = 1f;
        private float rowsHeight;
        private float rowsWidth;
        private Vector2 bubbleSize;
        private string shownText;
        private string shownName;
        private Action pendingRows;

        private static Sprite triangle;

        public TypewriterText Typewriter => typewriter;

        public bool IsVisible => visible;

        public bool IsTyping => typewriter != null && typewriter.IsRevealing;

        public bool IsThought => thought;

        private void Awake()
        {
            if (rowTemplate != null) rowTemplate.gameObject.SetActive(false);
            if (pillTemplate != null) pillTemplate.gameObject.SetActive(false);
            Sprite tri = Triangle();
            if (tailOutline != null) tailOutline.sprite = tri;
            if (tailFill != null) tailFill.sprite = tri;
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }

            if (typewriter != null) typewriter.RevealEnded += OnRevealEnded;
        }

        private void OnDestroy()
        {
            if (typewriter != null) typewriter.RevealEnded -= OnRevealEnded;
        }

        /// <summary>
        /// Shows text on a speaker (null anchor = docked bottom-centre). Same speaker + text = kept as is (choice
        /// refreshes do not retype). cost &gt; 0 shows the chip in costColor.
        /// </summary>
        public void Present(string key, string speakerName, Color accent, bool asThought, string text, bool typewrite, float cost, Color costColor, bool awaitAdvance)
        {
            speakerKey = key;
            anchor = SpeakerAnchor.Find(key);
            waitingAdvance = awaitAdvance;
            bool same = visible && text == shownText && speakerName == shownName && asThought == thought;
            thought = asThought;
            shownName = speakerName;
            if (fill != null) fill.color = asThought ? thoughtPaper : paper;
            if (nameTab != null) nameTab.gameObject.SetActive(!asThought && !string.IsNullOrEmpty(speakerName));
            if (nameLabel != null)
            {
                nameLabel.text = (speakerName ?? string.Empty).ToUpperInvariant();
                nameLabel.color = anchor != null ? anchor.Accent : accent;
            }

            if (costChip != null) costChip.gameObject.SetActive(cost > 0f);
            if (costLabel != null && cost > 0f)
            {
                costLabel.text = SixtyOne.Text.TextDB.Format(SixtyOne.Text.UiKeys.TimeSpent, Mathf.CeilToInt(cost));
                costLabel.color = costColor;
            }

            if (tailOutline != null) tailOutline.enabled = !asThought;
            if (tailFill != null)
            {
                tailFill.enabled = !asThought;
                tailFill.color = paper;
            }

            foreach (Image dot in thoughtDots)
            {
                if (dot == null) continue;
                dot.enabled = asThought;
                dot.color = thoughtPaper;
            }

            if (same) return;
            shownText = text;
            ClearRows();
            if (typewriter != null) typewriter.Play(text, typewrite);
            else if (body != null) body.text = text;
            FitBubble();
            if (!visible) popT = 0f;
            visible = true;
        }

        /// <summary>Choice rows under the card; they appear once the line has finished typing (0.1 s stagger).</summary>
        public void SetRows(IReadOnlyList<ChoiceView> choices, Color accent, Action<int> onChosen)
        {
            ClearRows();
            if (choices == null || choices.Count == 0 || rowTemplate == null) return;
            void Build()
            {
                int number = 1;
                foreach (ChoiceView view in choices)
                {
                    int index = view.Index;
                    BubbleChoiceRow row = Spawn(rowTemplate);
                    row.Bind(view, number, accent, (number - 1) * 0.1f, () => onChosen?.Invoke(index));
                    number++;
                }

                LayoutRows(false);
            }

            if (IsTyping) pendingRows = Build;
            else Build();
        }

        /// <summary>During-dialogue choices as small pills under the card (shown at once).</summary>
        public void SetPills(IReadOnlyList<ChoiceView> choices, Color accent, Action<int> onChosen)
        {
            ClearRows();
            if (choices == null || choices.Count == 0 || pillTemplate == null) return;
            foreach (ChoiceView view in choices)
            {
                int index = view.Index;
                Spawn(pillTemplate).BindPill(view, accent, () => onChosen?.Invoke(index));
            }

            LayoutRows(true);
        }

        /// <summary>Plays row n (1-based, keyboard 1-4). Returns false if there is no such clickable row.</summary>
        public bool PressRow(int number)
        {
            int n = 0;
            foreach (BubbleChoiceRow row in spawned)
            {
                if (row == null || row.ChoiceIndex < 0 || row.gameObject.name.StartsWith("Pill")) continue;
                n++;
                if (n == number)
                {
                    if (!row.Clickable) return false;
                    row.Press();
                    return true;
                }
            }

            return false;
        }

        /// <summary>The line stops being an advance target (rows take over); its cost is paid, so the chip goes.</summary>
        public void StopWaiting()
        {
            waitingAdvance = false;
            if (costChip != null) costChip.gameObject.SetActive(false);
        }

        public void CompleteTyping()
        {
            if (typewriter != null && typewriter.IsRevealing) typewriter.Complete();
        }

        public void Hide()
        {
            visible = false;
            waitingAdvance = false;
            shownText = null;
            shownName = null;
            pendingRows = null;
            anchor = null;
            speakerKey = null;
            ClearRows();
            if (typewriter != null) typewriter.Complete();
        }

        /// <summary>Hidden for a moment (pause, room transition, the player running) without losing the content.</summary>
        public void SetSuppressed(bool on)
        {
            if (on == hiddenExternally) return;
            hiddenExternally = on;
            if (!on && visible) popT = 0f;
        }

        private void OnRevealEnded()
        {
            if (pendingRows == null) return;
            Action build = pendingRows;
            pendingRows = null;
            build();
        }

        private BubbleChoiceRow Spawn(BubbleChoiceRow template)
        {
            BubbleChoiceRow row = Instantiate(template, rows);
            row.name = template == pillTemplate ? "Pill" : "Row";
            row.gameObject.SetActive(true);
            spawned.Add(row);
            return row;
        }

        private void ClearRows()
        {
            pendingRows = null;
            foreach (BubbleChoiceRow row in spawned)
            {
                if (row != null) Destroy(row.gameObject);
            }

            spawned.Clear();
            rowsHeight = 0f;
            rowsWidth = 0f;
        }

        private void FitBubble()
        {
            if (body == null || bubble == null) return;
            float maxLine = sizes.x;
            Vector2 one = body.GetPreferredValues(body.text, 99999f, 0f);
            float lineWidth = Mathf.Min(maxLine, one.x + 2f);
            float height = body.GetPreferredValues(body.text, lineWidth, 0f).y;
            float width = Mathf.Max(sizes.y, lineWidth + sizes.z * 2f);
            if (nameLabel != null && nameTab != null && nameTab.gameObject.activeSelf)
            {
                width = Mathf.Max(width, nameLabel.GetPreferredValues(nameLabel.text).x + 40f);
            }

            // Room for the ▼ under the last line.
            bubbleSize = new Vector2(width, height + sizes.w * 2f + 10f);
            body.rectTransform.sizeDelta = new Vector2(lineWidth, height);
        }

        private void LayoutRows(bool pills)
        {
            float y = 0f;
            float width = pills ? 0f : Mathf.Max(rowSizes.x, bubbleSize.x);
            foreach (BubbleChoiceRow row in spawned)
            {
                var rect = (RectTransform)row.transform;
                if (pills)
                {
                    TMP_Text text = row.GetComponentInChildren<TMP_Text>();
                    float w = text != null ? text.GetPreferredValues(text.text).x + 64f : 160f;
                    rect.sizeDelta = new Vector2(w, row.Height);
                    width = Mathf.Max(width, w);
                }
                else
                {
                    rect.sizeDelta = new Vector2(width, row.Height);
                }

                rect.anchoredPosition = new Vector2(0f, -y);
                y += row.Height + rowSizes.y;
            }

            rowsHeight = spawned.Count > 0 ? y - rowSizes.y : 0f;
            rowsWidth = width;
        }

        private void LateUpdate()
        {
            if (group == null) return;
            bool show = visible && !hiddenExternally;
            float target = show ? 1f : 0f;
            if (show) popT = Mathf.Min(1f, popT + Time.unscaledDeltaTime / Mathf.Max(0.01f, popSeconds));
            group.alpha = show ? popT : Mathf.MoveTowards(group.alpha, 0f, Time.unscaledDeltaTime / Mathf.Max(0.01f, popSeconds));
            group.blocksRaycasts = show;
            group.interactable = show;
            if (block != null) block.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, show ? popT : 1f);
            if (!visible && target == 0f && group.alpha <= 0f) return;
            Layout();
            Bob();
        }

        private void Layout()
        {
            var parent = block.parent as RectTransform;
            if (parent == null || bubble == null) return;
            Rect area = parent.rect;
            float tabOverhang = nameTab != null && nameTab.gameObject.activeSelf ? 16f : 0f;
            float gap = rowsHeight > 0f ? (costChip != null && costChip.gameObject.activeSelf ? 26f : 8f) : 0f;
            Vector2 blockSize = new Vector2(Mathf.Max(bubbleSize.x, rowsWidth), bubbleSize.y + tabOverhang + gap + rowsHeight);
            Rect safe = SafeArea(parent, area);
            Vector2 head = HeadPoint(parent, area, safe);
            BubblePlacement place = BubbleLayout.Place(head, blockSize, bubbleSize.y + tabOverhang, safe, headOffset);

            block.anchoredPosition = place.BlockMin - area.min;
            block.sizeDelta = blockSize;
            bubble.anchoredPosition = new Vector2(0f, rowsHeight + gap);
            bubble.sizeDelta = bubbleSize;
            if (bubbleBack != null)
            {
                bubbleBack.anchoredPosition = bubble.anchoredPosition;
                bubbleBack.sizeDelta = bubbleSize;
            }

            if (rows != null) rows.anchoredPosition = new Vector2(0f, rowsHeight);

            // Tail from the corner nearest the head, aimed at it (also when docked to an edge).
            Vector2 baseLocal = place.TailBase - place.BlockMin;
            float distance = Vector2.Distance(place.TailBase, head);
            float length = Mathf.Clamp(distance - 4f, 10f, 28f);
            if (tailFill != null && tailFill.enabled)
            {
                PlaceTail(tailOutline.rectTransform, baseLocal, place.TailAngle, length + 3f, 24f);
                PlaceTail(tailFill.rectTransform, baseLocal, place.TailAngle, length, 18f);
            }

            Vector2 dir = new Vector2(Mathf.Cos(place.TailAngle * Mathf.Deg2Rad), Mathf.Sin(place.TailAngle * Mathf.Deg2Rad));
            float[] steps = { 10f, 24f, 35f };
            for (int i = 0; i < thoughtDots.Length; i++)
            {
                if (thoughtDots[i] == null || !thoughtDots[i].enabled) continue;
                float step = Mathf.Min(steps[Mathf.Min(i, steps.Length - 1)], Mathf.Max(6f, distance - 6f) * (i + 1) / thoughtDots.Length);
                thoughtDots[i].rectTransform.anchoredPosition = baseLocal + dir * step;
            }
        }

        private static void PlaceTail(RectTransform tail, Vector2 at, float angle, float length, float width)
        {
            tail.anchorMin = tail.anchorMax = Vector2.zero;
            tail.pivot = new Vector2(0f, 0.5f);
            tail.anchoredPosition = at;
            tail.sizeDelta = new Vector2(length, width);
            tail.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        /// <summary>Parent-space rect below the panic meter, left of the bag column, inside the screen.</summary>
        private Rect SafeArea(RectTransform parent, Rect area)
        {
            float xMax = area.xMax - margin;
            float yMax = area.yMax - margin;
            if (rightLimit != null && rightLimit.gameObject.activeInHierarchy) xMax = Mathf.Min(xMax, LocalMin(parent, rightLimit).x - margin);
            if (topLimit != null) yMax = Mathf.Min(yMax, LocalMin(parent, topLimit).y - margin);
            return Rect.MinMaxRect(area.xMin + margin, area.yMin + margin, xMax, yMax);
        }

        private static Vector2 LocalMin(RectTransform parent, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 local = parent.InverseTransformPoint(corners[0]);
            return local;
        }

        /// <summary>The speaker's head in parent space; without a speaker, low centre of the safe area.</summary>
        private Vector2 HeadPoint(RectTransform parent, Rect area, Rect safe)
        {
            Camera cam = worldCamera != null ? worldCamera : Camera.main;
            // The speaker's room may only become active mid-transition: resolve the anchor lazily.
            if ((anchor == null || !anchor.isActiveAndEnabled) && speakerKey != null) Resolve();
            if (anchor == null || !anchor.isActiveAndEnabled || cam == null) return new Vector2(safe.center.x - 180f, safe.yMin + 40f);
            Vector3 screen = cam.WorldToScreenPoint(anchor.HeadWorld);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out Vector2 local);
            return local;
        }

        private void Resolve()
        {
            anchor = SpeakerAnchor.Find(speakerKey);
            if (anchor != null && nameLabel != null && !thought) nameLabel.color = anchor.Accent;
        }

        private void Bob()
        {
            if (continueMark == null) return;
            bool show = waitingAdvance && !IsTyping;
            if (continueMark.gameObject.activeSelf != show) continueMark.gameObject.SetActive(show);
            if (!show) return;
            float y = LedClock.ReduceEffects ? 0f : Mathf.Sin(Time.unscaledTime * bob.y * Mathf.PI * 2f) * bob.x;
            continueMark.anchoredPosition = new Vector2(-14f, 12f + y);
        }

        /// <summary>A white right-pointing triangle (base on the left edge), generated once.</summary>
        private static Sprite Triangle()
        {
            if (triangle != null) return triangle;
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "BubbleTail" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float half = (size - 1 - x) * 0.5f;
                    float dy = Mathf.Abs(y + 0.5f - size * 0.5f);
                    float a = Mathf.Clamp01(half - dy + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            triangle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0f, 0.5f), 100f);
            return triangle;
        }
    }
}
