using System;
using System.Collections.Generic;

namespace SixtyOne.Core
{
    /// <summary>
    /// Pure death -> rewind logic: snapshot history, dying, restoring.
    /// GameManager wraps this; tests use it directly.
    /// </summary>
    public sealed class GameSession
    {
        private readonly Dictionary<string, Snapshot> byNode = new Dictionary<string, Snapshot>(StringComparer.Ordinal);

        public GameState State { get; }

        /// <summary>True between Die() and Rewind().</summary>
        public bool IsDead { get; private set; }

        public DeathInfo LastDeath { get; private set; }

        /// <summary>The most recent snapshot (default rewind target).</summary>
        public Snapshot Latest { get; private set; }

        /// <summary>Snapshot of the current floor's checkpoint (macro rewind target).</summary>
        public Snapshot Checkpoint { get; private set; }

        /// <summary>True if the last Rewind() went back to the checkpoint.</summary>
        public bool LastRewindWasMacro { get; private set; }

        public GameSession(GameState state = null)
        {
            State = state ?? new GameState();
        }

        /// <summary>Snapshots the state for a decision/placement node.</summary>
        public void TakeSnapshot(string nodeId)
        {
            Latest = Snapshot.Capture(State, nodeId);
            if (!string.IsNullOrEmpty(nodeId))
            {
                byNode[nodeId] = Latest;
            }
        }

        /// <summary>Snapshots a floor checkpoint: used for its node and as the macro rewind target.</summary>
        public void TakeCheckpoint(string nodeId)
        {
            TakeSnapshot(nodeId);
            Checkpoint = Latest;
        }

        /// <summary>True if a snapshot exists for the node.</summary>
        public bool HasSnapshot(string nodeId)
        {
            return !string.IsNullOrEmpty(nodeId) && byNode.ContainsKey(nodeId);
        }

        /// <summary>Kills (or traps) a member, collects the tip and raises Died. Ignored while already dead.</summary>
        public bool Die(DeathInfo info)
        {
            if (IsDead || info == null)
            {
                return false;
            }

            IsDead = true;
            LastDeath = info;

            if (!string.IsNullOrEmpty(info.TipId))
            {
                State.CollectedTips.Add(info.TipId);
            }

            if (!string.IsNullOrEmpty(info.AnchorId))
            {
                State.RevealedAnchors.Add(info.AnchorId);
            }

            State.Knowledge.Add(info.MissingItemId);

            // Tone rule: the child is never killed, only trapped.
            MemberHealth health = info.Victim == MemberType.Child ? MemberHealth.Trapped : MemberHealth.Dead;
            StateOps.SetHealth(State, info.Victim, health);

            GameEvents.RaiseDied(info);
            return true;
        }

        /// <summary>
        /// Restores the target snapshot (checkpoint for macro deaths, else DeathInfo.RewindNodeId or the latest), bumps the loop
        /// counter and raises Rewound. Returns the node id to re-enter, or null.
        /// </summary>
        public string Rewind()
        {
            if (!IsDead)
            {
                return null;
            }

            Snapshot target = Latest;
            bool macro = false;
            if (LastDeath != null && LastDeath.RewindTarget == RewindTarget.Checkpoint && Checkpoint != null)
            {
                target = Checkpoint;
                macro = true;
            }
            else if (LastDeath != null && !string.IsNullOrEmpty(LastDeath.RewindNodeId)
                && byNode.TryGetValue(LastDeath.RewindNodeId, out Snapshot specific))
            {
                target = specific;
            }

            IsDead = false;
            if (target == null)
            {
                return null;
            }

            target.RestoreInto(State, keepMemberAnchors: !macro);
            State.Loop.Increment();
            Latest = target;
            LastRewindWasMacro = macro;
            GameEvents.RaiseRewound(target.NodeId);
            return target.NodeId;
        }
    }
}
