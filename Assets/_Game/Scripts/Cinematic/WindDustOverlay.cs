using SixtyOne.Core;
using SixtyOne.Placement;
using UnityEngine;

namespace SixtyOne.Cinematic
{
    /// <summary>
    /// EXPERIMENTAL, self-contained: wind-blown dust in front of the scene. Two particle systems it builds itself
    /// (fine warm-grey specks + a few large soft, out-of-focus motes) drift mostly left → right with a slight fall,
    /// in camera space (it follows the camera, so it works in every room), sorted above the world and below the UI
    /// (the UI is a screen-space overlay). Gusts every 4–8 s raise speed and density for ~1.5 s. Denser in the
    /// street; Reduce Effects cuts the density by 60 %. Unscaled time; stops while the pause menu is open. No
    /// colliders, no raycast targets. Nothing else references it: remove the FX_WindDust object (or the scene
    /// builder's WindDust flag) and this file to take it out.
    /// </summary>
    public sealed class WindDustOverlay : MonoBehaviour
    {
        [Tooltip("Camera to follow (Camera.main when empty).")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("Room id where the dust is denser.")]
        [SerializeField] private string streetRoomId = "street";

        [SerializeField] private float speckRate = 22f;
        [SerializeField] private float moteRate = 1.6f;
        [SerializeField] private float drift = 0.55f;
        [SerializeField] private float streetDensity = 1.7f;
        [SerializeField] private float reduceEffectsDensity = 0.4f;
        [SerializeField] private Vector2 gustGap = new Vector2(4f, 8f);
        [SerializeField] private float gustSeconds = 1.5f;
        [SerializeField] private float gustBoost = 2.4f;

        [Tooltip("Sorting order above every world sprite (iso orders stay within ±30000).")]
        [SerializeField] private int sortingOrder = 32000;

        /// <summary>Ortho size the sizes and speeds are tuned at (rooms frame around it).</summary>
        private const float ReferenceOrtho = 2.4f;

        private static readonly Vector2 SpeckSize = new Vector2(0.012f, 0.03f);
        private static readonly Vector2 MoteSize = new Vector2(0.12f, 0.26f);

        private ParticleSystem specks;
        private ParticleSystem motes;
        private RoomSwitcher rooms;
        private float nextGust;
        private float gustStart = -100f;
        private bool paused;

        private void Awake()
        {
            Texture2D sharp = Dot(32, 1.6f);
            Texture2D soft = Dot(64, 0.7f);
            specks = Build("Specks", sharp, 170, SpeckSize, new Vector2(0.15f, 0.32f), new Vector2(7f, 11f));
            motes = Build("Motes", soft, 30, MoteSize, new Vector2(0.12f, 0.22f), new Vector2(9f, 14f));
            nextGust = Time.unscaledTime + Random.Range(gustGap.x, gustGap.y);
        }

        private ParticleSystem Build(string name, Texture2D texture, int max, Vector2 size, Vector2 alpha, Vector2 life)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.useUnscaledTime = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = max;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life.x, life.y);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
            Color warm = new Color(0.82f, 0.76f, 0.66f, 1f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(warm.r, warm.g, warm.b, alpha.x), new Color(warm.r, warm.g, warm.b, alpha.y));

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;

            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(drift * 0.6f, drift * 1.3f);
            velocity.y = new ParticleSystem.MinMaxCurve(-drift * 0.35f, -drift * 0.05f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.12f;
            noise.frequency = 0.35f;

            ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            renderer.sharedMaterial = new Material(shader) { mainTexture = texture };
            renderer.sortingOrder = sortingOrder;
            ps.Play();
            return ps;
        }

        /// <summary>A white radial dot; a lower sharpness = a softer, out-of-focus edge.</summary>
        private static Texture2D Dot(int size, float sharpness)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Pow(a, 1f / sharpness));
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private void LateUpdate()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null || specks == null) return;

            // Pause menu: freeze in place (unscaled time would keep it moving).
            if (PauseState.IsPaused != paused)
            {
                paused = PauseState.IsPaused;
                foreach (ParticleSystem ps in new[] { specks, motes })
                {
                    if (paused) ps.Pause(true);
                    else ps.Play(true);
                }
            }

            if (paused) return;

            // Camera space: the box covers the view (plus a margin upwind), just in front of the camera.
            float half = cam.orthographicSize;
            float k = half / ReferenceOrtho;
            transform.position = cam.transform.position + cam.transform.forward * 5f;
            transform.rotation = cam.transform.rotation;
            var box = new Vector3(half * cam.aspect * 2.6f, half * 2.2f, 0f);

            float now = Time.unscaledTime;
            if (now >= nextGust)
            {
                gustStart = now;
                nextGust = now + gustSeconds + Random.Range(gustGap.x, gustGap.y);
            }

            float g = Mathf.Clamp01((now - gustStart) / gustSeconds);
            float gust = 1f + (gustBoost - 1f) * Mathf.Sin(g * Mathf.PI) * (g < 1f ? 1f : 0f);

            float density = 1f;
            if (rooms == null) rooms = FindAnyObjectByType<RoomSwitcher>();
            if (rooms != null && rooms.Current != null && rooms.Current.RoomId == streetRoomId) density *= streetDensity;
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.Settings != null && gm.Settings.ReduceEffects) density *= reduceEffectsDensity;

            Apply(specks, box, k, speckRate * density * gust, gust, SpeckSize);
            Apply(motes, box, k, moteRate * density * gust, gust, MoteSize);
        }

        private static void Apply(ParticleSystem ps, Vector3 box, float k, float rate, float gust, Vector2 size)
        {
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.scale = box;
            ParticleSystem.MainModule main = ps.main;
            main.startSize = new ParticleSystem.MinMaxCurve(size.x * k, size.y * k);
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;
            ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
            velocity.speedModifier = gust * k;
        }
    }
}
