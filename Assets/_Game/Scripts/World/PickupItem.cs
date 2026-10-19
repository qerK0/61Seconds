using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// WorldInteractable preset for items lying on an open surface: 1 second, one-shot, disappears when taken.
    /// Set Grants Item; for items inside a drawer/cabinet use a plain WorldInteractable with a 3s cost.
    /// </summary>
    public sealed class PickupItem : WorldInteractable
    {
        protected override void Reset()
        {
            base.Reset();
            ApplyPreset(1f, isOneShot: true, hide: true);
        }
    }
}
