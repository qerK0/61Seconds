using System.Collections;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// "SİMÜLASYON SONA ERDİ": a full-screen overlay in the game scene, shown when the epilogue was read
    /// (GameEvents.EpilogueDone). Ruins background pushing in slowly, ash, grain and a vignette. The title types in
    /// with a terminal cursor and then glows; the "simulation" line types in (amber end); after a pause the red
    /// punchline lands with one hard glitch, a boom and a shake. Then the stats count up one by one and the buttons
    /// fade in: Yeniden Dene (a brand new run; the Deprem Notları stay) and Menüye Dön. Any key or click during the
    /// reveal completes it. Unscaled time throughout.
    /// </summary>
    public sealed class EndScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup root;
        [SerializeField] private RectTransform background;
        [SerializeField] private GameObject ambience;

        [Header("Text")]
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text line;
        [SerializeField] private TMP_Text punchline;
        [SerializeField] private GlitchText punchGlitch;
        [SerializeField] private Color highlight = new Color(0.949f, 0.635f, 0.227f, 1f);

        [Header("Stats (labels and values in row order: loops, notes, time, family)")]
        [SerializeField] private CanvasGroup stats;
        [SerializeField] private TMP_Text[] statValues = new TMP_Text[4];

        [Tooltip("Total Deprem Notları (for X/18).")]
        [SerializeField] private int totalTips = 18;

        [Header("Buttons")]
        [SerializeField] private CanvasGroup buttons;
        [SerializeField] private Button retry;
        [SerializeField] private Button menu;
        [SerializeField] private ScreenFader fader;

        [Header("Audio")]
        [SerializeField] private AudioCue outroMusic;
        [SerializeField] private AudioCue typeCue;
        [SerializeField] private AudioCue boomCue;

        [Header("Timing (seconds)")]
        [SerializeField] private float titleCharSeconds = 0.06f;
        [SerializeField] private float lineCharSeconds = 0.028f;
        [SerializeField] private float punchPause = 0.8f;

        private bool shown;
        private bool skip;
        private bool leaving;
        private string titleText;
        private Material titleMaterial;

        private void Awake()
        {
            SetGroup(root, false);
            if (ambience != null) ambience.SetActive(false);
            if (retry != null) retry.onClick.AddListener(() => Leave(SceneFlow.StartGame));
            if (menu != null) menu.onClick.AddListener(() => Leave(SceneFlow.LoadMainMenu));
        }

        private void OnEnable() => GameEvents.EpilogueDone += Show;

        private void OnDisable() => GameEvents.EpilogueDone -= Show;

        /// <summary>Shows the end screen and plays the reveal (once per scene).</summary>
        public void Show()
        {
            if (shown) return;
            shown = true;
            SetGroup(root, true);
            if (ambience != null) ambience.SetActive(true);
            StartCoroutine(Reveal());
            StartCoroutine(PushIn());
        }

        private void Update()
        {
            if (!shown || skip) return;
            Keyboard k = Keyboard.current;
            Mouse m = Mouse.current;
            if ((k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame)) || (m != null && m.leftButton.wasPressedThisFrame)) skip = true;
        }

        private IEnumerator PushIn()
        {
            for (float t = 0f; background != null; t += Time.unscaledDeltaTime)
            {
                float s = Mathf.Lerp(1f, 1.05f, Mathf.Clamp01(t / 30f));
                background.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        private IEnumerator Reveal()
        {
            if (AudioService.Instance != null && outroMusic != null)
            {
                AudioService.Instance.PlayMusic(outroMusic);
                StartCoroutine(FadeMusicIn(2.5f));
            }

            SetGroup(stats, false);
            SetGroup(buttons, false);
            titleText = title != null ? TextDB.GetDisplay(UiKeys.EndTitle) : string.Empty;
            if (line != null)
            {
                line.text = TextDB.GetDisplay(UiKeys.EndLine) + " <color=#" + ColorUtility.ToHtmlStringRGB(highlight) + ">" + TextDB.GetDisplay(UiKeys.EndLineHighlight) + "</color>";
                line.maxVisibleCharacters = 0;
            }

            if (punchline != null)
            {
                punchline.text = TextDB.GetDisplay(UiKeys.EndPunchline);
                punchline.alpha = 0f;
            }

            yield return Wait(0.6f);

            // 1. The title types in behind a blinking terminal cursor, then glows.
            for (int i = 0; i <= titleText.Length; i++)
            {
                if (title != null) title.text = titleText.Substring(0, i) + "_";
                if (i > 0 && i < titleText.Length && titleText[i - 1] != ' ') AudioService.PlayUiCue(typeCue);
                if (!skip) yield return Wait(titleCharSeconds);
            }

            yield return Cursor(0.6f);
            if (title != null) title.text = titleText;
            StartCoroutine(Glow());

            // 2. The simulation line.
            if (line != null)
            {
                line.ForceMeshUpdate();
                int total = line.textInfo.characterCount;
                for (int i = 1; i <= total; i++)
                {
                    line.maxVisibleCharacters = i;
                    if (i % 3 == 0) AudioService.PlayUiCue(typeCue);
                    if (!skip) yield return Wait(lineCharSeconds);
                }

                line.maxVisibleCharacters = total;
            }

            // 3. The punchline, with weight.
            yield return Wait(punchPause);
            if (punchline != null) punchline.alpha = 1f;
            AudioService.PlayUiCue(boomCue);
            if (punchGlitch != null) punchGlitch.Trigger(0.35f);
            UiShake.Kick(1f, 0.45f);
            yield return Wait(1.1f);

            // Stats, one by one, counting up.
            yield return FadeIn(stats, 0.35f);
            GameState state = GameManager.Instance != null ? GameManager.Instance.State : null;
            int loops = state != null ? state.LoopCount : 0;
            int tips = TipStore.Load().Count;
            if (state != null) tips = Mathf.Max(tips, state.CollectedTips.Count);
            yield return CountUp(0, loops, v => v.ToString());
            yield return CountUp(1, tips, v => $"{v}/{totalTips}");
            if (state != null && state.Flags.Get(FlagNames.CollapsedBehind))
            {
                SetStat(2, TextDB.GetDisplay(UiKeys.EndStatCollapsed));
                yield return Wait(0.25f);
            }
            else
            {
                int seconds = state != null ? Mathf.CeilToInt(Mathf.Max(0f, state.BuildingTime)) : 0;
                yield return CountUp(2, seconds, v => TextDB.Format(UiKeys.EndStatTimeValue, v));
            }

            SetStat(3, FamilyLine(state));
            AudioService.PlayUiCue(typeCue);
            yield return Wait(0.4f);

            yield return FadeIn(buttons, 0.4f);
            if (menu != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(menu.gameObject);
        }

        /// <summary>Everyone safe, or "Herkes dışarıda, N yaralı".</summary>
        private static string FamilyLine(GameState state)
        {
            int injured = 0;
            if (state != null)
            {
                foreach (FamilyMemberState member in state.Members)
                {
                    if (member.Health != MemberHealth.Healthy) injured++;
                }
            }

            return injured == 0 ? TextDB.GetDisplay(UiKeys.EndFamilySafe) : TextDB.Format(UiKeys.EndFamilyInjured, injured);
        }

        private IEnumerator CountUp(int row, int target, System.Func<int, string> format)
        {
            float seconds = skip ? 0f : Mathf.Clamp(0.25f + target * 0.01f, 0.25f, 0.9f);
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                SetStat(row, format(Mathf.RoundToInt(target * (t / seconds))));
                yield return null;
            }

            SetStat(row, format(target));
            AudioService.PlayUiCue(typeCue);
            yield return Wait(0.2f);
        }

        private void SetStat(int row, string text)
        {
            if (row < statValues.Length && statValues[row] != null) statValues[row].text = text;
        }

        private IEnumerator Cursor(float seconds)
        {
            for (float t = 0f; t < seconds && !skip; t += Time.unscaledDeltaTime)
            {
                if (title != null) title.text = titleText + (Mathf.Repeat(t, 0.4f) < 0.2f ? "_" : " ");
                yield return null;
            }
        }

        /// <summary>A soft glow grows on the title (TMP glow on an instanced material).</summary>
        private IEnumerator Glow()
        {
            if (title == null) yield break;
            titleMaterial = title.fontMaterial;
            titleMaterial.EnableKeyword("GLOW_ON");
            titleMaterial.SetColor("_GlowColor", new Color(0.9f, 0.9f, 0.88f, 0.5f));
            titleMaterial.SetFloat("_GlowOuter", 0.35f);
            for (float t = 0f; t < 1.2f; t += Time.unscaledDeltaTime)
            {
                titleMaterial.SetFloat("_GlowPower", Mathf.Lerp(0f, 0.35f, t / 1.2f));
                yield return null;
            }
        }

        private IEnumerator FadeMusicIn(float seconds)
        {
            float target = outroMusic.Volume * UserSettings.Music;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (AudioService.Instance != null) AudioService.Instance.SetMusicVolume(target * t / seconds);
                yield return null;
            }

            if (AudioService.Instance != null) AudioService.Instance.SetMusicVolume(target);
        }

        private IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds && !skip; t += Time.unscaledDeltaTime) yield return null;
        }

        private IEnumerator FadeIn(CanvasGroup group, float seconds)
        {
            if (group == null) yield break;
            group.interactable = true;
            group.blocksRaycasts = true;
            for (float t = 0f; t < seconds && !skip; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / seconds;
                yield return null;
            }

            group.alpha = 1f;
        }

        private void Leave(System.Action load)
        {
            if (leaving) return;
            leaving = true;
            if (AudioService.Instance != null) AudioService.Instance.StopMusic();
            if (fader != null) fader.FadeOut(0.5f, load);
            else load();
        }

        private static void SetGroup(CanvasGroup group, bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
