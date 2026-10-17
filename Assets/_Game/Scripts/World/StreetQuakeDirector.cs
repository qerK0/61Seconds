using System;
using System.Collections;
using System.Collections.Generic;
using SixtyOne.Audio;
using SixtyOne.Building;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Placement;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// The street finale's aftershock (the 4th and strongest quake): about quakeDelay seconds after entering the street
    /// (or as soon as the player nears the power line) it hits with maximum shake (Reduce Effects still scales it),
    /// the tilt, a louder cue_quake_f4, car alarms, glass raining from the façades and 3-4 façade drops (a red
    /// shadow grows on 2x2 cells for warnSeconds, then rubble crashes there; a member under it dies / is trapped).
    /// "Açık alana çök!": at the peak the player must stand in the open middle (StreetLayout.InOpen), away from the
    /// façades and the line. The sparking cells kill on contact. When the building timer hits 0 here, the collapse
    /// only kills within StreetLayout.CollapseRadius cells of the door; otherwise it is a dust-and-shake beat.
    /// Every death here is a macro rewind to the street checkpoint.
    /// </summary>
    public sealed class StreetQuakeDirector : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Rooms (the street) and the family.")]
        [SerializeField] private RoomSwitcher rooms;

        [Tooltip("Façade rubble blocks paths through it (never the only path).")]
        [SerializeField] private DebrisPresenter debris;

        [Tooltip("Camera shake for the building collapsing behind them.")]
        [SerializeField] private CameraShake cameraShake;

        [Tooltip("The street room id.")]
        [SerializeField] private string streetRoomId = "street";

        [Tooltip("The street node id (checkpoint).")]
        [SerializeField] private string streetNodeId = "f2.street";

        [Header("Quake")]
        [Tooltip("Seconds after entering the street before the aftershock.")]
        [SerializeField] private float quakeDelay = 6f;

        [Tooltip("The quake also starts once the player is this close (cells) to the power line.")]
        [SerializeField] private int triggerCellsFromLine = 2;

        [Tooltip("Seconds of shaking.")]
        [SerializeField] private float quakeSeconds = 7f;

        [Tooltip("Seconds into the shaking when it peaks (the open-area check).")]
        [SerializeField] private float peakAt = 3.6f;

        [Tooltip("Magnitude shown.")]
        [SerializeField] private float magnitude = 6.4f;

        [Tooltip("Camera shake multiplier on the floor-2 strength (the strongest of the game).")]
        [SerializeField] private float shakeScale = 1.6f;

        [Tooltip("Volume scale of the quake recording (cue_quake_f4).")]
        [SerializeField] private float loudness = 1.35f;

        [Header("Façade drops")]
        [Tooltip("Seconds into the shaking when each façade drop's warning starts (3-4 drops).")]
        [SerializeField] private float[] dropStarts = { 0.6f, 1.7f, 2.8f, 4.0f };

        [Tooltip("Seconds the red warning shadow grows before the rubble lands.")]
        [SerializeField] private float warnSeconds = 1.2f;

        [Tooltip("Hazard for members under a façade drop (player death, wife cut to black, child trapped).")]
        [SerializeField] private Hazard facadeHazard;

        /// <summary>Hazard used for façade drops (the validator counts it as linked).</summary>
        public Hazard FacadeHazard => facadeHazard;

        [Header("Deaths")]
        [Tooltip("Not in the open at the peak.")]
        [SerializeField] private string openDeathKey = UiKeys.StreetGlassDeath;

        [Tooltip("Tip for the street deaths.")]
        [SerializeField] private string streetTipId = "tip.street";

        [Tooltip("The power line (its death is also used for the spark cells).")]
        [SerializeField] private StreetPowerLine powerLine;

        [Header("Look and sound")]
        [Tooltip("Floor diamond for the warning shadow.")]
        [SerializeField] private Sprite warningSprite;

        [Tooltip("Rubble sprites for the façade drops.")]
        [SerializeField] private Sprite[] rubbleSprites = new Sprite[0];

        [Tooltip("Small soft sprite for glass shards and dust.")]
        [SerializeField] private Sprite shardSprite;

        [Tooltip("Lit sprite material. Optional.")]
        [SerializeField] private Material spriteMaterial;

        [SerializeField] private AudioCue glassCue;
        [SerializeField] private AudioCue crashCue;
        [SerializeField] private AudioCue carAlarmCue;

        private bool armed;
        private bool quakeStarted;
        private float elapsed;
        private Coroutine quake;
        private Transform fxRoot;

        private GameManager Gm => gameManager != null ? gameManager : GameManager.Instance;

        /// <summary>True while the street is shown and its quake has not started yet.</summary>
        public bool IsArmed => armed && !quakeStarted;

        private void OnEnable()
        {
            GameEvents.NodeEntered += OnNodeEntered;
            GameEvents.Rewound += OnRewound;
            GameEvents.Died += OnDied;
            GameEvents.BuildingCollapsedBehind += OnCollapsedBehind;
            SurvivalController.CollapseSpares = Spared;
        }

        private void OnDisable()
        {
            GameEvents.NodeEntered -= OnNodeEntered;
            GameEvents.Rewound -= OnRewound;
            GameEvents.Died -= OnDied;
            GameEvents.BuildingCollapsedBehind -= OnCollapsedBehind;
            if (SurvivalController.CollapseSpares == (Func<bool>)Spared) SurvivalController.CollapseSpares = null;
            StopQuake();
        }

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(GameManager manager, RoomSwitcher switcher, DebrisPresenter debrisPresenter, CameraShake shake, string roomId, string nodeId,
            Hazard facade, StreetPowerLine line, Sprite warning, Sprite[] rubble, Sprite shard, Material material, AudioCue glass, AudioCue crash, AudioCue carAlarm)
        {
            gameManager = manager;
            rooms = switcher;
            debris = debrisPresenter;
            cameraShake = shake;
            streetRoomId = roomId;
            streetNodeId = nodeId;
            facadeHazard = facade;
            powerLine = line;
            warningSprite = warning;
            rubbleSprites = rubble ?? new Sprite[0];
            shardSprite = shard;
            spriteMaterial = material;
            glassCue = glass;
            crashCue = crash;
            carAlarmCue = carAlarm;
        }

        private void OnNodeEntered(string nodeId)
        {
            if (nodeId == streetNodeId) Arm();
            else Disarm();
        }

        private void OnRewound(string nodeId)
        {
            StopQuake();
            GameManager gm = Gm;
            if (gm != null && gm.State.CurrentNodeId == streetNodeId) Arm();
        }

        private void OnDied(DeathInfo info) => StopQuake();

        private void Arm()
        {
            StopQuake();
            GameManager gm = Gm;
            armed = true;
            elapsed = 0f;
            // After a rewind into the street the aftershock (and its flag) come back with the checkpoint.
            quakeStarted = gm != null && gm.State.Flags.Get(FlagNames.StreetQuakeDone);
        }

        private void Disarm()
        {
            armed = false;
            StopQuake();
        }

        private Room Street()
        {
            Room room = rooms != null ? rooms.Current : null;
            return room != null && room.RoomId == streetRoomId ? room : null;
        }

        private Transform Player()
        {
            if (rooms == null) return null;
            foreach (FamilyMember m in rooms.Members) if (m != null && m.Type == MemberType.Player) return m.transform;
            return null;
        }

        private void Update()
        {
            GameManager gm = Gm;
            if (!armed || gm == null || gm.IsDead) return;
            Room room = Street();
            Transform player = Player();
            if (room == null || player == null) return;
            Vector2Int cell = room.CellOf(player.position);

            // The cut end of the line: walking into its sparks electrocutes.
            if (StreetLayout.IsSpark(cell))
            {
                gm.Die(powerLine != null ? powerLine.Death() : new DeathInfo { TextKey = UiKeys.StreetPowerDeath, TipId = "tip.electric", RewindTarget = RewindTarget.Checkpoint });
                return;
            }

            if (quakeStarted) return;
            elapsed += NavigationEvents.TimerDelta(Time.deltaTime);
            bool nearLine = cell.x <= StreetLayout.LineX + triggerCellsFromLine;
            if (elapsed >= quakeDelay || nearLine) quake = StartCoroutine(Quake(room));
        }

        private IEnumerator Quake(Room room)
        {
            quakeStarted = true;
            GameManager gm = Gm;
            GameState state = gm.State;
            state.QuakeCount = QuakeRules.CountAfter(state.QuakeCount, 4);
            var info = new QuakeInfo(3, quakeSeconds, magnitude, shakeScale, soundAct: 1, loudness: loudness);
            GameEvents.RaiseQuakeStarted(info);
            GameEvents.RaiseMessage(UiKeys.StreetDropOpen);
            AudioService.PlayCue(carAlarmCue);
            AudioService.PlayCue(glassCue);
            fxRoot = new GameObject("StreetQuakeFx").transform;
            fxRoot.SetParent(room.transform, false);
            StartCoroutine(GlassRain(room));

            int seed = DebrisPlanner.Hash(room.RoomId, "facade", state.DebrisSeed);
            int count = 3 + seed % 2;
            List<Vector2Int> drops = StreetLayout.PlanFacadeDrops(room.Nav, seed, Mathf.Min(count, dropStarts.Length), room.RequiredCells());
            var landed = new List<Vector2Int>();
            for (int i = 0; i < drops.Count; i++) StartCoroutine(FacadeDrop(room, drops[i], dropStarts[i], landed));

            yield return new WaitForSeconds(peakAt);
            if (gm.IsDead) yield break;
            Transform player = Player();
            if (player != null && !StreetLayout.InOpen(room.CellOf(player.position)))
            {
                gm.Die(new DeathInfo { Victim = MemberType.Player, TextKey = openDeathKey, TipId = streetTipId, RewindTarget = RewindTarget.Checkpoint });
                yield break;
            }

            GameEvents.RaiseMessage(UiKeys.StreetSafe);
            yield return new WaitForSeconds(Mathf.Max(0f, quakeSeconds - peakAt));
            if (gm.IsDead) yield break;
            GameEvents.RaiseQuakeEnded(info);
            StateOps.SetFlag(state, FlagNames.StreetQuakeDone, true);
            quake = null;
            yield return new WaitForSeconds(4f);
            AudioService.FadeOutCue(carAlarmCue, 3f);
        }

        private IEnumerator FacadeDrop(Room room, Vector2Int origin, float delay, List<Vector2Int> landed)
        {
            yield return new WaitForSeconds(delay);
            if (Gm == null || Gm.IsDead || fxRoot == null) yield break;

            // Red warning shadow growing on the 2x2 cells.
            var shadows = new List<SpriteRenderer>();
            foreach (Vector2Int c in StreetLayout.DropCells(origin))
            {
                SpriteRenderer s = MakeSprite(room, "Warning", warningSprite, IsoMath.GridToWorld(c.x + 0.5f, c.y + 0.5f), new Color(1f, 0.1f, 0.05f, 0f));
                s.sortingOrder = -28500;
                shadows.Add(s);
            }

            yield return Tween.Run(warnSeconds, k =>
            {
                foreach (SpriteRenderer s in shadows)
                {
                    if (s == null) continue;
                    s.color = new Color(1f, 0.1f, 0.05f, Mathf.Lerp(0.15f, 0.65f, k));
                    s.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 0.95f, k);
                }
            });
            foreach (SpriteRenderer s in shadows) if (s != null) Destroy(s.gameObject);
            if (Gm == null || Gm.IsDead || fxRoot == null) yield break;

            // Crash: rubble on the cells, then check who stood there.
            AudioService.PlayCue(crashCue);
            var cells = new List<Vector2Int>(StreetLayout.DropCells(origin));
            int i = 0;
            foreach (Vector2Int c in cells)
            {
                Sprite art = rubbleSprites != null && rubbleSprites.Length > 0 ? rubbleSprites[(origin.x + c.y + i++) % rubbleSprites.Length] : warningSprite;
                SpriteRenderer r = MakeSprite(room, "Rubble", art, IsoMath.GridToWorld(c.x + 0.5f, c.y + 0.5f), rubbleSprites != null && rubbleSprites.Length > 0 ? Color.white : new Color(0.45f, 0.42f, 0.38f, 1f));
                IsoSorter sorter = r.gameObject.AddComponent<IsoSorter>();
                sorter.Mode = IsoSortMode.Static;
                sorter.Apply();
            }

            landed.AddRange(cells);
            if (debris != null) debris.SetExtraBlocked(room, landed);
            HitMembers(room, cells);
        }

        private void HitMembers(Room room, List<Vector2Int> cells)
        {
            GameManager gm = Gm;
            if (gm == null || gm.IsDead || rooms == null) return;
            foreach (MemberType type in new[] { MemberType.Player, MemberType.Wife, MemberType.Child })
            {
                foreach (FamilyMember m in rooms.Members)
                {
                    if (m == null || m.Type != type || !cells.Contains(room.CellOf(m.transform.position))) continue;
                    gm.Die(new DeathInfo
                    {
                        Victim = type,
                        TextKey = facadeHazard != null ? facadeHazard.TextKeyFor(type).Key : openDeathKey,
                        TipId = facadeHazard != null && facadeHazard.Tip != null ? facadeHazard.Tip.Id : streetTipId,
                        RewindTarget = RewindTarget.Checkpoint
                    });
                    return;
                }
            }
        }

        private IEnumerator GlassRain(Room room)
        {
            for (int i = 0; i < 26; i++)
            {
                if (fxRoot == null) yield break;
                float gx = UnityEngine.Random.Range(0.5f, StreetLayout.Width - 0.5f);
                Vector3 from = IsoMath.GridToWorld(gx, StreetLayout.Height) + new Vector3(0f, UnityEngine.Random.Range(1.4f, 2.6f), 0f);
                Vector3 to = IsoMath.GridToWorld(gx, StreetLayout.Height - UnityEngine.Random.Range(0.3f, 1.8f));
                SpriteRenderer shard = MakeSprite(room, "Glass", shardSprite, from, new Color(0.75f, 0.9f, 1f, 0.9f));
                shard.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.05f, 0.11f);
                shard.sortingOrder = 2000;
                StartCoroutine(Fall(shard, from, to, UnityEngine.Random.Range(0.45f, 0.85f)));
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.04f, 0.16f));
            }
        }

        private static IEnumerator Fall(SpriteRenderer shard, Vector3 from, Vector3 to, float seconds)
        {
            Transform t = shard.transform;
            yield return Tween.Run(seconds, k =>
            {
                if (t == null) return;
                t.localPosition = Vector3.Lerp(from, to, k);
                Color c = shard.color;
                c.a = 0.9f * (1f - Mathf.Clamp01((k - 0.7f) / 0.3f));
                shard.color = c;
            }, Ease.InQuad);
            if (t != null) Destroy(t.gameObject);
        }

        private SpriteRenderer MakeSprite(Room room, string objectName, Sprite sprite, Vector3 localPosition, Color color)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(fxRoot != null ? fxRoot : room.transform, false);
            go.transform.localPosition = localPosition;
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            if (spriteMaterial != null) r.sharedMaterial = spriteMaterial;
            return r;
        }

        private void StopQuake()
        {
            StopAllCoroutines();
            quake = null;
            if (fxRoot != null) Destroy(fxRoot.gameObject);
            fxRoot = null;
            if (quakeStarted && !armed) quakeStarted = false;
            AudioService.FadeOutCue(carAlarmCue, 0.5f);
        }

        // Asked by SurvivalController at timer 0: out of the collapse's reach once far enough from the door.
        private bool Spared()
        {
            Room room = Street();
            Transform player = Player();
            return room != null && player != null && !StreetLayout.CollapseKills(room.CellOf(player.position));
        }

        private void OnCollapsedBehind()
        {
            Room room = Street();
            if (room == null) return;
            GameEvents.RaiseMessage(UiKeys.StreetCollapseBehind);
            AudioService.PlayCue(crashCue);
            if (cameraShake != null) cameraShake.Shake(2.5f, 0.35f);
            if (fxRoot == null)
            {
                fxRoot = new GameObject("StreetQuakeFx").transform;
                fxRoot.SetParent(room.transform, false);
            }

            // A huge dust cloud rolling out of the building's end of the street.
            for (int i = 0; i < 18; i++)
            {
                Vector3 at = IsoMath.GridToWorld(StreetLayout.DoorCell.x + UnityEngine.Random.Range(-1f, 1.5f), UnityEngine.Random.Range(0.5f, StreetLayout.Height - 0.5f));
                SpriteRenderer dust = MakeSprite(room, "CollapseDust", shardSprite, at, new Color(0.72f, 0.68f, 0.62f, 0.75f));
                dust.sortingOrder = 2500;
                StartCoroutine(Billow(dust));
            }
        }

        private static IEnumerator Billow(SpriteRenderer dust)
        {
            Transform t = dust.transform;
            Vector3 start = t.localPosition;
            Vector3 drift = new Vector3(UnityEngine.Random.Range(-1.6f, -0.4f), UnityEngine.Random.Range(0.1f, 0.8f), 0f);
            yield return Tween.Run(3.2f, k =>
            {
                if (t == null) return;
                t.localPosition = start + drift * k;
                t.localScale = Vector3.one * Mathf.Lerp(0.4f, 2.4f, k);
                Color c = dust.color;
                c.a = 0.75f * (1f - k);
                dust.color = c;
            }, Ease.OutQuad);
            if (t != null) Destroy(t.gameObject);
        }
    }
}
