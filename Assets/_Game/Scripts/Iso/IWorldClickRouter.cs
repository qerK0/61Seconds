using UnityEngine;

namespace SixtyOne.Iso
{
    /// <summary>A clickable the player walks up to before using it (doors, pickups, NPCs).</summary>
    public interface IApproachable
    {
        /// <summary>World point the player runs to (clamped onto the floor by the mover).</summary>
        Vector3 ApproachPoint { get; }
    }

    /// <summary>Gets told when the pointer starts / stops hovering it (IsoPicker).</summary>
    public interface IHoverable
    {
        void SetHovered(bool hovered);
    }

    /// <summary>
    /// Takes over a world click (after the panic delay): e.g. run to the target first, or run to an empty floor
    /// point. Returns false to let IsoPicker click the target directly (placement, anchors).
    /// </summary>
    public interface IWorldClickRouter
    {
        /// <summary>True while empty-floor clicks mean something (exploration).</summary>
        bool AcceptsFloorClicks { get; }

        bool Route(IClickable target, Vector3 worldPoint);
    }
}
