using UnityEngine;

namespace SixtyOne.Prep
{
    /// <summary>
    /// PrepInteractable preset for items lying on an open surface: 1 second, one-shot, disappears when taken.
    /// Set Grants Item; for items inside a drawer/cabinet use a plain PrepInteractable with a 3s cost.
    /// </summary>
    public sealed class PickupItem : PrepInteractable
    {
        protected override void Reset()
        {
            base.Reset();
            ApplyPreset(1f, isOneShot: true, hide: true);
        }
    }
}
