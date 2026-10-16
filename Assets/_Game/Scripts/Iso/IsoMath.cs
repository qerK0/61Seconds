using UnityEngine;

namespace SixtyOne.Iso
{
    /// <summary>2:1 diamond isometric grid helpers.</summary>
    public static class IsoMath
    {
        /// <summary>Grid cell (x, y) to world position for tiles of the given world width (height = width / 2).</summary>
        public static Vector3 GridToWorld(float x, float y, float tileWidth = 1f)
        {
            float halfW = tileWidth * 0.5f;
            float halfH = tileWidth * 0.25f;
            return new Vector3((x - y) * halfW, (x + y) * halfH, 0f);
        }

        /// <summary>World position to (fractional) grid cell.</summary>
        public static Vector2 WorldToGrid(Vector3 world, float tileWidth = 1f)
        {
            float halfW = tileWidth * 0.5f;
            float halfH = tileWidth * 0.25f;
            float a = world.x / halfW;
            float b = world.y / halfH;
            return new Vector2((a + b) * 0.5f, (b - a) * 0.5f);
        }

        /// <summary>sortingOrder for a world Y: lower Y draws in front.</summary>
        public static int SortingOrderForY(float y, float precision = 100f, int offset = 0)
        {
            int order = Mathf.RoundToInt(-y * precision) + offset;
            return Mathf.Clamp(order, short.MinValue, short.MaxValue);
        }
    }
}
