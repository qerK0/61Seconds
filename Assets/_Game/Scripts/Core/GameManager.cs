using System.Collections;
using UnityEngine;

namespace SixtyOne.Core
{
    /// <summary>Scene owner of GameState. Takes snapshots and runs the death -> rewind flow.</summary>
    [DefaultExecutionOrder(-500)]
    public sealed class GameManager : MonoBehaviour
    {
        [Tooltip("Seconds before an automatic rewind after death. 0 = wait for the death card's rewind button.")]
        [SerializeField] private float autoRewindSeconds = 0f;

        [Tooltip("Player options (knowledge highlight...). Optional.")]
        [SerializeField] private GameSettings settings;

        /// <summary>Active instance in the scene.</summary>
        public static GameManager Instance { get; private set; }

        public GameSession Session { get; private set; } = new GameSession();
        public GameState State => Session.State;
        public bool IsDead => Session.IsDead;
        public GameSettings Settings => settings;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[GameManager] Second instance found; ignoring it.", this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Starts a fresh run (new state, loop 0).</summary>
        public void ResetSession()
        {
            Session = new GameSession();
        }

        /// <summary>Snapshots the state for a decision/placement node.</summary>
        public void TakeSnapshot(string nodeId)
        {
            Session.TakeSnapshot(nodeId);
        }

        /// <summary>Snapshots the start of a prep phase (macro rewind target).</summary>
        public void TakePrepStartSnapshot(string nodeId)
        {
            Session.TakePrepStartSnapshot(nodeId);
        }

        /// <summary>Kills (or traps) someone and starts the death flow.</summary>
        public void Die(DeathInfo info)
        {
            if (!Session.Die(info))
            {
                return;
            }

            if (autoRewindSeconds > 0f)
            {
                StartCoroutine(AutoRewind());
            }
        }

        /// <summary>Restores the last decision point. Called by the death card button.</summary>
        public void Rewind()
        {
            if (Session.IsDead && Session.Rewind() == null)
            {
                Debug.LogWarning("[GameManager] No snapshot to rewind to.", this);
            }
        }

        private IEnumerator AutoRewind()
        {
            yield return new WaitForSeconds(autoRewindSeconds);
            Rewind();
        }
    }
}
