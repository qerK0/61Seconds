using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>
    /// Marks a room child for the tile transition: Floor = a Grid with the floor Tilemap (animated per tile through
    /// sprite proxies), Walls = a container whose children (wall segments) move one by one. Untagged room children
    /// with renderers are props.
    /// </summary>
    public sealed class TransitionLayerTag : MonoBehaviour
    {
        [Tooltip("Floor (Tilemap grid) or Walls (container of wall segments).")]
        [SerializeField] private TransitionLayer layer = TransitionLayer.Floor;

        public TransitionLayer Layer => layer;

        /// <summary>Authoring helper for the scene builder.</summary>
        public void Configure(TransitionLayer value)
        {
            layer = value;
        }
    }
}
