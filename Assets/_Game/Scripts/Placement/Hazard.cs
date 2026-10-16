using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Placement
{
    /// <summary>What a hazard does to a member standing on its anchor.</summary>
    public enum HazardEffect
    {
        None,
        Injured,
        Trapped,
        Death
    }

    /// <summary>Code tween played on the hazard's prop during the quake.</summary>
    public enum HazardMotion
    {
        None,
        Topple,
        Drop,
        Shatter
    }

    /// <summary>A danger linked to an anchor (wardrobe, window, doorway...). Resolved per member when a quake hits.</summary>
    [CreateAssetMenu(menuName = "SixtyOne/Hazard", fileName = "hz_new")]
    public sealed class Hazard : ScriptableObject
    {
        [Tooltip("Unique id.")]
        [SerializeField] private string id;

        [Tooltip("Which quakes trigger this hazard.")]
        [SerializeField] private ActMask acts = ActMask.All;

        [Tooltip("Effect on the player character.")]
        [SerializeField] private HazardEffect playerEffect = HazardEffect.Death;

        [Tooltip("Effect on the wife (Death or Injured).")]
        [SerializeField] private HazardEffect wifeEffect = HazardEffect.Injured;

        [Tooltip("Effect on the child. Death is converted to Trapped (the child never dies).")]
        [SerializeField] private HazardEffect childEffect = HazardEffect.Trapped;

        [Tooltip("Death line for the player (also the fallback for others).")]
        [SerializeField] private TextKey textKey;

        [Tooltip("One line shown on black when the wife dies here. No comedy.")]
        [SerializeField] private TextKey wifeTextKey;

        [Tooltip("Line shown when the child gets trapped here.")]
        [SerializeField] private TextKey childTextKey;

        [Tooltip("Deprem Notu unlocked by dying here.")]
        [SerializeField] private Tip tip;

        [Tooltip("Sound when the hazard goes off.")]
        [SerializeField] private AudioCue cue;

        [Tooltip("Tween played on the prop.")]
        [SerializeField] private HazardMotion motion = HazardMotion.None;

        [Tooltip("HazardProp.PropId of the prop to animate in the room.")]
        [SerializeField] private string targetPropId;

        public string Id => string.IsNullOrEmpty(id) ? name : id;
        public ActMask Acts => acts;
        public Tip Tip => tip;
        public AudioCue Cue => cue;
        public HazardMotion Motion => motion;
        public string TargetPropId => targetPropId;
        public TextKey TextKey => textKey;

        public bool IsActiveInAct(int act) => ActMaskUtil.Includes(acts, act);

        /// <summary>Effect on a member type. The child is never killed.</summary>
        public HazardEffect EffectFor(MemberType member)
        {
            switch (member)
            {
                case MemberType.Wife: return wifeEffect;
                case MemberType.Child: return childEffect == HazardEffect.Death ? HazardEffect.Trapped : childEffect;
                default: return playerEffect;
            }
        }

        /// <summary>Death/trapped line for a member, falling back to the main text.</summary>
        public TextKey TextKeyFor(MemberType member)
        {
            if (member == MemberType.Wife && !wifeTextKey.IsEmpty) return wifeTextKey;
            if (member == MemberType.Child && !childTextKey.IsEmpty) return childTextKey;
            return textKey;
        }

        /// <summary>Authoring helper for generators.</summary>
        public void Configure(string hazardId, ActMask hazardActs, HazardEffect player, HazardEffect wife, HazardEffect child, HazardMotion hazardMotion, string propId)
        {
            id = hazardId;
            acts = hazardActs;
            playerEffect = player;
            wifeEffect = wife;
            childEffect = child;
            motion = hazardMotion;
            targetPropId = propId;
        }

        /// <summary>Authoring helper for generators.</summary>
        public void ConfigureText(TextKey playerText, TextKey wifeText, TextKey childText, Tip hazardTip, AudioCue hazardCue = null)
        {
            textKey = playerText;
            wifeTextKey = wifeText;
            childTextKey = childText;
            tip = hazardTip;
            if (hazardCue != null) cue = hazardCue;
        }
    }
}
