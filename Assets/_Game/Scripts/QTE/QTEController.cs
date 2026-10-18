using System;
using SixtyOne.Audio;
using SixtyOne.Core;
using SixtyOne.Iso;
using SixtyOne.Nodes;
using SixtyOne.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SixtyOne.QTE
{
    /// <summary>
    /// Plays QTENodes: rolls a random W/A/S/D sequence (QTESession), reads ONLY the W, A, S and D keys through
    /// Input System actions (plus the view's on-screen buttons), and applies the node's outcomes. A miss or a
    /// timeout costs building time, shakes the camera, freezes the QTE briefly (hit-stop without touching
    /// Time.timeScale) and re-rolls a fresh sequence. Uses unscaled time.
    /// </summary>
    public sealed class QTEController : MonoBehaviour, INodeHandler
    {
        [Tooltip("State owner.")]
        [SerializeField] private GameManager gameManager;

        [Tooltip("Component implementing IQTEView (QTEView).")]
        [SerializeField] private MonoBehaviour view;

        [Tooltip("Camera shake used on a miss. Optional.")]
        [SerializeField] private CameraShake cameraShake;

        private static readonly System.Random Rng = new System.Random(Environment.TickCount);

        private InputAction[] keyActions;
        private QTENode node;
        private QTESession session;
        private Action<SceneNode> onFinished;
        private float hitStopLeft;
        private QTEConfig config;
        private int failures;

        private IQTEView View => view as IQTEView;

        /// <summary>The running session (null when no QTE is active).</summary>
        public QTESession Session => session;

        private void Awake()
        {
            if (gameManager == null) gameManager = GameManager.Instance;
            keyActions = new InputAction[QTESequence.AllKeys.Length];
            for (int i = 0; i < keyActions.Length; i++)
            {
                QTEKey key = QTESequence.AllKeys[i];
                var action = new InputAction("QTE_" + key, InputActionType.Button, BindingFor(key));
                action.performed += _ => OnKey(key);
                keyActions[i] = action;
            }
        }

        private void OnDestroy()
        {
            if (keyActions == null) return;
            foreach (InputAction action in keyActions) action?.Dispose();
        }

        /// <summary>Keyboard binding path for a key (W/A/S/D only).</summary>
        public static string BindingFor(QTEKey key) => "<Keyboard>/" + key.ToString().ToLowerInvariant();

        public bool CanHandle(SceneNode sceneNode) => sceneNode is QTENode;

        public void Begin(SceneNode sceneNode, Action<SceneNode> done)
        {
            Cancel();
            node = (QTENode)sceneNode;
            onFinished = done;
            config = node.ConfigFor(gameManager != null ? gameManager.State.Flags : null);
            failures = 0;
            if (config == null)
            {
                Debug.LogError($"[QTE] Node '{node.Id}' has no QTEConfig.", this);
                return;
            }

            session = new QTESession(node.Context, config.SequenceLengthMin, config.SequenceLengthMax, config.TimePerKey, config.TotalTimeout, Rng);
            session.KeyHit += OnHit;
            session.Failed += OnFailed;
            session.Succeeded += OnSucceeded;
            session.Begin();
            hitStopLeft = 0f;

            View?.Show(node.Context, node.PromptKey.IsEmpty ? string.Empty : node.PromptKey.Display, OnKey);
            View?.Refresh(session.Current);
            SetKeysEnabled(true);
        }

        public void OnTimeout()
        {
        }

        public void Cancel()
        {
            if (session != null) session.Stop();
            session = null;
            node = null;
            onFinished = null;
            SetKeysEnabled(false);
            View?.Hide();
        }

        private void Update()
        {
            if (session == null || session.Done) return;
            // Unscaled time: the ESC pause (timeScale 0) does not stop it, so freeze explicitly.
            if (PauseState.IsPaused) return;
            if (gameManager != null && gameManager.IsDead)
            {
                Cancel();
                return;
            }

            if (hitStopLeft > 0f)
            {
                hitStopLeft -= Time.unscaledDeltaTime;
                return;
            }

            session.Tick(Time.unscaledDeltaTime);
            if (session != null) View?.Refresh(session.Current);
        }

        /// <summary>Key input (keyboard action or on-screen button).</summary>
        public void OnKey(QTEKey key)
        {
            if (session == null || session.Done || hitStopLeft > 0f || PauseState.IsPaused) return;
            if (gameManager != null && gameManager.IsDead) return;
            session.Press(key);
            if (session != null) View?.Refresh(session.Current);
        }

        private void OnHit(QTESession s, int keysDone)
        {
            if (config != null) AudioService.PlayCue(config.HitCue);
            View?.FlashHit(keysDone);
        }

        private void OnFailed(QTESession s, QTEFailReason reason)
        {
            QTENode current = node;
            if (current == null) return;
            AudioService.PlayCue(config.MissCue);
            View?.FlashMiss(reason);
            Shake(config);
            hitStopLeft = config.HitStopSeconds;

            ActionCostRouter.SpendTime(config.FailureBuildingTimerPenalty);
            if (gameManager != null && gameManager.IsDead)
            {
                Cancel();
                return;
            }

            // Too many misses (the rabid dog): this failure kills instead of re-rolling.
            failures++;
            if (gameManager != null && QTENode.FailuresKill(failures, current.MaxFailures))
            {
                Cancel();
                gameManager.Die(current.FailDeath.ToInfo());
                return;
            }

            if (gameManager != null) OutcomeExecutor.Execute(current.FailOutcomes, gameManager.State);
            GameEvents.RaiseMessage(reason == QTEFailReason.WrongKey ? (current.FailKey.IsEmpty ? UiKeys.QteMiss : current.FailKey.Key) : UiKeys.QteTimeout);
            View?.Refresh(s.Current);
        }

        private void OnSucceeded(QTESession s)
        {
            QTENode current = node;
            if (current == null) return;
            AudioService.PlayCue(config.SuccessCue);
            if (gameManager != null) OutcomeExecutor.Execute(current.SuccessOutcomes, gameManager.State);
            if (!current.SuccessKey.IsEmpty) GameEvents.RaiseMessage(current.SuccessKey.Key);

            Action<SceneNode> done = onFinished;
            SceneNode next = current.NextNode;
            session = null;
            node = null;
            onFinished = null;
            SetKeysEnabled(false);
            View?.Hide();
            done?.Invoke(next);
        }

        private void Shake(QTEConfig config)
        {
            if (cameraShake == null || config.FailShake <= 0f) return;
            float magnitude = config.FailShake;
            GameSettings settings = gameManager != null ? gameManager.Settings : null;
            if (settings != null && settings.ReduceEffects) magnitude *= settings.ReducedShakeScale;
            cameraShake.Shake(0.25f, magnitude);
        }

        private void SetKeysEnabled(bool on)
        {
            if (keyActions == null) return;
            foreach (InputAction action in keyActions)
            {
                if (action == null) continue;
                if (on) action.Enable();
                else action.Disable();
            }
        }
    }
}
