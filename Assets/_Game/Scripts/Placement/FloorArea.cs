using System;
using System.Collections.Generic;
using SixtyOne.Iso;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// The walkable diamond of a room in grid cells: [0,Width]x[0,Height] plus extra cells (balconies).
    /// Pure logic: clamps a room-local world point onto the floor with a small margin from the walls.
    /// </summary>
    [Serializable]
    public struct FloorArea
    {
        public int Width;
        public int Height;
        public List<Vector2Int> ExtraCells;

        public FloorArea(int width, int height, List<Vector2Int> extraCells = null)
        {
            Width = width;
            Height = height;
            ExtraCells = extraCells ?? new List<Vector2Int>();
        }

        /// <summary>True if the grid point lies on the main rectangle or an extra cell.</summary>
        public bool ContainsGrid(Vector2 g)
        {
            if (g.x >= 0f && g.x <= Width && g.y >= 0f && g.y <= Height) return true;
            if (ExtraCells == null) return false;
            foreach (Vector2Int c in ExtraCells)
            {
                if (g.x >= c.x && g.x <= c.x + 1 && g.y >= c.y && g.y <= c.y + 1) return true;
            }

            return false;
        }

        /// <summary>Nearest grid point on the floor (inset by margin cells), main rectangle or an extra cell.</summary>
        public Vector2 ClampGrid(Vector2 g, float margin)
        {
            Vector2 best = Clamp(g, 0f, 0f, Width, Height, margin);
            float bestDist = (best - g).sqrMagnitude;
            if (ExtraCells != null)
            {
                foreach (Vector2Int c in ExtraCells)
                {
                    Vector2 p = Clamp(g, c.x, c.y, c.x + 1, c.y + 1, margin);
                    float d = (p - g).sqrMagnitude;
                    if (d < bestDist)
                    {
                        best = p;
                        bestDist = d;
                    }
                }
            }

            return best;
        }

        /// <summary>Clamps a room-local world point onto the floor.</summary>
        public Vector3 ClampLocal(Vector3 local, float margin)
        {
            Vector2 g = ClampGrid(IsoMath.WorldToGrid(local), margin);
            return IsoMath.GridToWorld(g.x, g.y);
        }

        private static Vector2 Clamp(Vector2 g, float minX, float minY, float maxX, float maxY, float margin)
        {
            float mx = Mathf.Min(margin, (maxX - minX) * 0.5f);
            float my = Mathf.Min(margin, (maxY - minY) * 0.5f);
            return new Vector2(Mathf.Clamp(g.x, minX + mx, maxX - mx), Mathf.Clamp(g.y, minY + my, maxY - my));
        }
    }
}
