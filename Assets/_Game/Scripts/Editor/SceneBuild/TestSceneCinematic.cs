using System.Collections.Generic;
using SixtyOne.Cinematic;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Prep;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SixtyOne.Editor
{
    /// <summary>Adds the cinematic layer to the test scene: global volume, Light2Ds, dust and the director.</summary>
    public static class TestSceneCinematic
    {
        private const int DustOrder = 31000;

        /// <summary>A warm practical light (lamp, window) placed in a room.</summary>
        public static Light2D PracticalLight(Transform room, string lightName, float gx, float gy, float lift, Color color, float radius)
        {
            var go = new GameObject("Light_" + lightName);
            go.transform.SetParent(room, false);
            go.transform.localPosition = IsoMath.GridToWorld(gx, gy) + new Vector3(0f, lift, 0f);
            Light2D light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            light.color = color;
            light.intensity = 1.1f;
            light.pointLightInnerRadius = radius * 0.15f;
            light.pointLightOuterRadius = radius;
            light.falloffIntensity = 0.6f;
            return light;
        }

        public static CinematicDirector Build(CinematicAssets assets, Camera camera, GameManager gameManager, NodePlayer nodePlayer,
            PrepController prep, LetterboxBars letterbox, Transform player, List<Light2D> practicals)
        {
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var root = new GameObject("Cinematic");

            Volume volume = root.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.weight = 1f;
            volume.sharedProfile = assets.BaseVolume;

            var globalGo = new GameObject("GlobalLight2D");
            globalGo.transform.SetParent(root.transform, false);
            Light2D global = globalGo.AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            global.intensity = assets.ActProfiles[0].GlobalLightIntensity;
            global.color = assets.ActProfiles[0].GlobalLightColor;

            var flashGo = new GameObject("FlashlightLight2D");
            flashGo.transform.SetParent(root.transform, false);
            Light2D flashlight = flashGo.AddComponent<Light2D>();
            flashlight.lightType = Light2D.LightType.Point;
            flashlight.color = new Color(1f, 0.95f, 0.8f, 1f);
            flashlight.intensity = 1.2f;
            flashlight.pointLightInnerRadius = 0.3f;
            flashlight.pointLightOuterRadius = 2.4f;
            flashlight.falloffIntensity = 0.7f;
            flashlight.enabled = false;

            ParticleSystem dust = BuildDust(root.transform, assets.DustMaterial);

            CinematicDirector director = root.AddComponent<CinematicDirector>();
            SerializedWire.SetArray(director, "actProfiles", assets.ActProfiles);
            SerializedWire.Set(director, "volume", volume);
            SerializedWire.Set(director, "targetCamera", camera);
            SerializedWire.Set(director, "gameManager", gameManager);
            SerializedWire.Set(director, "nodePlayer", nodePlayer);
            SerializedWire.Set(director, "prep", prep);
            SerializedWire.Set(director, "globalLight", global);
            SerializedWire.SetArray(director, "practicalLights", practicals.ToArray());
            SerializedWire.Set(director, "flashlight", flashlight);
            SerializedWire.Set(director, "player", player);
            SerializedWire.Set(director, "dust", dust);
            SerializedWire.Set(director, "letterbox", letterbox);
            return director;
        }

        private static ParticleSystem BuildDust(Transform parent, Material material)
        {
            var go = new GameObject("QuakeDust");
            go.transform.SetParent(parent, false);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.useUnscaledTime = true;
            main.duration = 1f;
            main.maxParticles = 400;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
            main.startColor = new Color(0.78f, 0.72f, 0.62f, 0.45f);
            main.gravityModifier = 0.12f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(9f, 3f, 0.1f);

            ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingOrder = DustOrder;
            return ps;
        }
    }
}
