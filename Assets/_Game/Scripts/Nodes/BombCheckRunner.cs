using System.Collections.Generic;
using SixtyOne.Core;

namespace SixtyOne.Nodes
{
    /// <summary>Runs a node's bomb checks. Pure logic.</summary>
    public static class BombCheckRunner
    {
        /// <summary>
        /// Executes every triggered bomb in order and returns the first one that killed, or null.
        /// Non-fatal bombs (injuries, flags) all apply.
        /// </summary>
        public static BombCheck Run(IReadOnlyList<BombCheck> bombs, GameState state)
        {
            if (bombs == null)
            {
                return null;
            }

            for (int i = 0; i < bombs.Count; i++)
            {
                BombCheck bomb = bombs[i];
                if (bomb == null || !ConditionEvaluator.AllMet(bomb.Conditions, state))
                {
                    continue;
                }

                if (OutcomeExecutor.Execute(bomb.Outcomes, state).Died)
                {
                    return bomb;
                }
            }

            return null;
        }
    }
}
