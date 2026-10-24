using System;
using ShapeInventory.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShapeInventory.Game
{
    /// <summary>
    /// Timed, skippable cutscene slot. It does NOT play any media itself: it waits for <see cref="Duration"/> seconds
    /// (or until the player skips) and then hides <c>rootToHide</c>.
    ///
    /// Plug your GIF playback in one of two ways:
    ///  - put the GIF player GameObject under <see cref="ContentRoot"/> and start it from the Started event
    ///    (C# event <see cref="Started"/> or the <c>onStarted</c> UnityEvent in the inspector);
    ///  - or leave "Play On Enable" off, start your GIF yourself and call <see cref="Play(float)"/> with the GIF length.
    /// Stop/clean up your playback in <see cref="Finished"/> / <c>onFinished</c> (it fires BEFORE the overlay fades out and hides).
    ///
    /// By default it starts every time the object is enabled, i.e. every time the main menu scene is entered.
    /// Skipping: click / tap anywhere (full-screen <c>skipButton</c>), or Enter / Space (the skip button is selected in the
    /// EventSystem, so this works with both the old and the new Input System). <see cref="Skip"/> is public for your own UI.
    /// </summary>
    public class CutscenePlayer : MonoBehaviour
    {
        private enum State
        {
            Idle,
            Playing,
            FadingOut
        }

        [Header("Timing")]
        [Tooltip("How long the cutscene lasts, in seconds, before it hides itself.")]
        [SerializeField, Min(0f)] private float duration = 5f;
        [Tooltip("Start automatically every time this component is enabled (= every time the scene loads).")]
        [SerializeField] private bool playOnEnable = true;
        [Tooltip("Count real time (true) or scaled game time (false).")]
        [SerializeField] private bool useUnscaledTime = true;
        [Tooltip("Fade-out time after the cutscene ends or is skipped. 0 = hide instantly.")]
        [SerializeField, Min(0f)] private float fadeOutSeconds = 0.25f;

        [Header("Skipping")]
        [SerializeField] private bool skippable = true;
        [Tooltip("Full-screen button in front of the content. Clicking / tapping it skips the cutscene.")]
        [SerializeField] private Button skipButton;

        [Header("Layout")]
        [Tooltip("Object that is hidden when the cutscene ends. Empty = this GameObject.")]
        [SerializeField] private GameObject rootToHide;
        [Tooltip("Put your GIF player under this RectTransform.")]
        [SerializeField] private RectTransform contentRoot;

        [Header("Events")]
        public UnityEvent onStarted = new UnityEvent();
        [Tooltip("Fires when the cutscene ends for ANY reason (time ran out or skipped), before the fade-out starts.")]
        public UnityEvent onFinished = new UnityEvent();
        [Tooltip("Fires additionally when the player skipped it.")]
        public UnityEvent onSkipped = new UnityEvent();

        /// <summary>Raised when playback starts.</summary>
        public event Action Started;

        /// <summary>Raised when the cutscene ends. The argument is true when it was skipped.</summary>
        public event Action<bool> Finished;

        private CutsceneTimer _timer;
        private State _state = State.Idle;
        private float _fadeElapsed;
        private CanvasGroup _group;
        private bool _buttonWired;
        private bool _selectPending;

        public float Duration => duration;
        public bool Skippable => skippable;
        public RectTransform ContentRoot => contentRoot;

        /// <summary>True while the timer is running (not while fading out).</summary>
        public bool IsPlaying => _state == State.Playing;

        /// <summary>Seconds elapsed, 0..Duration. 0 when idle.</summary>
        public float Elapsed => _timer != null ? _timer.Elapsed : 0f;

        private GameObject RootObject => rootToHide != null ? rootToHide : gameObject;

        private void OnEnable()
        {
            WireSkipButton();
            if (playOnEnable) Play();
        }

        private void OnDisable()
        {
            UnwireSkipButton();
            _state = State.Idle;
            _selectPending = false;
        }

        /// <summary>Starts the cutscene with the configured duration. Ignored while one is already running.</summary>
        public void Play()
        {
            if (_state != State.Idle) return;

            GameObject root = RootObject;
            if (!root.activeSelf) root.SetActive(true);

            EnsureGroup();
            if (_group != null)
            {
                _group.alpha = 1f;
                _group.blocksRaycasts = true;
                _group.interactable = true;
            }

            _timer = new CutsceneTimer(duration);
            _timer.Start();
            _state = State.Playing;
            _fadeElapsed = 0f;

            if (skipButton != null)
            {
                skipButton.gameObject.SetActive(skippable);
                _selectPending = skippable;
            }

            RaiseStarted();
        }

        /// <summary>Starts the cutscene with a duration given by the caller (e.g. the length of the GIF).</summary>
        public void Play(float seconds)
        {
            duration = Mathf.Max(0f, seconds);
            Play();
        }

        /// <summary>Changes the duration used by the next Play().</summary>
        public void SetDuration(float seconds)
        {
            duration = Mathf.Max(0f, seconds);
        }

        /// <summary>Ends the cutscene early (does nothing when not skippable or not playing).</summary>
        public void Skip()
        {
            if (!skippable || _state != State.Playing || _timer == null) return;
            if (_timer.Skip()) BeginFinish(true);
        }

        private void Update()
        {
            if (_state == State.Idle) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (_state == State.Playing)
            {
                if (_selectPending) TrySelectSkipButton();

                if (_timer != null && _timer.Tick(dt)) BeginFinish(false);
            }
            else if (_state == State.FadingOut)
            {
                _fadeElapsed += dt;
                float t = fadeOutSeconds <= 0f ? 1f : Mathf.Clamp01(_fadeElapsed / fadeOutSeconds);
                if (_group != null) _group.alpha = 1f - t;
                if (t >= 1f) Hide();
            }
        }

        private void BeginFinish(bool skipped)
        {
            if (_group != null)
            {
                _group.blocksRaycasts = false;
                _group.interactable = false;
            }

            RaiseFinished(skipped);

            if (fadeOutSeconds > 0f && _group != null)
            {
                _state = State.FadingOut;
                _fadeElapsed = 0f;
            }
            else
            {
                Hide();
            }
        }

        private void Hide()
        {
            _state = State.Idle;
            _selectPending = false;
            RootObject.SetActive(false);
        }

        // ------------------------------------------------------------------ events (a throwing listener must never leave the overlay stuck)

        private void RaiseStarted()
        {
            try { Started?.Invoke(); }
            catch (Exception e) { Debug.LogException(e, this); }

            try { onStarted.Invoke(); }
            catch (Exception e) { Debug.LogException(e, this); }
        }

        private void RaiseFinished(bool skipped)
        {
            try { Finished?.Invoke(skipped); }
            catch (Exception e) { Debug.LogException(e, this); }

            try
            {
                onFinished.Invoke();
                if (skipped) onSkipped.Invoke();
            }
            catch (Exception e) { Debug.LogException(e, this); }
        }

        // ------------------------------------------------------------------ helpers

        private void EnsureGroup()
        {
            if (_group != null) return;

            GameObject root = RootObject;
            _group = root.GetComponent<CanvasGroup>();
            if (_group == null && root.transform is RectTransform)
                _group = root.AddComponent<CanvasGroup>();
        }

        private void WireSkipButton()
        {
            if (skipButton == null || _buttonWired) return;
            skipButton.onClick.AddListener(Skip);
            _buttonWired = true;
        }

        private void UnwireSkipButton()
        {
            if (skipButton == null || !_buttonWired) return;
            skipButton.onClick.RemoveListener(Skip);
            _buttonWired = false;
        }

        /// <summary>Selecting the button lets the EventSystem's Submit (Enter / Space) trigger it. Retried until an EventSystem exists.</summary>
        private void TrySelectSkipButton()
        {
            if (skipButton == null || EventSystem.current == null) return;
            EventSystem.current.SetSelectedGameObject(skipButton.gameObject);
            _selectPending = false;
        }
    }
}
