using System;
using System.Collections.Generic;
using NUnit.Framework;
using SixtyOne.QTE;

namespace SixtyOne.Tests
{
    /// <summary>WASD sequence generation, single attempts and full sessions (breaker wiring, dog evasion).</summary>
    public class QTETests
    {
        private static readonly QTEKey[] Wasd = { QTEKey.W, QTEKey.A, QTEKey.S, QTEKey.D };

        [Test]
        public void SequencesOnlyUseWasd()
        {
            var rng = new Random(1);
            for (int run = 0; run < 200; run++)
            {
                QTEKey[] keys = QTESequence.Generate(rng, QTESequence.RollLength(rng, 4, 6));
                Assert.That(keys.Length, Is.InRange(4, 6));
                foreach (QTEKey key in keys) CollectionAssert.Contains(Wasd, key);
                Assert.LessOrEqual(QTESequence.LongestRun(keys), QTESequence.MaxRun, "never 3 identical keys in a row");
            }

            Assert.AreEqual(4, Enum.GetValues(typeof(QTEKey)).Length, "only W, A, S, D exist");
            Assert.AreEqual("<Keyboard>/w", QTEController.BindingFor(QTEKey.W));
            Assert.AreEqual("<Keyboard>/d", QTEController.BindingFor(QTEKey.D));
        }

        [Test]
        public void ConsecutiveRunsRollDifferentSequences()
        {
            var rng = new Random(7);
            var seen = new HashSet<string>();
            for (int i = 0; i < 20; i++) seen.Add(string.Join("", QTESequence.Generate(rng, 6)));
            Assert.Greater(seen.Count, 15, "fresh sequence every attempt, no fixed pattern");

            var session = new QTESession(QTEContext.BreakerWiring, 6, 6, 1f, 0f, new Random(3));
            session.Begin();
            string first = string.Join("", session.Current.Keys);
            session.Press(Wrong(session.Current.Target));
            Assert.AreEqual(2, session.Attempts, "a failure re-rolls");
            Assert.AreEqual(0, session.Current.Index);
            Assert.AreNotEqual(first, string.Join("", session.Current.Keys), "with this seed the retry differs");
        }

        [Test]
        public void RunHitsMissesAndTimesOut()
        {
            var run = new QTERun(new[] { QTEKey.W, QTEKey.A }, 1f, 5f);
            Assert.AreEqual(QTEPressResult.Hit, run.Press(QTEKey.W));
            Assert.AreEqual(QTEPressResult.Completed, run.Press(QTEKey.A));
            Assert.IsTrue(run.Succeeded);
            Assert.AreEqual(QTEPressResult.Ignored, run.Press(QTEKey.S));

            var wrong = new QTERun(new[] { QTEKey.S }, 1f, 5f);
            Assert.AreEqual(QTEPressResult.Miss, wrong.Press(QTEKey.D));
            Assert.AreEqual(QTEFailReason.WrongKey, wrong.FailReason);

            var slow = new QTERun(new[] { QTEKey.S, QTEKey.D }, 1f, 5f);
            Assert.IsFalse(slow.Tick(0.5f));
            Assert.IsTrue(slow.Tick(0.6f));
            Assert.AreEqual(QTEFailReason.KeyTimeout, slow.FailReason);

            var total = new QTERun(new[] { QTEKey.S, QTEKey.D, QTEKey.W }, 1f, 1.5f);
            total.Tick(0.9f);
            total.Press(QTEKey.S);
            Assert.IsTrue(total.Tick(0.7f));
            Assert.AreEqual(QTEFailReason.TotalTimeout, total.FailReason);
        }

        [TestCase(QTEContext.BreakerWiring)]
        [TestCase(QTEContext.DogEvasion)]
        public void SessionCallsSuccessAndFailureCallbacks(QTEContext context)
        {
            var session = new QTESession(context, 4, 6, 1.2f, 8f, new Random(11));
            int failures = 0;
            var reasons = new List<QTEFailReason>();
            int successes = 0;
            int hits = 0;
            QTEContext seen = (QTEContext)(-1);
            session.Failed += (s, reason) => { failures++; reasons.Add(reason); };
            session.Succeeded += s => { successes++; seen = s.Context; };
            session.KeyHit += (s, done) => hits++;
            session.Begin();

            session.Press(Wrong(session.Current.Target));
            Assert.AreEqual(1, failures);
            Assert.AreEqual(0, successes);

            Assert.AreEqual(QTEFailReason.WrongKey, reasons[0]);

            session.Tick(10f);
            Assert.AreEqual(2, failures, "timeouts fail too");
            Assert.AreEqual(QTEFailReason.TotalTimeout, reasons[1]);

            IReadOnlyList<QTEKey> keys = session.Current.Keys;
            foreach (QTEKey key in keys) session.Press(key);
            Assert.AreEqual(1, successes);
            Assert.AreEqual(context, seen);
            Assert.AreEqual(keys.Count, hits);
            Assert.IsTrue(session.Done);
            Assert.AreEqual(QTEPressResult.Ignored, session.Press(QTEKey.W), "no events after success");
            Assert.AreEqual(2, failures);
        }

        [Test]
        public void ThirdDogFailureKills()
        {
            Assert.IsFalse(QTENode.FailuresKill(2, 3));
            Assert.IsTrue(QTENode.FailuresKill(3, 3), "the 3rd failure is the bite");
            Assert.IsFalse(QTENode.FailuresKill(50, 0), "0 = unlimited (the breaker)");
        }

        [Test]
        public void WhistleSwitchesToTheEasyConfig()
        {
            var normal = UnityEngine.ScriptableObject.CreateInstance<QTEConfig>();
            var easy = UnityEngine.ScriptableObject.CreateInstance<QTEConfig>();
            var node = UnityEngine.ScriptableObject.CreateInstance<QTENode>();
            try
            {
                node.ConfigureQte(QTEContext.DogEvasion, normal, new SixtyOne.Nodes.Outcome[0], null, null);
                node.ConfigureEasy("DogScared", easy);
                var flags = new SixtyOne.Core.Flags();
                Assert.AreSame(normal, node.ConfigFor(flags));
                flags.Set("DogScared", true);
                Assert.AreSame(easy, node.ConfigFor(flags));
                Assert.AreSame(normal, node.ConfigFor(null));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(node);
                UnityEngine.Object.DestroyImmediate(easy);
                UnityEngine.Object.DestroyImmediate(normal);
            }
        }

        private static QTEKey Wrong(QTEKey target) => target == QTEKey.W ? QTEKey.A : QTEKey.W;
    }
}
