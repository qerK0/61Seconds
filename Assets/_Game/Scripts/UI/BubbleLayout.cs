using UnityEngine;

namespace SixtyOne.UI
{
    /// <summary>Where a speech bubble block (bubble + choice rows under it) goes, and where its tail points.</summary>
    public readonly struct BubblePlacement
    {
        /// <summary>Bottom-left corner of the whole block (canvas px, origin bottom-left).</summary>
        public readonly Vector2 BlockMin;
        /// <summary>True if the block sits left of the head (the preferred right side did not fit).</summary>
        public readonly bool OnLeft;
        /// <summary>The bubble corner the tail starts from (canvas px).</summary>
        public readonly Vector2 TailBase;
        /// <summary>Tail direction in degrees (0 = right, -90 = down), aimed at the head.</summary>
        public readonly float TailAngle;
        /// <summary>The head is outside the safe area (off-screen): the block docks to the nearest edge.</summary>
        public readonly bool Docked;

        public BubblePlacement(Vector2 blockMin, bool onLeft, Vector2 tailBase, float tailAngle, bool docked)
        {
            BlockMin = blockMin;
            OnLeft = onLeft;
            TailBase = tailBase;
            TailAngle = tailAngle;
            Docked = docked;
        }
    }

    /// <summary>
    /// Pure bubble placement (unit-tested): above-right of the head by preference, above-left when that does not
    /// fit, always clamped inside the safe area (below the timer / panic meter, left of the bag column). An
    /// off-screen speaker docks the block to the nearest edge with the tail pointing at them.
    /// </summary>
    public static class BubbleLayout
    {
        /// <summary>
        /// head = speaker's head (canvas px); block = bubble + rows size; bubbleHeight = the bubble part (top of the
        /// block); offset = gap from the head (x sideways, y up to the bubble's bottom edge).
        /// </summary>
        public static BubblePlacement Place(Vector2 head, Vector2 block, float bubbleHeight, Rect safe, Vector2 offset)
        {
            bool docked = !safe.Contains(head);
            bool onLeft = false;

            // Bubble bottom at head + offset.y; the rows hang below it, beside the speaker.
            float x = head.x + offset.x;
            float bubbleBottom = head.y + offset.y;
            if (x + block.x > safe.xMax)
            {
                onLeft = true;
                x = head.x - offset.x - block.x;
            }

            float y = bubbleBottom - (block.y - bubbleHeight);
            x = Mathf.Clamp(x, safe.xMin, Mathf.Max(safe.xMin, safe.xMax - block.x));
            y = Mathf.Clamp(y, safe.yMin, Mathf.Max(safe.yMin, safe.yMax - block.y));

            // Tail from the bubble corner nearest the head.
            float bubbleY = y + block.y - bubbleHeight;
            bool headLeft = head.x < x + block.x * 0.5f;
            bool headAbove = head.y > bubbleY + bubbleHeight;
            var corner = new Vector2(headLeft ? x + 18f : x + block.x - 18f, headAbove ? bubbleY + bubbleHeight : bubbleY);
            Vector2 to = head - corner;
            float angle = to.sqrMagnitude > 0.01f ? Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg : -90f;
            return new BubblePlacement(new Vector2(x, y), onLeft, corner, angle, docked);
        }
    }
}
