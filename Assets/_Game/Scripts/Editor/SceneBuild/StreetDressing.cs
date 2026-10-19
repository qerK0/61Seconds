using System.Collections.Generic;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Placement;
using SixtyOne.Text;
using SixtyOne.World;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Dresses the street finale (20 x 6, heading left) with every asset we have, all decorative (the nav, the quake,
    /// the façade drops, the power line and the sign keep working as before):
    /// - collapsed façades (enkaz son 2 / 3) along the back edge, alternating, flipped, 0.9–1.1 scale, darker further back;
    /// - rubble mounds (enkaz son 1), small debris (enkaz 1 / 2), broken low walls;
    /// - furniture thrown out of the buildings (a toppled wardrobe, a mattress, a sofa, a chair on its side, a broken
    ///   table, a bookshelf face-down) on the kerbs;
    /// - three street lamps (dead, flickering, swinging), the door art on our building, dark cars with a blinking
    ///   orange alarm light;
    /// - a darkened floor with a lighter walkway, dust / fog layers and falling ash, a fire glowing behind a façade
    ///   (sparks + smoke), red / blue emergency lights from off-screen left;
    /// - the hopeful end: a green ACİL TOPLANMA ALANI board, a warm light and survivors wrapped in blankets.
    /// StreetAmbience animates the lights and plays the street sounds.
    /// </summary>
    public static class StreetDressing
    {
        private static readonly Color AfadGreen = new Color32(0x1E, 0x8E, 0x3E, 0xFF);
        private static readonly Color Concrete = new Color(0.36f, 0.35f, 0.34f, 1f);
        private const int Seed = 1061;

        public static void Build(Room street, BuildingLayout.RoomSpec spec, BuildingAssets data, Transform player, RoomSwitcher rooms, GameManager gameManager)
        {
            Transform root = new GameObject("StreetDressing").transform;
            root.SetParent(street.transform, false);
            var rng = new System.Random(Seed);
            PropArtSet props = SceneKit.Props;
            float w = spec.Width, h = spec.Height;

            DarkenFloor(street.transform, spec);

            // Collapsed façades along the back edge: alternating art, flips, scale variation, darker further back.
            Sprite ruinA = props != null ? props.Find("FacadeRuinA") : null;
            Sprite ruinB = props != null ? props.Find("FacadeRuinB") : null;
            int i = 0;
            for (float x = 0.6f; x < w + 0.5f; x += 2.3f, i++)
            {
                float back = (i % 3) * 0.35f;
                float shade = Mathf.Lerp(0.62f, 0.38f, back / 0.7f);
                Sprite art = i % 2 == 0 ? ruinA : ruinB;
                float scale = 0.9f + (float)rng.NextDouble() * 0.2f;
                Piece(root, "Ruin", art, x + (float)rng.NextDouble() * 0.4f, h + 0.55f + back, scale, i % 3 == 1, new Color(shade, shade, shade * 1.05f, 1f), 0f, -40 - i % 3);
            }

            // Rubble mounds along the kerbs (never on the walkway).
            Sprite mound = props != null ? props.Find("RubbleMound") : null;
            foreach (Vector3 m in new[] { new Vector3(2.4f, h + 0.15f, 1.0f), new Vector3(8.2f, h + 0.05f, 0.9f), new Vector3(13.6f, h + 0.2f, 1.1f), new Vector3(17.2f, -0.45f, 0.85f) })
            {
                Piece(root, "Mound", mound, m.x, m.y, m.z, m.x > 10f, new Color(0.6f, 0.58f, 0.56f, 1f), 0f, 0);
            }

            // Small debris scattered on both kerbs (seeded).
            Sprite[] small = props != null ? props.Debris : new Sprite[0];
            for (int d = 0; d < 18 && small.Length > 0; d++)
            {
                bool backKerb = d % 2 == 0;
                float x = (float)rng.NextDouble() * w;
                float y = backKerb ? h - 0.15f + (float)rng.NextDouble() * 0.4f : -0.55f + (float)rng.NextDouble() * 0.35f;
                Piece(root, "Debris", small[d % small.Length], x, y, 0.7f + (float)rng.NextDouble() * 0.6f, rng.Next(2) == 0, new Color(0.62f, 0.6f, 0.57f, 1f), (float)rng.NextDouble() * 30f - 15f, 0);
            }

            // Broken low walls (cracked concrete stubs) on the front kerb.
            foreach (float x in new[] { 5.5f, 11.8f, 15.2f })
            {
                SpriteRenderer wall = Piece(root, "BrokenWall", SceneKit.Art.Block, x, -0.6f, 1f, false, Concrete, 0f, 0);
                wall.transform.localScale = new Vector3(0.9f, 0.32f, 1f);
                if (small.Length > 0) Piece(root, "WallDebris", small[0], x + 0.2f, -0.5f, 0.6f, false, new Color(0.55f, 0.53f, 0.5f, 1f), 0f, 1);
            }

            // Furniture thrown out of the buildings.
            Thrown(root, props, "Wardrobe", 4.2f, h + 0.05f, -78f, 0.9f);
            Thrown(root, props, "Bed", 9.2f, -0.45f, 6f, 1.3f);
            Thrown(root, props, "Sofa", 14.6f, h + 0.1f, -4f, 1.1f);
            Thrown(root, props, "Chair", 7.0f, -0.5f, 88f, 1f);
            Thrown(root, props, "Table", 17.8f, h + 0.1f, 14f, 1.1f);
            Thrown(root, props, "Bookshelf", 11.2f, h + 0.1f, 92f, 0.8f);

            // The sinkhole (FloorHole) is a solid prop in the layout; our building's door gets the door art.
            Transform door = street.transform.Find("BuildingDoor");
            if (door != null && door.TryGetComponent(out SpriteRenderer doorRenderer)) DoorArt.Apply(doorRenderer, DoorArt.Door, true);

            var flicker = new List<Light2D>();
            var swing = new List<Transform>();
            Lamp(root, 4.5f, h + 0.15f, false, null, null);
            Lamp(root, 11.5f, h + 0.15f, true, flicker, null);
            Lamp(root, 16.5f, h + 0.15f, true, null, swing);

            // Cars: dark, one with a blinking orange alarm light.
            var blink = new List<Light2D>();
            foreach (Transform child in street.transform)
            {
                if (!child.name.StartsWith("Car") || !child.TryGetComponent(out SpriteRenderer car)) continue;
                car.color = car.color * 0.42f + new Color(0f, 0f, 0.02f, 0.58f);
            }

            Vector2Int carCell = StreetLayout.CarCells[0];
            blink.Add(Light(root, "CarAlarm", carCell.x + 1f, carCell.y + 0.5f, 0.4f, new Color(1f, 0.55f, 0.1f, 1f), 2.6f, 1.8f));

            // The fire behind a façade: an orange flickering glow, sparks and smoke rising.
            var fire = new List<Light2D> { Light(root, "Fire", 6.8f, h + 1.2f, 0.6f, new Color(1f, 0.45f, 0.1f, 1f), 3.8f, 4.2f) };
            Particles(root, "FireSparks", 6.8f, h + 1.1f, 0.5f, data, Sparks);
            Particles(root, "FireSmoke", 6.8f, h + 1.3f, 1.4f, data, Smoke);

            // Dust / fog (two slow layers) and falling ash over the whole street.
            Particles(root, "FogFar", w * 0.5f, h * 0.7f, 0.6f, data, FogFar);
            Particles(root, "FogNear", w * 0.5f, h * 0.3f, 0.3f, data, FogNear);
            Particles(root, "Ash", w * 0.5f, h * 0.5f, 2.6f, data, Ash);

            // Help is that way: red / blue emergency lights pulsing from off-screen left.
            var red = new List<Light2D> { Light(root, "EmergencyRed", -2.2f, 1.6f, 0.6f, new Color(1f, 0.1f, 0.08f, 1f), 3.2f, 7f) };
            var blue = new List<Light2D> { Light(root, "EmergencyBlue", -2.0f, 4.4f, 0.6f, new Color(0.15f, 0.35f, 1f, 1f), 3.2f, 7f) };

            BuildAssemblyArea(root, spec);

            StreetAmbience ambience = root.gameObject.AddComponent<StreetAmbience>();
            SerializedWire.Set(ambience, "rooms", rooms);
            SerializedWire.Set(ambience, "gameManager", gameManager);
            SerializedWire.Set(ambience, "player", player);
            SerializedWire.SetArray(ambience, "flicker", flicker.ToArray());
            SerializedWire.SetArray(ambience, "fire", fire.ToArray());
            SerializedWire.SetArray(ambience, "blink", blink.ToArray());
            SerializedWire.SetArray(ambience, "emergencyRed", red.ToArray());
            SerializedWire.SetArray(ambience, "emergencyBlue", blue.ToArray());
            SerializedWire.SetArray(ambience, "swing", swing.ToArray());
            SerializedWire.Set(ambience, "wind", data.WindCue);
            SerializedWire.Set(ambience, "sirens", data.SirensCue);
            SerializedWire.Set(ambience, "ambulance", data.AmbulanceCue);
            SerializedWire.SetArray(ambience, "oneShots", new Object[] { data.DogBarkCue, data.MetalGroanCue });
            EditorUtility.SetDirty(ambience);
        }

        /// <summary>Rocky floor darkened, the walkway rows a little lighter so the path reads.</summary>
        private static void DarkenFloor(Transform street, BuildingLayout.RoomSpec spec)
        {
            Tilemap map = street.GetComponentInChildren<Tilemap>(true);
            if (map == null) return;
            map.color = map.color * 0.8f + new Color(0f, 0f, 0.03f, 0.2f);
            for (int x = 0; x < spec.Width; x++)
            {
                for (int y = 2; y <= 3; y++)
                {
                    var cell = new Vector3Int(x, y, 0);
                    if (!map.HasTile(cell)) continue;
                    map.SetTileFlags(cell, TileFlags.None);
                    map.SetColor(cell, new Color(1.22f, 1.2f, 1.15f, 1f));
                }
            }
        }

        /// <summary>The assembly area at the far left: green board, warm light, survivors in blankets.</summary>
        private static void BuildAssemblyArea(Transform root, BuildingLayout.RoomSpec spec)
        {
            Vector2Int sign = StreetLayout.SignCell;
            // The clickable sign hotspot keeps its collider; its block body becomes the board's thin pole.
            Transform post = root.parent.Find("Hotspot_Sign/Body");
            if (post != null) post.localScale = new Vector3(0.1f, 1.55f, 1f);
            // On top of the clickable sign post (the hotspot body).
            Vector3 at = IsoMath.GridToWorld(sign.x + 1.0f, sign.y + 1.0f) + new Vector3(0.3f, 0f, 0f);
            var board = new GameObject("AssemblySign").transform;
            board.SetParent(root, false);
            board.localPosition = at;
            SpriteRenderer panel = Sprite(board, "Panel", SceneKit.Art.Rounded, new Vector3(0f, 1.75f, 0f), AfadGreen);
            panel.drawMode = SpriteDrawMode.Sliced;
            panel.size = new Vector2(1.5f, 0.42f);
            SpriteRenderer rim = Sprite(board, "Rim", SceneKit.Art.Rounded, new Vector3(0f, 1.75f, 0f), Color.white);
            rim.drawMode = SpriteDrawMode.Sliced;
            rim.size = new Vector2(1.56f, 0.48f);
            rim.sortingOrder = panel.sortingOrder - 1;
            TextMeshPro text = SceneKit.WorldLabel(board, new Vector3(0f, 1.75f, 0f), 1.6f, Color.white, bold: true);
            text.rectTransform.sizeDelta = new Vector2(1.45f, 0.4f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            // Inter Bold carries the Turkish İ (the default world-label font does not).
            SixtyOne.UI.UITheme theme = FontAssetsGenerator.LoadTheme();
            if (theme != null && theme.Bold != null) text.font = theme.Bold;
            text.alignment = TextAlignmentOptions.Center;
            SerializedWire.SetTextKey(text.gameObject.AddComponent<LocalizedText>(), "key", "street.sign");
            board.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
            panel.sortingOrder = 1;
            rim.sortingOrder = 0;
            text.sortingOrder = 2;
            var sorter = board.gameObject.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            sorter.Apply();

            Light(root, "AssemblyWarm", sign.x + 1.2f, sign.y + 0.8f, 1.2f, new Color(1f, 0.74f, 0.42f, 1f), 2.6f, 4.2f);

            // Survivors: dark silhouettes standing still, wrapped in blankets.
            Vector2[] spots = { new Vector2(0.45f, 5.3f), new Vector2(1.15f, 5.6f), new Vector2(0.4f, 4.6f), new Vector2(1.9f, 5.75f) };
            for (int s = 0; s < spots.Length; s++)
            {
                var person = new GameObject("Survivor" + s).transform;
                person.SetParent(root, false);
                person.localPosition = IsoMath.GridToWorld(spots[s].x, spots[s].y);
                SpriteRenderer body = Sprite(person, "Body", SceneKit.Art.Character, Vector3.zero, new Color(0.07f, 0.075f, 0.09f, 1f));
                float height = CharacterArt.PlayerHeight * (s == 3 ? 0.6f : 0.95f + 0.05f * s);
                float k = height / Mathf.Max(0.01f, SceneKit.Art.Character.bounds.size.y);
                body.transform.localScale = Vector3.one * k;
                SpriteRenderer blanket = Sprite(person, "Blanket", SceneKit.Art.Rounded, new Vector3(0f, height * 0.62f, 0f), new Color(0.35f, 0.22f, 0.14f, 1f) * (0.8f + 0.1f * s));
                blanket.drawMode = SpriteDrawMode.Sliced;
                blanket.size = new Vector2(height * 0.42f, height * 0.5f);
                blanket.sortingOrder = 1;
                person.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
                var sort = person.gameObject.AddComponent<IsoSorter>();
                sort.Mode = IsoSortMode.Static;
                sort.Apply();
            }
        }

        private static void Thrown(Transform root, PropArtSet props, string name, float gx, float gy, float angle, float scale)
        {
            Sprite art = props != null ? props.Find(name) : null;
            if (art == null) return;
            Piece(root, "Thrown_" + name, art, gx, gy, scale, angle > 45f, new Color(0.42f, 0.4f, 0.4f, 1f), angle, 0);
        }

        /// <summary>A street lamp: pole + lamp head (lamba.png); lit lamps get a Light2D, swinging ones swing from the top.</summary>
        private static void Lamp(Transform root, float gx, float gy, bool lit, List<Light2D> flicker, List<Transform> swing)
        {
            var lamp = new GameObject("StreetLamp").transform;
            lamp.SetParent(root, false);
            lamp.localPosition = IsoMath.GridToWorld(gx, gy);
            SpriteRenderer pole = Sprite(lamp, "Pole", SceneKit.Art.Block, Vector3.zero, new Color(0.16f, 0.17f, 0.18f, 1f));
            pole.transform.localScale = new Vector3(0.05f, 1.9f, 1f);
            var pivot = new GameObject("Head").transform;
            pivot.SetParent(lamp, false);
            pivot.localPosition = new Vector3(0f, 1.9f, 0f);
            Sprite head = SceneKit.Props != null ? SceneKit.Props.Find("WallLamp") : null;
            SpriteRenderer h = Sprite(pivot, "Lamp", head != null ? head : SceneKit.Art.Dot, new Vector3(0.08f, -0.25f, 0f), lit ? Color.white : new Color(0.45f, 0.45f, 0.45f, 1f));
            h.transform.localScale = Vector3.one * 1.3f;
            if (lit)
            {
                Light2D light = Light(pivot, "Light", 0f, 0f, 0f, new Color(1f, 0.8f, 0.5f, 1f), 2.4f, 3f);
                light.transform.localPosition = new Vector3(0.08f, -0.3f, 0f);
                flicker?.Add(light);
            }

            swing?.Add(pivot);
            lamp.gameObject.AddComponent<UnityEngine.Rendering.SortingGroup>();
            var sorter = lamp.gameObject.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            sorter.Apply();
        }

        /// <summary>A static decorative sprite on the street (lit material, iso-sorted, nudged by `nudge`).</summary>
        private static SpriteRenderer Piece(Transform root, string name, Sprite sprite, float gx, float gy, float scale, bool flip, Color tint, float angle, int nudge)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy);
            go.transform.localScale = Vector3.one * scale;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite != null ? sprite : SceneKit.Art.Block;
            r.color = tint;
            r.flipX = flip;
            if (RoomTiles.LitMaterial != null) r.sharedMaterial = RoomTiles.LitMaterial;
            IsoSorter sorter = go.AddComponent<IsoSorter>();
            sorter.Mode = IsoSortMode.Static;
            SerializedWire.SetInt(sorter, "offset", nudge);
            sorter.Apply();
            return r;
        }

        private static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Vector3 local, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sprite = sprite;
            r.color = color;
            if (RoomTiles.LitMaterial != null) r.sharedMaterial = RoomTiles.LitMaterial;
            return r;
        }

        private static Light2D Light(Transform root, string name, float gx, float gy, float lift, Color color, float intensity, float radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy) + new Vector3(0f, lift, 0f);
            Light2D light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightInnerRadius = radius * 0.15f;
            light.pointLightOuterRadius = radius;
            light.falloffIntensity = 0.6f;
            light.shadowsEnabled = false;
            return light;
        }

        // ---------- particles ----------

        private delegate void ParticleStyle(ParticleSystem ps, ParticleSystemRenderer renderer);

        private static void Particles(Transform root, string name, float gx, float gy, float lift, BuildingAssets data, ParticleStyle style)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy) + new Vector3(0f, lift, 0f);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = HazeMaterial();
            renderer.sortingOrder = SceneKit.LabelOrder - 200;
            ParticleSystem.MainModule main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.playOnAwake = true;
            main.loop = true;
            main.prewarm = true;
            style(ps, renderer);
        }

        private static Material haze;

        /// <summary>Soft white particle material (ph_glow) shared by the fog, ash, smoke and sparks.</summary>
        private static Material HazeMaterial()
        {
            if (haze != null) return haze;
            const string path = "Assets/_Game/Data/Cinematic/StreetHaze.mat";
            haze = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (haze == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                haze = new Material(shader);
                AssetDatabase.CreateAsset(haze, path);
            }

            haze.mainTexture = SceneKit.Art.Glow.texture;
            EditorUtility.SetDirty(haze);
            return haze;
        }

        private static void Fog(ParticleSystem ps, float alpha, float size, float speed, Vector3 box, float rate)
        {
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 26f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.7f, size * 1.3f);
            main.startColor = new Color(0.7f, 0.68f, 0.64f, alpha);
            main.maxParticles = 60;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;
            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-speed * 1.3f, -speed * 0.7f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
        }

        private static void FogFar(ParticleSystem ps, ParticleSystemRenderer r) => Fog(ps, 0.07f, 4.5f, 0.12f, new Vector3(13f, 2.2f, 0f), 1.6f);

        private static void FogNear(ParticleSystem ps, ParticleSystemRenderer r)
        {
            Fog(ps, 0.05f, 3.2f, 0.22f, new Vector3(13f, 1.6f, 0f), 1.4f);
            r.sortingOrder = SceneKit.LabelOrder - 150;
        }

        private static void Ash(ParticleSystem ps, ParticleSystemRenderer r)
        {
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 9f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
            main.startColor = new Color(0.75f, 0.73f, 0.7f, 0.55f);
            main.maxParticles = 220;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 20f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(14f, 0.5f, 0f);
            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.05f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.35f, -0.2f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 0.4f;
        }

        private static void Sparks(ParticleSystem ps, ParticleSystemRenderer r)
        {
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.3f, 1f), new Color(1f, 0.45f, 0.1f, 1f));
            main.maxParticles = 80;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 14f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.9f, 0.1f, 0f);
            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            r.sortingOrder = SceneKit.LabelOrder - 250;
        }

        private static void Smoke(ParticleSystem ps, ParticleSystemRenderer r)
        {
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = 7f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startColor = new Color(0.16f, 0.15f, 0.15f, 0.35f);
            main.maxParticles = 40;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 3f;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.8f, 0.1f, 0f);
            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.25f, -0.1f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.6f));
            r.sortingOrder = SceneKit.LabelOrder - 260;
        }
    }
}
