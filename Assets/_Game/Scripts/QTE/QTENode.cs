using System.Collections.Generic;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;

namespace SixtyOne.QTE
{
    /// <summary>
    /// A node played as a randomized WASD quick-time event (QTEController). Success applies the success outcomes
    /// and goes to the next node; every failure costs building time (QTEConfig), applies the fail outcomes and
    /// re-rolls a new sequence. The node's own choices (e.g. "Geri dön") stay available next to the QTE.
    /// </summary>
    [CreateAssetMenu(menuName = "SixtyOne/QTE Node", fileName = "qte_new")]
    public sealed class QTENode : SceneNode
    {
        [Tooltip("Breaker wiring or dog evasion (view title + feedback).")]
        [SerializeField] private QTEContext context;

        [Tooltip("Sequence length, timing and penalty.")]
        [SerializeField] private QTEConfig config;

        [Tooltip("Prompt above the keys, e.g. 'Kabloları bağla!'.")]
        [SerializeField] private TextKey promptKey;

        [Tooltip("Message on success.")]
        [SerializeField] private TextKey successKey;

        [Tooltip("Message on a failed attempt (after the penalty).")]
        [SerializeField] private TextKey failKey;

        [Tooltip("Applied on success (e.g. SetFlag PowerCut_F3, DogGone).")]
        [SerializeField] private List<Outcome> successOutcomes = new List<Outcome>();

        [Tooltip("Applied on each failed attempt. Optional.")]
        [SerializeField] private List<Outcome> failOutcomes = new List<Outcome>();

        [Tooltip("Node after success.")]
        [SerializeField] private SceneNode nextNode;

        public QTEContext Context => context;
        public QTEConfig Config => config;
        public TextKey PromptKey => promptKey;
        public TextKey SuccessKey => successKey;
        public TextKey FailKey => failKey;
        public IReadOnlyList<Outcome> SuccessOutcomes => successOutcomes;
        public IReadOnlyList<Outcome> FailOutcomes => failOutcomes;
        public SceneNode NextNode => nextNode;

        /// <summary>Authoring helper (call after Configure). Keys follow id.prompt / id.success / id.fail.</summary>
        public void ConfigureQte(QTEContext qteContext, QTEConfig qteConfig, IEnumerable<Outcome> onSuccess, IEnumerable<Outcome> onFail, SceneNode next)
        {
            context = qteContext;
            config = qteConfig;
            promptKey = Id + ".prompt";
            successKey = Id + ".success";
            failKey = Id + ".fail";
            successOutcomes = new List<Outcome>(onSuccess);
            failOutcomes = onFail != null ? new List<Outcome>(onFail) : new List<Outcome>();
            nextNode = next;
        }
    }
}
