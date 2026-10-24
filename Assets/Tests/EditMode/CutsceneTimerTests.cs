using NUnit.Framework;
using ShapeInventory.Core;

namespace ShapeInventory.Tests
{
    public class CutsceneTimerTests
    {
        [Test]
        public void NewTimer_IsNotRunningAndNotFinished()
        {
            var t = new CutsceneTimer(3f);
            Assert.IsFalse(t.IsRunning);
            Assert.IsFalse(t.IsFinished);
            Assert.AreEqual(3f, t.Duration, 1e-5f);
        }

        [Test]
        public void Tick_BeforeStart_DoesNothing()
        {
            var t = new CutsceneTimer(1f);
            Assert.IsFalse(t.Tick(5f));
            Assert.IsFalse(t.IsFinished);
            Assert.AreEqual(0f, t.Elapsed, 1e-5f);
        }

        [Test]
        public void Tick_FinishesExactlyWhenDurationIsReached()
        {
            var t = new CutsceneTimer(2f);
            t.Start();

            Assert.IsFalse(t.Tick(0.5f));
            Assert.IsFalse(t.Tick(1.0f));
            Assert.IsTrue(t.IsRunning);
            Assert.AreEqual(0.75f, t.Progress, 1e-5f);

            Assert.IsTrue(t.Tick(0.5f));
            Assert.IsFalse(t.IsRunning);
            Assert.IsTrue(t.IsFinished);
            Assert.IsFalse(t.WasSkipped);
            Assert.AreEqual(1f, t.Progress, 1e-5f);
        }

        [Test]
        public void Tick_ReturnsTrueOnlyOnce()
        {
            var t = new CutsceneTimer(1f);
            t.Start();
            Assert.IsTrue(t.Tick(2f));
            Assert.IsFalse(t.Tick(2f));
        }

        [Test]
        public void Tick_IgnoresNegativeDelta()
        {
            var t = new CutsceneTimer(1f);
            t.Start();
            Assert.IsFalse(t.Tick(-3f));
            Assert.AreEqual(0f, t.Elapsed, 1e-5f);
        }

        [Test]
        public void Skip_WhileRunning_FinishesAndMarksSkipped()
        {
            var t = new CutsceneTimer(10f);
            t.Start();
            t.Tick(1f);

            Assert.IsTrue(t.Skip());
            Assert.IsFalse(t.IsRunning);
            Assert.IsTrue(t.IsFinished);
            Assert.IsTrue(t.WasSkipped);
            Assert.AreEqual(1f, t.Progress, 1e-5f);
        }

        [Test]
        public void Skip_WhenNotRunning_ReturnsFalse()
        {
            var t = new CutsceneTimer(10f);
            Assert.IsFalse(t.Skip());

            t.Start();
            Assert.IsTrue(t.Skip());
            Assert.IsFalse(t.Skip());
        }

        [Test]
        public void Skip_AfterTimeRanOut_ReturnsFalseAndKeepsNotSkipped()
        {
            var t = new CutsceneTimer(1f);
            t.Start();
            t.Tick(5f);

            Assert.IsFalse(t.Skip());
            Assert.IsFalse(t.WasSkipped);
        }

        [Test]
        public void ZeroDuration_FinishesOnFirstTick()
        {
            var t = new CutsceneTimer(0f);
            t.Start();
            Assert.IsTrue(t.IsRunning);
            Assert.AreEqual(0f, t.Progress, 1e-5f);
            Assert.IsTrue(t.Tick(0f));
            Assert.IsTrue(t.IsFinished);
        }

        [Test]
        public void NegativeDuration_IsClampedToZero()
        {
            var t = new CutsceneTimer(-4f);
            Assert.AreEqual(0f, t.Duration, 1e-5f);
            t.Start();
            Assert.IsTrue(t.Tick(0.01f));
        }

        [Test]
        public void Start_AfterFinish_ResetsEverything()
        {
            var t = new CutsceneTimer(1f);
            t.Start();
            t.Skip();

            t.Start();
            Assert.IsTrue(t.IsRunning);
            Assert.IsFalse(t.IsFinished);
            Assert.IsFalse(t.WasSkipped);
            Assert.AreEqual(0f, t.Elapsed, 1e-5f);
            Assert.IsTrue(t.Tick(1f));
        }
    }
}
