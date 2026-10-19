using System;

namespace SixtyOne.Nodes
{
    /// <summary>Plays special node types (e.g. placement) instead of the normal choice flow.</summary>
    public interface INodeHandler
    {
        /// <summary>True if this handler plays the node.</summary>
        bool CanHandle(SceneNode node);

        /// <summary>Starts the node. Call onFinished with the next node when done (null = stay).</summary>
        void Begin(SceneNode node, Action<SceneNode> onFinished);

        /// <summary>The node timer ran out.</summary>
        void OnTimeout();

        /// <summary>Abort (death, rewind, node change).</summary>
        void Cancel();
    }

    /// <summary>Handlers that re-present their own UI after a choice that stays in the node.</summary>
    public interface IChoiceRefresher
    {
        void RefreshChoices();
    }
}
