using System;
using System.Collections;
using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Core;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>Kind of room change.</summary>
    public enum TransitionKind
    {
        /// <summary>Same floor: the standard rise / drop.</summary>
        Lateral,

        /// <summary>Down a floor (rope, cables, stairs, hole): same effect with a longer, higher drop.</summary>
        Descend
    }

    /// <summary>
    /// Room-to-room transition (coroutines, unscaled time, no DOTween, never touches Time.timeScale). Locks input
    /// (NavigationEvents.OnTransitionStart), then: OUT, the old room's floor tiles, walls and props rise and fade
    /// in a wave from the far corner while the family fades; SWAP at the midpoint; IN, the new room's floor drops
    /// in, then the walls, then the props, with a small settle, and the player pops in at the arrival point; the
    /// wife and child stay hidden until input unlocks (OnTransitionComplete), then appear at the arrival door and run
    /// in (RoomSwitcher / FamilyFollower). Timing comes from TransitionConfig via the pure TileTransitionPlan.
    /// Reduce Effects plays a quick fade instead. Rewinds never get here (RoomSwitcher swaps instantly).
    /// </summary>
    public sealed class RoomTransitionController : MonoBehaviour
    {
        [Tooltip("Durations, heights, stagger, bounce and cues.")]
        [SerializeField] private TransitionConfig config;

        [Tooltip("Full-screen dark overlay for the Reduce Effects fade (never blocks clicks; input is locked through InputGate).")]
        [SerializeField] private CanvasGroup overlay;

        [Tooltip("Family members, player first (hidden during the swap; the player pops in after the new room has landed, the others run in after).")]
        [SerializeField] private Transform[] characters = new Transform[0];

        private readonly Dictionary<Room, RoomTransitionPieces> piecesByRoom = new Dictionary<Room, RoomTransitionPieces>();
        private readonly List<RoomTransitionPieces> active = new List<RoomTransitionPieces>();

        private Coroutine running;
        private Action pendingMidpoint;
        private string pendingRoom;
        private bool locked;
        private CharacterLook[] looks = new CharacterLook[0];

        private struct CharacterLook
        {
            public Transform Transform;
            public Vector3 Scale;
            public SpriteRenderer[] Sprites;
            public Color[] Colors;
        }

        public bool IsPlaying => running != null;

        private void Awake()
        {
            SetOverlay(0f);
        }

        private void OnDisable()
        {
            Finish();
        }

        /// <summary>Plays a transition; midpoint swaps the room. A running transition completes first.</summary>
        public void Play(TransitionKind kind, Room from, Room to, Action midpoint)
        {
            Finish();
            string toId = to != null ? to.RoomId : null;
            if (config == null || !isActiveAndEnabled)
            {
                midpoint?.Invoke();
                NavigationEvents.RaiseTransitionComplete(toId);
                return;
            }

            pendingMidpoint = midpoint;
            pendingRoom = toId;
            InputGate.Block();
            locked = true;
            NavigationEvents.RaiseTransitionStart(from != null ? from.RoomId : null, toId);
            if (kind == TransitionKind.Descend) AudioService.PlayCue(config.DescendCue);
            // Footsteps loop while the player runs (FootstepsAudio), not per transition.
            else AudioService.PlayCue(config.DoorCue);

            GameManager gm = GameManager.Instance;
            bool reduce = gm != null && gm.Settings != null && gm.Settings.ReduceEffects;
            running = StartCoroutine(reduce ? Fade() : Tiles(kind == TransitionKind.Descend, from, to));
        }

        /// <summary>Jumps a running transition to its end (swap, everything at rest, input unlocked).</summary>
        public void Finish()
        {
            if (running != null)
            {
                StopCoroutine(running);
                running = null;
            }

            RunMidpoint();
            RestoreAll();
            ShowCharacters(1f, 1f);
            if (locked) Complete();
        }

        private IEnumerator Tiles(bool descend, Room from, Room to)
        {
            TransitionTiming timing = config.Timing(descend);
            CaptureCharacters();

            // OUT: the old room rises and fades, far corner first; the family fades with it.
            RoomTransitionPieces outPieces = Begin(from);
            float outTotal = TileTransitionPlan.OutTotal(timing);
            float outPiece = TileTransitionPlan.OutPieceDuration(timing);
            for (float t = 0f; t < outTotal; t += Time.unscaledDeltaTime)
            {
                if (outPieces != null)
                {
                    foreach (RoomTransitionPieces.Piece piece in outPieces.Pieces)
                    {
                        float k = TileTransitionPlan.Progress(t, TileTransitionPlan.OutDelay(piece.Depth01, timing), outPiece);
                        RoomTransitionPieces.Apply(piece, new Vector3(0f, timing.Rise * TileTransitionPlan.EaseIn(k), 0f), 1f - k);
                    }
                }

                ShowCharacters(1f - Mathf.Clamp01(t / outTotal), 1f);
                yield return null;
            }

            // SWAP: activate the new room, put the old one back to rest, start the new pieces up high and invisible.
            ShowCharacters(0f, 1f);
            RunMidpoint();
            End(outPieces);
            RoomTransitionPieces inPieces = Begin(to);
            float inPiece = TileTransitionPlan.InPieceDuration(timing);
            float landed = TileTransitionPlan.InDelay(TransitionLayer.Props, 1f, timing) + inPiece;
            ApplyIn(inPieces, 0f, timing, inPiece);

            // IN: floor, then walls, then props drop in with a small settle.
            for (float t = 0f; t < landed; t += Time.unscaledDeltaTime)
            {
                ApplyIn(inPieces, t, timing, inPiece);
                yield return null;
            }

            End(inPieces);

            // The player pops in at the arrival point; the wife and child run in once input unlocks.
            for (float t = 0f; t < timing.CharacterPop; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.Clamp01(t / timing.CharacterPop);
                ShowCharacters(Mathf.Clamp01(k * 2f), TileTransitionPlan.PopScale(k), 0f);
                yield return null;
            }

            ShowCharacters(1f, 1f);
            running = null;
            Complete();
        }

        private static void ApplyIn(RoomTransitionPieces pieces, float t, TransitionTiming timing, float pieceSeconds)
        {
            if (pieces == null) return;
            foreach (RoomTransitionPieces.Piece piece in pieces.Pieces)
            {
                float k = TileTransitionPlan.Progress(t, TileTransitionPlan.InDelay(piece.Layer, piece.Depth01, timing), pieceSeconds);
                float height = timing.Drop * TileTransitionPlan.DropOffset01(k, timing.Bounce);
                RoomTransitionPieces.Apply(piece, new Vector3(0f, height, 0f), Mathf.Clamp01(k * 3f));
            }
        }

        // Reduce Effects: a plain quick fade through the overlay, nothing moves.
        private IEnumerator Fade()
        {
            float half = config.ReducedFadeDuration * 0.5f;
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                SetOverlay(Mathf.Clamp01(t / half));
                yield return null;
            }

            SetOverlay(1f);
            RunMidpoint();
            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                SetOverlay(1f - Mathf.Clamp01(t / half));
                yield return null;
            }

            running = null;
            Complete();
        }

        private RoomTransitionPieces Begin(Room room)
        {
            if (room == null) return null;
            if (!piecesByRoom.TryGetValue(room, out RoomTransitionPieces pieces))
            {
                pieces = new RoomTransitionPieces(room);
                piecesByRoom[room] = pieces;
            }

            pieces.Begin();
            active.Add(pieces);
            return pieces;
        }

        private void End(RoomTransitionPieces pieces)
        {
            if (pieces == null) return;
            pieces.Restore();
            active.Remove(pieces);
        }

        private void RestoreAll()
        {
            foreach (RoomTransitionPieces pieces in active) pieces.Restore();
            active.Clear();
        }

        private void RunMidpoint()
        {
            Action midpoint = pendingMidpoint;
            pendingMidpoint = null;
            midpoint?.Invoke();
        }

        private void CaptureCharacters()
        {
            looks = new CharacterLook[characters.Length];
            for (int i = 0; i < characters.Length; i++)
            {
                Transform t = characters[i];
                if (t == null) continue;
                SpriteRenderer[] sprites = t.GetComponentsInChildren<SpriteRenderer>(true);
                var colors = new Color[sprites.Length];
                for (int s = 0; s < sprites.Length; s++) colors[s] = sprites[s].color;
                looks[i] = new CharacterLook { Transform = t, Scale = t.localScale, Sprites = sprites, Colors = colors };
            }
        }

        // alpha 0..1 and a scale factor on the captured look (1, 1 = exactly as before the transition).
        private void ShowCharacters(float alpha, float scale) => ShowCharacters(alpha, scale, alpha);

        // The player (first character) gets alpha and scale; the others only an alpha at their normal scale.
        private void ShowCharacters(float alpha, float scale, float othersAlpha)
        {
            for (int i = 0; i < looks.Length; i++)
            {
                CharacterLook look = looks[i];
                if (look.Transform == null) continue;
                look.Transform.localScale = look.Scale * (i == 0 ? scale : 1f);
                float a = i == 0 ? alpha : othersAlpha;
                for (int s = 0; s < look.Sprites.Length; s++)
                {
                    if (look.Sprites[s] == null) continue;
                    Color c = look.Colors[s];
                    c.a *= a;
                    look.Sprites[s].color = c;
                }
            }

            if (alpha >= 1f && othersAlpha >= 1f && scale == 1f) looks = new CharacterLook[0];
        }

        private void Complete()
        {
            SetOverlay(0f);
            if (locked)
            {
                locked = false;
                InputGate.Unblock();
            }

            NavigationEvents.RaiseTransitionComplete(pendingRoom);
        }

        private void SetOverlay(float alpha)
        {
            if (overlay == null) return;
            overlay.alpha = alpha;
            overlay.blocksRaycasts = false;
            overlay.interactable = false;
        }
    }
}
