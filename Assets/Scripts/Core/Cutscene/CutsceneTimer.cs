namespace ShapeInventory.Core
{
    /// <summary>
    /// Pure timing/state logic of a skippable timed cutscene (no Unity objects, unit-testable).
    /// Idle -> Start() -> running -> finished by time (Tick) or by Skip().
    /// </summary>
    public sealed class CutsceneTimer
    {
        public CutsceneTimer(float duration)
        {
            Duration = duration > 0f ? duration : 0f;
        }

        /// <summary>Seconds the cutscene lasts. Never negative; 0 means it finishes on the first Tick.</summary>
        public float Duration { get; }

        public float Elapsed { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsFinished { get; private set; }

        /// <summary>True when the cutscene ended because Skip() was called, false when the time ran out.</summary>
        public bool WasSkipped { get; private set; }

        /// <summary>0..1, 1 once finished.</summary>
        public float Progress
        {
            get
            {
                if (IsFinished) return 1f;
                if (Duration <= 0f) return 0f;
                float p = Elapsed / Duration;
                return p < 0f ? 0f : (p > 1f ? 1f : p);
            }
        }

        /// <summary>(Re)starts from zero.</summary>
        public void Start()
        {
            Elapsed = 0f;
            IsRunning = true;
            IsFinished = false;
            WasSkipped = false;
        }

        /// <summary>Advances time. Returns true on the single tick in which the cutscene finishes by time.</summary>
        public bool Tick(float deltaTime)
        {
            if (!IsRunning) return false;

            if (deltaTime > 0f) Elapsed += deltaTime;
            if (Elapsed < Duration && Duration > 0f) return false;

            Finish(false);
            return true;
        }

        /// <summary>Ends a running cutscene early. Returns false (and changes nothing) if it is not running.</summary>
        public bool Skip()
        {
            if (!IsRunning) return false;
            Finish(true);
            return true;
        }

        private void Finish(bool skipped)
        {
            IsRunning = false;
            IsFinished = true;
            WasSkipped = skipped;
        }
    }
}
