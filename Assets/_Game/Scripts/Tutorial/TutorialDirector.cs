using System;
using System.Collections;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Placement;
using SixtyOne.Text;
using SixtyOne.UI;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Tutorial
{
    /// <summary>
    /// Runs Scenes/Tutorial.unity on top of the real systems (NodePlayer, bag, loot, Craft Defteri, quake events):
    /// 1 walk to the floor marker, 2 search the dolap and take everything (bag slots, fener/düdük pockets), 3 craft the
    /// ıslak bez, 4 the LED clock / Panik / Enerji, 5 the quake decision node (a wrong choice explains and asks again),
    /// 6 walk out of the exit door, then the "Eğitim tamamlandı" panel. Each step shows a hint, waits until the player
    /// really did it (or presses Geç), plays the success sound and moves on. No timer pressure: the building clock
    /// shows but is frozen and actions cost nothing; nothing can kill.
    /// </summary>
    public sealed class TutorialDirector : MonoBehaviour
    {
        private const int StepCount = 6;

        [Header("Systems")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private NodePlayer nodePlayer;
        [SerializeField] private RoomSwitcher rooms;
        [SerializeField] private CameraShake cameraShake;

        [Tooltip("The SurvivalController (kept disabled: no clock, drain, poison or panic).")]
        [SerializeField] private Behaviour survival;

        [Tooltip("Mehmet.")]
        [SerializeField] private Transform player;

        [Header("UI")]
        [SerializeField] private TutorialHintPanel hint;
        [SerializeField] private TutorialHighlight highlight;
        [SerializeField] private PauseMenu pauseMenu;

        [Tooltip("The bag column (opened for the bag and craft steps).")]
        [SerializeField] private CollapsiblePanel bagPanel;

        [Tooltip("'Öğreticiyi atla' in the pause menu.")]
        [SerializeField] private Button skipTutorial;

        [Tooltip("'Eğitim tamamlandı' panel with Oyuna başla / Ana menü.")]
        [SerializeField] private CanvasGroup completePanel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button menuButton;

        [Header("Targets")]
        [SerializeField] private Transform walkMarker;
        [SerializeField] private Transform container;
        [SerializeField] private RectTransform lootWindow;
        [SerializeField] private RectTransform bagSlots;
        [SerializeField] private RectTransform[] pockets = new RectTransform[0];
        [SerializeField] private RectTransform craftButton;
        [SerializeField] private RectTransform notebook;
        [SerializeField] private RectTransform clock;
        [SerializeField] private RectTransform panic;
        [SerializeField] private RectTransform[] energy = new RectTransform[0];

        [Tooltip("The door from the salon into the hall.")]
        [SerializeField] private Transform innerDoor;

        [Tooltip("The flat's exit door (plays the exit choice).")]
        [SerializeField] private Transform exitDoor;

        [Header("Feel")]
        [SerializeField] private AudioCue successCue;
        [SerializeField] private AudioCue wrongCue;
        [SerializeField] private AudioCue clickCue;

        [Tooltip("Calm music under the tutorial.")]
        [SerializeField] private AudioCue music;

        [Tooltip("Distance (world units at scale 1) that counts as standing on the floor marker.")]
        [SerializeField] private float markerRadius = 0.35f;

        [Tooltip("Longest the scripted quake may shake while the player decides (seconds).")]
        [SerializeField] private float quakeSeconds = 60f;

        [Tooltip("Shake strength of the scripted quake (1 = the opening quake).")]
        [SerializeField] private float quakeShake = 0.55f;

        private bool skip;
        private bool finished;
        private bool quaking;
        private bool holding;
        private string lastChoice;
        private string lastNode;
        private Coroutine run;
        private QuakeInfo quake;

        private GameManager Gm => gameManager != null ? gameManager : GameManager.Instance;
        private GameState State => Gm.State;

        private void Awake()
        {
            // The tutorial clock never runs: SurvivalController stays off (its Awake still sets up the state).
            if (survival != null) survival.enabled = false;
            if (hint != null) hint.SkipPressed += () => skip = true;
            if (skipTutorial != null) skipTutorial.onClick.AddListener(SkipTutorial);
            if (playButton != null) playButton.onClick.AddListener(() => Leave(SceneFlow.StartGame));
            if (menuButton != null) menuButton.onClick.AddListener(() => Leave(SceneFlow.LoadMainMenu));
            UIVisibility.Set(completePanel, false);
        }

        private void OnEnable()
        {
            GameEvents.ChoiceMade += OnChoiceMade;
            GameEvents.NodeEntered += OnNodeEntered;
        }

        private void OnDisable()
        {
            GameEvents.ChoiceMade -= OnChoiceMade;
            GameEvents.NodeEntered -= OnNodeEntered;
        }

        private void OnDestroy()
        {
            ReleaseHold();
        }

        private IEnumerator Start()
        {
            // After every Awake / Start: SurvivalController.Awake registered itself as the cost sink and reset the
            // state for the prep. Show the building clock (frozen) and make every action free.
            yield return null;
            ActionCostRouter.Sink = null;
            State.BuildingTimerStarted = true;
            if (music != null && AudioService.Instance != null) AudioService.Instance.PlayMusic(music);
            run = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return Walk();
            yield return Loot();
            yield return Craft();
            yield return Interface();
            yield return Quake();
            yield return Exit();
            Finish();
        }

        // ---------- steps ----------

        private IEnumerator Walk()
        {
            Begin();
            Show(TextDB.Format(UiKeys.TutorialWalk, TutorialBindings.PointerPress()), 1, false);
            highlight.PointAtWorld(walkMarker);
            yield return Until(OnMarker);
            if (walkMarker != null) walkMarker.gameObject.SetActive(false);
            yield return Succeed();
        }

        private IEnumerator Loot()
        {
            Begin();
            // The bag column may be collapsed (remembered per viewer): this step and the next point into it.
            if (bagPanel != null) bagPanel.Expand();
            int shown = -1;
            while (!skip && !HasAll(TutorialIds.LootItems))
            {
                // Open the dolap first; while its window is open, take everything.
                int now = LootEvents.IsOpen ? 1 : 0;
                if (now != shown)
                {
                    shown = now;
                    Show(TextDB.Get(now == 0 ? UiKeys.TutorialLootOpen : UiKeys.TutorialLootTake), 2, false);
                    if (now == 0) highlight.PointAtWorld(container);
                    else highlight.PointAt(lootWindow);
                }

                yield return null;
            }

            if (skip) Grant(TutorialIds.LootItems);
            // The emptied dolap window would cover the bag tour.
            if (LootEvents.IsOpen) LootEvents.RaiseClosed();
            yield return Succeed();

            Begin();
            Show(TextDB.Get(UiKeys.TutorialBagSlots), 2, true);
            highlight.PointAt(bagSlots);
            yield return Continue();
            Show(TextDB.Get(UiKeys.TutorialBagPockets), 2, true);
            highlight.PointAt(pockets);
            yield return Continue();
        }

        private IEnumerator Craft()
        {
            Begin();
            if (bagPanel != null) bagPanel.Expand();
            int shown = -1;
            while (!skip && !StateOps.HasItem(State, TutorialIds.CraftOutput))
            {
                int now = CraftNotebook.Open != null ? 1 : 0;
                if (now != shown)
                {
                    shown = now;
                    Show(now == 0 ? TextDB.Format(UiKeys.TutorialCraftOpen, TutorialBindings.CraftKey()) : TextDB.Get(UiKeys.TutorialCraftCombine), 3, false);
                    if (now == 0) highlight.PointAt(craftButton);
                    else highlight.PointAt(notebook);
                }

                yield return null;
            }

            if (skip && !StateOps.HasItem(State, TutorialIds.CraftOutput))
            {
                StateOps.RemoveItem(State, TutorialIds.CraftA);
                StateOps.RemoveItem(State, TutorialIds.CraftB);
                Grant(TutorialIds.CraftOutput);
            }

            yield return Succeed();
            // Clear the view for the HUD tour.
            if (CraftNotebook.Open != null) CraftNotebook.Open.Close();
        }

        private IEnumerator Interface()
        {
            Begin();
            Show(TextDB.Get(UiKeys.TutorialUiClock), 4, true);
            highlight.PointAt(clock);
            yield return Continue();
            Show(TextDB.Get(UiKeys.TutorialUiPanic), 4, true);
            highlight.PointAt(panic);
            yield return Continue();
            Show(TextDB.Get(UiKeys.TutorialUiEnergy), 4, true);
            highlight.PointAt(energy);
            yield return Continue();
            yield return Succeed();
        }

        private IEnumerator Quake()
        {
            Begin();
            highlight.Clear();
            Show(TextDB.Get(UiKeys.TutorialQuake), 5, false);
            StartQuake();
            // Let the floor start shaking (and everyone crouch) before the decision comes up.
            yield return Seconds(1.2f);

            lastChoice = null;
            SceneNode decision = nodePlayer != null && nodePlayer.Content != null ? nodePlayer.Content.FindNode(TutorialIds.QuakeNode) : null;
            if (decision != null && !skip) nodePlayer.Enter(decision);

            while (!skip && lastNode != TutorialIds.AfterNode)
            {
                if (lastChoice == TutorialIds.ChoiceRun || lastChoice == TutorialIds.ChoiceWindow)
                {
                    // A wrong choice: the AFAD reason, then the same three options again (no death, no rewind).
                    Show(TextDB.Get(lastChoice == TutorialIds.ChoiceRun ? UiKeys.TutorialQuakeWrongRun : UiKeys.TutorialQuakeWrongWindow), 5, false);
                    AudioService.PlayUiCue(wrongCue);
                    lastChoice = null;
                }

                yield return null;
            }

            if (lastNode != TutorialIds.AfterNode) EnterNode(TutorialIds.AfterNode);
            yield return Succeed();

            // "Hold on until it stops": the floor keeps shaking under the praise, then it settles.
            Begin();
            Show(TextDB.Get(UiKeys.TutorialQuakeRight), 5, true);
            yield return Continue();
            StopQuake();
            yield return Seconds(0.8f);
        }

        private IEnumerator Exit()
        {
            Begin();
            Show(TextDB.Get(UiKeys.TutorialExit), 6, false);
            Transform target = null;
            while (!skip && lastNode != TutorialIds.DoneNode)
            {
                // Through the hall door first, then the flat's exit door.
                Transform next = rooms != null && rooms.Current != null && rooms.Current.RoomId == TutorialIds.Hall ? exitDoor : innerDoor;
                if (next != target)
                {
                    target = next;
                    highlight.PointAtWorld(target);
                }

                yield return null;
            }

            yield return Succeed();
        }

        // ---------- end ----------

        private void Finish()
        {
            if (finished) return;
            finished = true;
            highlight.Clear();
            hint.Hide();
            TutorialProgress.Completed = true;
            // A full-screen card: timers held, world clicks and the pause menu's Esc ignored.
            if (!holding)
            {
                holding = true;
                ScreenHold.Begin();
                InputGate.Block();
            }

            UIVisibility.Set(completePanel, true);
        }

        /// <summary>"Öğreticiyi atla" (pause menu): straight to the end panel.</summary>
        private void SkipTutorial()
        {
            AudioService.PlayUiCue(clickCue);
            if (pauseMenu != null) pauseMenu.Resume();
            if (run != null) StopCoroutine(run);
            run = null;
            StopQuake();
            Finish();
        }

        private void Leave(Action load)
        {
            AudioService.PlayUiCue(clickCue);
            ReleaseHold();
            load();
        }

        private void ReleaseHold()
        {
            if (!holding) return;
            holding = false;
            ScreenHold.End();
            InputGate.Unblock();
        }

        // ---------- quake ----------

        private void StartQuake()
        {
            quaking = true;
            // The opening quake's sound (act 1); CameraShake, the crouch, the flicker and the dust react to the event.
            quake = new QuakeInfo(1, quakeSeconds, QuakeInfo.MagnitudeForAct(1), quakeShake, 1);
            GameEvents.RaiseQuakeStarted(quake);
        }

        private void StopQuake()
        {
            if (!quaking) return;
            quaking = false;
            GameEvents.RaiseQuakeEnded(quake);
            if (cameraShake != null) cameraShake.Shake(0.01f, 0f);
        }

        // ---------- helpers ----------

        private void Begin()
        {
            skip = false;
        }

        private void Show(string text, int step, bool waitsForContinue)
        {
            if (hint != null) hint.Show(text, step, StepCount, waitsForContinue);
        }

        private IEnumerator Succeed()
        {
            AudioService.PlayUiCue(successCue);
            highlight.Clear();
            yield return Seconds(0.45f);
        }

        private IEnumerator Until(Func<bool> done)
        {
            while (!skip && !done()) yield return null;
        }

        /// <summary>Waits for a fresh click / Space / Enter (not on Geç, not while paused) or a skip.</summary>
        private IEnumerator Continue()
        {
            yield return null;
            while (!skip)
            {
                if (!PauseState.IsPaused && TutorialBindings.ContinuePressed() && !OverSkip()) break;
                yield return null;
            }

            AudioService.PlayUiCue(clickCue);
        }

        private IEnumerator Seconds(float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                if (PauseState.IsPaused) t -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private bool OverSkip()
        {
            RectTransform rect = hint != null ? hint.SkipRect : null;
            UnityEngine.InputSystem.Pointer pointer = UnityEngine.InputSystem.Pointer.current;
            return rect != null && pointer != null && RectTransformUtility.RectangleContainsScreenPoint(rect, pointer.position.ReadValue(), null);
        }

        private bool OnMarker()
        {
            if (walkMarker == null || player == null) return true;
            float scale = player.parent != null ? Mathf.Abs(player.parent.lossyScale.x) : 1f;
            Vector2 d = player.position - walkMarker.position;
            return d.magnitude <= markerRadius * scale;
        }

        private bool HasAll(string[] ids)
        {
            foreach (string id in ids)
            {
                if (!StateOps.HasItem(State, id)) return false;
            }

            return true;
        }

        /// <summary>Puts what a skipped step would have given into the bag (key items into their pockets).</summary>
        private void Grant(params string[] ids)
        {
            GameContent content = nodePlayer != null ? nodePlayer.Content : null;
            foreach (string id in ids)
            {
                if (StateOps.HasItem(State, id)) continue;
                Item item = content != null ? content.FindItem(id) : null;
                StateOps.TryAddItem(State, id, item != null && item.IsKeyItem);
            }
        }

        private void EnterNode(string id)
        {
            SceneNode node = nodePlayer != null && nodePlayer.Content != null ? nodePlayer.Content.FindNode(id) : null;
            if (node != null) nodePlayer.Enter(node);
        }

        private void OnChoiceMade(string nodeId, string labelKey)
        {
            if (nodeId == TutorialIds.QuakeNode) lastChoice = labelKey;
        }

        private void OnNodeEntered(string nodeId)
        {
            lastNode = nodeId;
        }

        /// <summary>Authoring helper for the scene builder (systems).</summary>
        public void ConfigureSystems(GameManager manager, NodePlayer nodes, RoomSwitcher switcher, CameraShake shake, Behaviour survivalController, Transform mehmet)
        {
            gameManager = manager;
            nodePlayer = nodes;
            rooms = switcher;
            cameraShake = shake;
            survival = survivalController;
            player = mehmet;
        }

        /// <summary>Authoring helper for the scene builder (UI).</summary>
        public void ConfigureUi(TutorialHintPanel hintPanel, TutorialHighlight pointer, PauseMenu pause, Button skipButton, CanvasGroup complete, Button play, Button menu)
        {
            hint = hintPanel;
            highlight = pointer;
            pauseMenu = pause;
            skipTutorial = skipButton;
            completePanel = complete;
            playButton = play;
            menuButton = menu;
        }

        /// <summary>Authoring helper for the scene builder (what each step points at).</summary>
        public void ConfigureTargets(Transform marker, Transform dolap, RectTransform loot, RectTransform slots, RectTransform[] pocketRects, RectTransform craft,
            RectTransform craftPage, RectTransform clockRect, RectTransform panicRect, RectTransform[] energyRects, Transform hallDoor, Transform exit)
        {
            walkMarker = marker;
            container = dolap;
            lootWindow = loot;
            bagSlots = slots;
            pockets = pocketRects;
            craftButton = craft;
            notebook = craftPage;
            clock = clockRect;
            panic = panicRect;
            energy = energyRects;
            innerDoor = hallDoor;
            exitDoor = exit;
        }

        /// <summary>Authoring helper for the scene builder (the collapsible bag column).</summary>
        public void ConfigureBag(CollapsiblePanel bag) => bagPanel = bag;

        /// <summary>Authoring helper for the scene builder (sounds).</summary>
        public void ConfigureAudio(AudioCue success, AudioCue wrong, AudioCue click, AudioCue calmMusic)
        {
            successCue = success;
            wrongCue = wrong;
            clickCue = click;
            music = calmMusic;
        }
    }
}
