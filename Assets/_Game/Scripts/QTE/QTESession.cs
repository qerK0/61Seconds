using System;

namespace SixtyOne.QTE
{
    /// <summary>
    /// A QTE until it is beaten (pure, unit-tested): rolls a fresh random sequence per attempt, raises Failed and
    /// re-rolls on every failure, raises Succeeded once. The controller applies penalties and node outcomes.
    /// </summary>
    public sealed class QTESession
    {
        private readonly Random rng;
        private readonly int minLength;
        private readonly int maxLength;
        private readonly float timePerKey;
        private readonly float totalTimeout;

        public QTEContext Context { get; }
        public QTERun Current { get; private set; }

        /// <summary>Attempts started (1 after Begin).</summary>
        public int Attempts { get; private set; }

        public bool Done { get; private set; }

        /// <summary>The sequence was completed.</summary>
        public event Action<QTESession> Succeeded;

        /// <summary>An attempt failed (a new sequence is already rolled when this fires).</summary>
        public event Action<QTESession, QTEFailReason> Failed;

        /// <summary>(session, keys done) a correct key was pressed.</summary>
        public event Action<QTESession, int> KeyHit;

        public QTESession(QTEContext context, int minLength, int maxLength, float timePerKey, float totalTimeout, Random rng)
        {
            Context = context;
            this.minLength = minLength;
            this.maxLength = maxLength;
            this.timePerKey = timePerKey;
            this.totalTimeout = totalTimeout;
            this.rng = rng ?? new Random();
        }

        /// <summary>Starts (or restarts) with a freshly rolled sequence.</summary>
        public void Begin()
        {
            Done = false;
            Attempts++;
            int length = QTESequence.RollLength(rng, minLength, maxLength);
            Current = new QTERun(QTESequence.Generate(rng, length), timePerKey, totalTimeout);
        }

        public QTEPressResult Press(QTEKey key)
        {
            if (Done || Current == null) return QTEPressResult.Ignored;
            QTEPressResult result = Current.Press(key);
            switch (result)
            {
                case QTEPressResult.Hit:
                    KeyHit?.Invoke(this, Current.Index);
                    break;
                case QTEPressResult.Completed:
                    KeyHit?.Invoke(this, Current.Index);
                    Done = true;
                    Succeeded?.Invoke(this);
                    break;
                case QTEPressResult.Miss:
                    Fail(QTEFailReason.WrongKey);
                    break;
            }

            return result;
        }

        public void Tick(float deltaSeconds)
        {
            if (Done || Current == null) return;
            if (Current.Tick(deltaSeconds)) Fail(Current.FailReason);
        }

        /// <summary>Stops the session (no more events).</summary>
        public void Stop()
        {
            Done = true;
        }

        private void Fail(QTEFailReason reason)
        {
            Begin();
            Failed?.Invoke(this, reason);
        }
    }
}
