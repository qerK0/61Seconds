using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.World
{
    /// <summary>
    /// A window/balcony where the bedsheet rope can be tied to climb down to the room below. Needs the rope in
    /// the bag (missing = message, nothing happens); heavy (locked at 0 stamina); costs time and stamina; the rope
    /// stays tied (removed from the bag); then the target node is entered (a "descend" room transition plays).
    /// </summary>
    public sealed class RopeDescentInteractable : MonoBehaviour, IClickable
    {
        [Tooltip("Unique id.")]
        [SerializeField] private string id = "rope_descent";

        [Tooltip("Label shown above it.")]
        [SerializeField] private TextKey labelKey;

        [Tooltip("The bedsheet rope.")]
        [SerializeField] private Item requiredItem;

        [Tooltip("Message when the rope is missing.")]
        [SerializeField] private TextKey missingKey;

        [Tooltip("Node entered after climbing down.")]
        [SerializeField] private SceneNode targetNode;

        [Tooltip("Node player that enters the target node.")]
        [SerializeField] private NodePlayer nodePlayer;

        [Tooltip("Building-timer seconds for the climb.")]
        [Min(0f)]
        [SerializeField] private float timeCost = 10f;

        [Tooltip("Stamina spent on the climb.")]
        [Min(0f)]
        [SerializeField] private float staminaCost = 20f;

        [Tooltip("The rope stays tied to the window (removed from the bag).")]
        [SerializeField] private bool consumesRope = true;

        public string Id => id;
        public TextKey LabelKey => labelKey;
        public Item RequiredItem => requiredItem;
        public SceneNode TargetNode => targetNode;

        public bool IsClickable
        {
            get
            {
                GameManager gm = GameManager.Instance;
                return isActiveAndEnabled && gm != null && !gm.IsDead && ExplorationGate.Allowed && !InputGate.IsBlocked;
            }
        }

        /// <summary>Authoring helper for scene builders.</summary>
        public void Configure(string interactableId, TextKey label, Item rope, TextKey missing, SceneNode target, NodePlayer player, float seconds, float stamina)
        {
            id = interactableId;
            labelKey = label;
            requiredItem = rope;
            missingKey = missing;
            targetNode = target;
            nodePlayer = player;
            timeCost = seconds;
            staminaCost = stamina;
        }

        /// <summary>Pure rule: can the climb start? (rope owned and stamina left).</summary>
        public static bool CanDescend(GameState state, Item rope)
        {
            return rope != null && StateOps.HasItem(state, rope.Id) && state.Stamina > ChoiceView.ExhaustedStamina;
        }

        public void OnClicked()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null || !IsClickable || targetNode == null) return;
            GameState state = gm.State;

            if (requiredItem == null || !StateOps.HasItem(state, requiredItem.Id))
            {
                GameEvents.RaiseMessage(missingKey.IsEmpty ? UiKeys.RopeDescentMissing : missingKey.Key);
                return;
            }

            if (!CanDescend(state, requiredItem))
            {
                GameEvents.RaiseMessage(UiKeys.StaminaExhausted);
                return;
            }

            ActionCostRouter.SpendStamina(staminaCost);
            ActionCostRouter.SpendTime(timeCost, true);
            if (gm.IsDead) return;
            if (consumesRope) StateOps.RemoveItem(state, requiredItem.Id);
            if (nodePlayer != null) nodePlayer.Enter(targetNode);
        }
    }
}
