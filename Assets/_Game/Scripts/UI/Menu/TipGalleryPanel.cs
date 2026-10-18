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
    /// Main menu "Galeri": every Deprem Notu as a card. Collected tips (TipStore, kept across runs) show their title
    /// and text; the others show ???. A counter reads "Toplanan notlar: X/18".
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

        private void Awake()
        {
            if (back != null) back.onClick.AddListener(Close);
            SetVisible(false);
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
            SetVisible(true);
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
