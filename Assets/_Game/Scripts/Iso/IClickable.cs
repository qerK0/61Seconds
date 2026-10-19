namespace SixtyOne.Iso
{
    /// <summary>A world object IsoPicker can click (family members, anchors, pickups).</summary>
    public interface IClickable
    {
        /// <summary>False to make the picker ignore this object right now.</summary>
        bool IsClickable { get; }

        /// <summary>Called by IsoPicker when this is the top-most clickable under the pointer.</summary>
        void OnClicked();
    }
}
