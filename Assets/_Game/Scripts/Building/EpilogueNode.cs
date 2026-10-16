using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.Building
{
    /// <summary>
    /// Ending screen after leaving the building: one family-state line, one borrow line (depends on LeftNote
    /// and how many items were borrowed; never judgemental) and the loop count.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/Epilogue Node", fileName = "epilogue_new")]
    public sealed class EpilogueNode : SceneNode
    {
        [Tooltip("Everyone got out unhurt.")]
        [SerializeField] private TextKey familyHealthyKey;

        [Tooltip("Someone is still injured.")]
        [SerializeField] private TextKey familyInjuredKey;

        [Tooltip("Nothing was borrowed.")]
        [SerializeField] private TextKey borrowNoneKey;

        [Tooltip("Items borrowed and a note was left ({0} = count).")]
        [SerializeField] private TextKey borrowWithNoteKey;

        [Tooltip("Items borrowed without a note ({0} = count).")]
        [SerializeField] private TextKey borrowWithoutNoteKey;

        [Tooltip("Loop count line ({0} = loops).")]
        [SerializeField] private TextKey loopsKey;

        public TextKey FamilyHealthyKey => familyHealthyKey;
        public TextKey FamilyInjuredKey => familyInjuredKey;
        public TextKey BorrowNoneKey => borrowNoneKey;
        public TextKey BorrowWithNoteKey => borrowWithNoteKey;
        public TextKey BorrowWithoutNoteKey => borrowWithoutNoteKey;
        public TextKey LoopsKey => loopsKey;

        /// <summary>Authoring helper (call after Configure). Keys follow id.family_ok / family_hurt / borrow_none / borrow_note / borrow_no_note / loops.</summary>
        public void ConfigureEpilogue(string prefix)
        {
            familyHealthyKey = prefix + ".family_ok";
            familyInjuredKey = prefix + ".family_hurt";
            borrowNoneKey = prefix + ".borrow_none";
            borrowWithNoteKey = prefix + ".borrow_note";
            borrowWithoutNoteKey = prefix + ".borrow_no_note";
            loopsKey = prefix + ".loops";
        }
    }
}
