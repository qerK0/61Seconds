using System;
using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.UI
{
    /// <summary>
    /// Main menu "Galeri", two tabs. Deprem Notları: every Deprem Notu as a card; collected tips (TipStore, kept across
    /// runs) show their title and text, the others ???; the counter reads "Toplanan notlar: X/18". Bilgi Kartları: the
    /// 8 floor info cards, seen ones (InfoCardStore) with their title and text; "Görülen kartlar: X/8".
    /// </summary>
    public sealed class TipGalleryPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text counter;

        [Tooltip("Tip ids, one per card (card order).")]
        [SerializeField] private string[] tipIds = new string[0];

        [SerializeField] private TMP_Text[] titles = new TMP_Text[0];
        [SerializeField] private TMP_Text[] bodies = new TMP_Text[0];
        [SerializeField] private Image[] cards = new Image[0];

        [Header("Tabs (optional)")]
        [SerializeField] private CanvasGroup tipsPage;
        [SerializeField] private CanvasGroup infoPage;
        [SerializeField] private Button tipsTab;
        [SerializeField] private Button infoTab;
        [Tooltip("Shown under the active tab.")]
        [SerializeField] private GameObject tipsTabMarker;
        [SerializeField] private GameObject infoTabMarker;
        [SerializeField] private TMP_Text[] infoTitles = new TMP_Text[0];
        [SerializeField] private TMP_Text[] infoBodies = new TMP_Text[0];
        [SerializeField] private Image[] infoCards = new Image[0];

        [SerializeField] private Button back;
        [SerializeField] private AudioCue clickCue;

        [SerializeField] private Color lockedCard = new Color(0.1f, 0.05f, 0.05f, 0.85f);
        [SerializeField] private Color unlockedCard = new Color(0.24f, 0.06f, 0.07f, 0.92f);

        /// <summary>Raised when the panel closes (the menu shows its buttons again).</summary>
        public event Action Closed;

        public bool IsOpen => group != null && group.alpha > 0.5f;

        public Button BackButton => back;

        public void Configure(CanvasGroup panel, TMP_Text count, string[] ids, TMP_Text[] cardTitles, TMP_Text[] cardBodies, Image[] cardImages, Button backButton, AudioCue click)
        {
            group = panel;
            counter = count;
            tipIds = ids;
            titles = cardTitles;
            bodies = cardBodies;
            cards = cardImages;
            back = backButton;
            clickCue = click;
        }

        /// <summary>Wires the Bilgi Kartları tab (builder).</summary>
        public void ConfigureTabs(CanvasGroup tips, CanvasGroup info, Button tipsButton, Button infoButton, GameObject tipsMarker, GameObject infoMarker,
            TMP_Text[] cardTitles, TMP_Text[] cardBodies, Image[] cardImages)
        {
            tipsPage = tips;
            infoPage = info;
            tipsTab = tipsButton;
            infoTab = infoButton;
            tipsTabMarker = tipsMarker;
            infoTabMarker = infoMarker;
            infoTitles = cardTitles;
            infoBodies = cardBodies;
            infoCards = cardImages;
        }

        private bool infoShown;

        private void Awake()
        {
            if (back != null) back.onClick.AddListener(Close);
            if (tipsTab != null) tipsTab.onClick.AddListener(() => ShowTab(false));
            if (infoTab != null) infoTab.onClick.AddListener(() => ShowTab(true));
            SetVisible(false);
        }

        private void ShowTab(bool info)
        {
            if (info != infoShown) AudioService.PlayUiCue(clickCue);
            infoShown = info;
            SetGroup(tipsPage, !info);
            SetGroup(infoPage, info);
            if (tipsTabMarker != null) tipsTabMarker.SetActive(!info);
            if (infoTabMarker != null) infoTabMarker.SetActive(info);
            if (counter == null) return;
            if (info)
            {
                HashSet<int> seen = InfoCardStore.Load();
                counter.text = TextDB.Format(UiKeys.MenuGalleryInfoCount, seen.Count, InfoCardRules.CardCount);
            }
            else
            {
                counter.text = TextDB.Format(UiKeys.MenuGalleryCount, CountCollected(), tipIds.Length);
            }
        }

        private int CountCollected()
        {
            HashSet<string> collected = TipStore.Load();
            int have = 0;
            foreach (string id in tipIds) if (collected.Contains(id)) have++;
            return have;
        }

        private void FillInfoCards()
        {
            HashSet<int> seen = InfoCardStore.Load();
            for (int i = 0; i < infoTitles.Length; i++)
            {
                int card = i + 1;
                bool unlocked = seen.Contains(card);
                if (infoTitles[i] != null) infoTitles[i].text = unlocked ? TextDB.GetDisplay(UiKeys.InfoTitle(card)) : "???";
                if (i < infoBodies.Length && infoBodies[i] != null) infoBodies[i].text = unlocked ? TextDB.GetDisplay(UiKeys.InfoBody(card)) : string.Empty;
                if (i < infoCards.Length && infoCards[i] != null) infoCards[i].color = unlocked ? unlockedCard : lockedCard;
            }
        }

        private static void SetGroup(CanvasGroup g, bool visible)
        {
            if (g == null) return;
            g.alpha = visible ? 1f : 0f;
            g.interactable = visible;
            g.blocksRaycasts = visible;
        }

        public void Open()
        {
            HashSet<string> collected = TipStore.Load();
            int have = 0;
            for (int i = 0; i < tipIds.Length; i++)
            {
                bool unlocked = collected.Contains(tipIds[i]);
                if (unlocked) have++;
                if (i < titles.Length) titles[i].text = unlocked ? TextDB.GetDisplay(tipIds[i] + ".title") : "???";
                if (i < bodies.Length) bodies[i].text = unlocked ? TextDB.GetDisplay(tipIds[i]) : string.Empty;
                if (i < cards.Length) cards[i].color = unlocked ? unlockedCard : lockedCard;
            }

            if (counter != null) counter.text = TextDB.Format(UiKeys.MenuGalleryCount, have, tipIds.Length);
            FillInfoCards();
            SetVisible(true);
            infoShown = false;
            if (tipsPage != null || infoPage != null) ShowTab(false);
        }

        public void Close()
        {
            if (!IsOpen) return;
            AudioService.PlayUiCue(clickCue);
            SetVisible(false);
            Closed?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
        }
    }
}
