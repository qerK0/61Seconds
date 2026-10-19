using System;
using System.Collections.Generic;

namespace SixtyOne.QTE
{
    /// <summary>
    /// One attempt at a key sequence (pure, unit-tested). Each key has its own time window; the whole attempt has a
    /// total timeout. A wrong key or a timeout fails the attempt.
    /// </summary>
    public sealed class QTERun
    {
        private readonly QTEKey[] keys;

        public IReadOnlyList<QTEKey> Keys => keys;
        public int Index { get; private set; }
        public float TimePerKey { get; }
        public float TotalTimeout { get; }
        public float KeyTimeLeft { get; private set; }
        public float TotalTimeLeft { get; private set; }
        public bool Succeeded { get; private set; }
        public QTEFailReason FailReason { get; private set; }
        public bool Failed => FailReason != QTEFailReason.None;
        public bool Running => !Succeeded && !Failed;

        /// <summary>Key the player must press now (the last one after the run ends).</summary>
        public QTEKey Target => keys[Math.Min(Index, keys.Length - 1)];

        /// <summary>0..1 of the current key's window left.</summary>
        public float KeyTimeFraction => TimePerKey > 0f ? KeyTimeLeft / TimePerKey : 0f;

        public QTERun(QTEKey[] sequence, float timePerKey, float totalTimeout)
        {
            if (sequence == null || sequence.Length == 0) throw new ArgumentException("QTE needs at least one key.", nameof(sequence));
            keys = (QTEKey[])sequence.Clone();
            TimePerKey = timePerKey;
            TotalTimeout = totalTimeout;
            KeyTimeLeft = timePerKey;
            TotalTimeLeft = totalTimeout;
        }

        public QTEPressResult Press(QTEKey key)
        {
            if (!Running) return QTEPressResult.Ignored;
            if (key != keys[Index])
            {
                FailReason = QTEFailReason.WrongKey;
                return QTEPressResult.Miss;
            }

            Index++;
            if (Index >= keys.Length)
            {
                Succeeded = true;
                return QTEPressResult.Completed;
            }

            KeyTimeLeft = TimePerKey;
            return QTEPressResult.Hit;
        }

        /// <summary>Advances time. Returns true if this tick failed the attempt.</summary>
        public bool Tick(float deltaSeconds)
        {
            if (!Running || deltaSeconds <= 0f) return false;
            if (TotalTimeout > 0f)
            {
                TotalTimeLeft -= deltaSeconds;
                if (TotalTimeLeft <= 0f)
                {
                    TotalTimeLeft = 0f;
                    FailReason = QTEFailReason.TotalTimeout;
                    return true;
                }
            }

            if (TimePerKey > 0f)
            {
                KeyTimeLeft -= deltaSeconds;
                if (KeyTimeLeft <= 0f)
                {
                    KeyTimeLeft = 0f;
                    FailReason = QTEFailReason.KeyTimeout;
                    return true;
                }
            }

            return false;
        }
    }
}
