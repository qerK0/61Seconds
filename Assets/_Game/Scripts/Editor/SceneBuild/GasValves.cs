using System.Collections.Generic;
using SixtyOne.Building;
using SixtyOne.Iso;
using SixtyOne.Placement;
using SixtyOne.World;
using UnityEngine;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Scene-builder step for the gas leaks: every LevelConfig gas zone gets a green haze over each of its rooms and
    /// a GasValveView on its valve hotspot (handle turn, creak, hiss loop, haze fade; all read from the valve flag).
    /// </summary>
    public static class GasValves
    {
        private static readonly Color Haze = new Color(0.45f, 0.85f, 0.35f, 0f);

        public static void Build(List<BuildingLayout.RoomSpec> specs, List<Room> rooms, BuildingAssets data)
        {
            if (data.Level == null) return;
            foreach (GasZone zone in data.Level.GasZones)
            {
                var zoneRooms = new List<Room>();
                var hazes = new List<SpriteRenderer>();
                for (int i = 0; i < specs.Count; i++)
                {
                    if ((specs[i].Zone ?? specs[i].Id) != zone.RoomId) continue;
                    zoneRooms.Add(rooms[i]);
                    hazes.Add(HazeOver(rooms[i].transform, specs[i]));
                }

                Transform valve = null;
                foreach (Room room in zoneRooms)
                {
                    Transform t = room.transform.Find("Hotspot_Valve");
                    if (t != null) valve = t;
                }

                if (valve == null)
                {
                    Debug.LogWarning($"[GasValves] zone '{zone.RoomId}' has no valve hotspot.");
                    continue;
                }

                Transform body = valve.Find("Body");
                SpriteRenderer art = body != null ? body.GetComponent<SpriteRenderer>() : null;
                float height = art != null && art.sprite != null ? art.sprite.bounds.size.y * body.localScale.y : 0.25f;
                valve.gameObject.AddComponent<GasValveView>().Configure(zone.ValveFlag, body, height, hazes.ToArray(), zoneRooms.ToArray(), data.GasHissCue, data.ValveCreakCue);
            }
        }

        /// <summary>A soft green haze over the whole floor (above props and characters; alpha driven at runtime).</summary>
        private static SpriteRenderer HazeOver(Transform room, BuildingLayout.RoomSpec spec)
        {
            var go = new GameObject("GasHaze");
            go.transform.SetParent(room, false);
            go.transform.localPosition = IsoMath.GridToWorld(spec.Width * 0.5f, spec.Height * 0.5f) + new Vector3(0f, 0.35f, 0f);
            float size = (spec.Width + spec.Height) * 0.5f * 1.25f;
            go.transform.localScale = new Vector3(size, size * 1.3f, 1f);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = SceneKit.Art.Glow;
            r.color = Haze;
            r.sortingOrder = SceneKit.LabelOrder - 100;
            r.enabled = false;
            return r;
        }
    }
}
