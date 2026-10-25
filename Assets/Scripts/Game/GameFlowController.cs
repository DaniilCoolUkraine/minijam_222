using ShapeInventory.Core;
using ShapeInventory.DragDrop;
using ShapeInventory.UI.ObjectInventory;
using ShapeInventory.UI.PlayerInventory;
using UnityEngine;
using UnityEngine.UI;

namespace ShapeInventory.Game
{
    /// <summary>
    /// Level flow: builds the models for a level, (re)binds the two inventory views and runs the quest loop:
    /// Playing -> (Done) -> ShowingResult (owner's reaction, inventories locked) -> (Next) -> next level,
    /// and after the last level a Summary with a Restart button. Every outcome advances.
    /// <see cref="QuestEvaluator"/> decides the outcome; a Full result also requires the shape to be completely filled
    /// (<see cref="ShapeFillChecker"/>). Done is never blocked.
    /// Missing UI references (panels, button, status/quest/result texts, drag controller) are created in code at startup.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameFlowController : MonoBehaviour
    {
        private const float PanelPadding = 24f;
        private const float ObjectCellMin = 40f;
        private const float ObjectCellMax = 110f;
        private const float PlayerCellMax = 80f;
        private const float PlayerCellMin = 16f;

        private static readonly Color FullColor = new Color(0.45f, 0.90f, 0.50f, 1f);
        private static readonly Color PartialColor = new Color(1.00f, 0.75f, 0.25f, 1f);
        private static readonly Color FailureColor = new Color(1.00f, 0.42f, 0.42f, 1f);

        private enum FlowState
        {
            Playing,
            ShowingResult,
            Summary
        }

        [Header("Levels")]
        [SerializeField] private LevelData[] levels = new LevelData[0];

        [Header("Player inventory size (rows are extended if the level has more items)")]
        [SerializeField] private int playerColumns = 5;
        [SerializeField] private int playerRows = 4;

        [Header("Scene references (created in code if left empty)")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform playerPanel;
        [SerializeField] private RectTransform objectPanel;
        [SerializeField] private Button doneButton;
        [SerializeField] private Text statusText;
        [SerializeField] private ItemDragController dragController;

        [Header("Quest UI (optional, created in code if left empty)")]
        [SerializeField] private Text questText;
        [SerializeField] private Text resultText;
        [SerializeField] private Text scoreText;

        private PlayerInventoryView _playerView;
        private ObjectInventoryView _objectView;
        private IPlayerInventory _playerModel;
        private IObjectInventory _objectModel;
        private Text _doneLabel;
        private RectTransform _questBubble;
        private CanvasGroup _playerGroup;
        private CanvasGroup _objectGroup;

        private int _levelIndex = -1;
        private FlowState _state = FlowState.Playing;
        private bool _initialised;
        private string _baseStatus = string.Empty;

        private int _totalScore;
        private int _totalMax;
        private int _happyCount;
        private int _neutralCount;
        private int _angryCount;
        private Vector2 _lastPlayerPanelSize;
        private Vector2 _lastObjectPanelSize;

        public int LevelIndex => _levelIndex;
        public int LevelCount => levels != null ? levels.Length : 0;

        private void Start()
        {
            Initialise();
            if (_initialised) LoadLevel(0);
        }

        private void OnDestroy()
        {
            if (doneButton != null) doneButton.onClick.RemoveListener(OnDonePressed);
            DetachObjectModel();
        }

        private void Update()
        {
            if (!_initialised || _state == FlowState.Summary || _objectModel == null) return;
            if (DragServiceLocator.Current != null && DragServiceLocator.Current.IsDragging) return;

            // Window resized (WebGL canvas) -> recompute cell sizes.
            Vector2 p = playerPanel.rect.size;
            Vector2 o = objectPanel.rect.size;
            if ((p - _lastPlayerPanelSize).sqrMagnitude > 1f || (o - _lastObjectPanelSize).sqrMagnitude > 1f)
                BindViews();
        }

        // ------------------------------------------------------------------ setup

        private void Initialise()
        {
            if (_initialised) return;

            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                Debug.LogError("GameFlowController: no root Canvas assigned.", this);
                return;
            }

            if (playerPanel == null || objectPanel == null || doneButton == null || statusText == null)
            {
                var ui = GameUiBuilder.BuildDefaultLayout(rootCanvas);
                if (playerPanel == null) playerPanel = ui.PlayerPanel; else DestroyIfUnused(ui.PlayerPanel);
                if (objectPanel == null)
                {
                    objectPanel = ui.ObjectPanel;
                    // The default object panel carries its own quest UI; adopt whatever is still missing.
                    if (questText == null) { questText = ui.QuestText; _questBubble = ui.QuestBubble; }
                    if (resultText == null) resultText = ui.ResultText;
                    if (scoreText == null) scoreText = ui.ScoreText;
                }
                else
                {
                    DestroyIfUnused(ui.ObjectPanel);
                }
                if (doneButton == null) { doneButton = ui.DoneButton; _doneLabel = ui.DoneLabel; } else DestroyIfUnused(ui.DoneButton.transform);
                if (statusText == null) statusText = ui.StatusText; else DestroyIfUnused(ui.StatusText.transform);
            }

            if (_doneLabel == null) _doneLabel = doneButton.GetComponentInChildren<Text>();

            // Old scenes have no quest UI: build the missing pieces inside the (existing) object panel.
            if (questText == null || resultText == null || scoreText == null)
            {
                var quest = GameUiBuilder.BuildQuestUi(objectPanel);
                if (questText == null) { questText = quest.QuestText; _questBubble = quest.QuestBubble; }
                else DestroyIfUnused(quest.QuestBubble);
                if (resultText == null) resultText = quest.ResultText; else DestroyIfUnused(quest.ResultText);
                if (scoreText == null) scoreText = quest.ScoreText; else DestroyIfUnused(quest.ScoreText);
            }

            _playerGroup = EnsureGroup(playerPanel);
            _objectGroup = EnsureGroup(objectPanel);

            if (dragController == null)
                dragController = ItemDragController.Create(rootCanvas, new Vector2(ObjectCellMin, ObjectCellMin));

            doneButton.onClick.RemoveListener(OnDonePressed);
            doneButton.onClick.AddListener(OnDonePressed);

            _playerView = PlayerInventoryView.Build(playerPanel);
            _objectView = ObjectInventoryView.Build(objectPanel);
            InsetObjectView();

            _initialised = true;
        }

        private static CanvasGroup EnsureGroup(RectTransform rt)
        {
            var group = rt.GetComponent<CanvasGroup>();
            if (group == null) group = rt.gameObject.AddComponent<CanvasGroup>();
            return group;
        }

        /// <summary>Keeps the quest bubble (top) and the result strip (bottom) free of the object grid.</summary>
        private void InsetObjectView()
        {
            var vrt = (RectTransform)_objectView.transform;
            vrt.anchorMin = new Vector2(0f, GameUiBuilder.ResultHeightFraction);
            vrt.anchorMax = new Vector2(1f, 1f - GameUiBuilder.QuestHeightFraction);
            vrt.offsetMin = Vector2.zero;
            vrt.offsetMax = Vector2.zero;
        }

        private void SetInventoriesInteractive(bool interactive)
        {
            ApplyGroup(_playerGroup, interactive);
            ApplyGroup(_objectGroup, interactive);
        }

        private static void ApplyGroup(CanvasGroup group, bool interactive)
        {
            if (group == null) return;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }

        private static void DestroyIfUnused(Component c)
        {
            if (c != null) Destroy(c.gameObject);
        }

        // ------------------------------------------------------------------ levels

        /// <summary>Loads the level with the given index (clamped to the valid range).</summary>
        public void LoadLevel(int index)
        {
            Initialise();
            if (!_initialised) return;

            if (levels == null || levels.Length == 0)
            {
                _baseStatus = "No levels assigned";
                RefreshStatus();
                return;
            }

            index = Mathf.Clamp(index, 0, levels.Length - 1);
            var level = levels[index];
            if (level == null || level.TargetObject == null)
            {
                Debug.LogError("GameFlowController: level " + index + " is missing or has no target object.", this);
                return;
            }

            UnbindViews();
            DetachObjectModel();

            if (index == 0) ResetTotals();

            _levelIndex = index;
            _state = FlowState.Playing;
            SetInventoriesInteractive(true);
            ClearResult();

            // Player inventory: ensure capacity for every starting item.
            var starting = level.StartingItems;
            int itemCount = 0;
            if (starting != null)
                for (int i = 0; i < starting.Length; i++)
                    if (starting[i] != null) itemCount++;

            int columns = Mathf.Max(1, playerColumns);
            int rows = Mathf.Max(1, playerRows);
            int neededRows = (itemCount + columns - 1) / columns;
            if (neededRows > rows) rows = neededRows;

            var playerModel = new PlayerInventoryModel(columns, rows);
            if (starting != null)
            {
                for (int i = 0; i < starting.Length; i++)
                {
                    if (starting[i] == null) continue;
                    int cell;
                    if (!playerModel.TryAddFirstFree(new ItemInstance(starting[i]), out cell))
                        Debug.LogWarning("GameFlowController: could not place starting item " + starting[i].name, this);
                }
            }

            _playerModel = playerModel;
            _objectModel = new ObjectInventoryModel(level.TargetObject);

            string description = level.QuestDescription;
            bool hasDescription = !string.IsNullOrEmpty(description) && description.Trim().Length > 0;
            if (questText != null) questText.text = hasDescription ? description : string.Empty;
            SetQuestVisible(hasDescription);

            _baseStatus = "Object " + (index + 1) + "/" + levels.Length + " - " + level.TargetObject.DisplayName;
            SetDoneLabel("Done");
            RefreshStatus();

            BindViews();
            _objectView.SetUnderlay(level.ObjectSprite);
        }

        public void RestartGame()
        {
            ResetTotals();
            LoadLevel(0);
        }

        private void ResetTotals()
        {
            _totalScore = 0;
            _totalMax = 0;
            _happyCount = 0;
            _neutralCount = 0;
            _angryCount = 0;
        }

        private void AdvanceLevel()
        {
            if (_levelIndex + 1 < LevelCount)
            {
                LoadLevel(_levelIndex + 1);
            }
            else
            {
                ShowSummary();
            }
        }

        private void ShowSummary()
        {
            UnbindViews();
            DetachObjectModel();
            if (_objectView != null) _objectView.SetUnderlay(null);

            _state = FlowState.Summary;
            SetInventoriesInteractive(false);
            ClearResult();

            _baseStatus = "All objects complete!";
            if (questText != null)
            {
                questText.text = "Total score: " + _totalScore + "/" + _totalMax
                                 + "\nHappy: " + _happyCount
                                 + "   Neutral: " + _neutralCount
                                 + "   Angry: " + _angryCount;
            }
            SetQuestVisible(true);
            SetDoneLabel("Restart");
            RefreshStatus();
        }

        private void ShowResult()
        {
            var level = (levels != null && _levelIndex >= 0 && _levelIndex < levels.Length) ? levels[_levelIndex] : null;
            if (level == null || _objectModel == null) return;

            QuestResult result = QuestEvaluator.Evaluate(_objectModel.PlacedItems, level.Requirements, IsObjectFilled());
            if (result == null)
            {
                result = new QuestResult
                {
                    Outcome = QuestOutcome.Failure,
                    Score = 0,
                    MaxScore = level.Requirements != null ? level.Requirements.Length : 0,
                    WrongCount = 0
                };
            }

            _totalScore += result.Score;
            _totalMax += result.MaxScore;
            switch (result.Outcome)
            {
                case QuestOutcome.Full: _happyCount++; break;
                case QuestOutcome.Partial: _neutralCount++; break;
                default: _angryCount++; break;
            }

            string reaction = level.GetOutcomeText(result.Outcome);
            if (string.IsNullOrEmpty(reaction)) reaction = DefaultReaction(result.Outcome);

            if (resultText != null)
            {
                resultText.text = reaction;
                resultText.color = OutcomeColor(result.Outcome);
            }
            if (scoreText != null)
                scoreText.text = result.MaxScore > 0 ? "Score " + result.Score + "/" + result.MaxScore : string.Empty;

            _state = FlowState.ShowingResult;
            SetInventoriesInteractive(false);
            SetDoneLabel(_levelIndex + 1 < LevelCount ? "Next" : "Finish");
            RefreshStatus();
        }

        private static Color OutcomeColor(QuestOutcome outcome)
        {
            switch (outcome)
            {
                case QuestOutcome.Full: return FullColor;
                case QuestOutcome.Partial: return PartialColor;
                default: return FailureColor;
            }
        }

        private static string DefaultReaction(QuestOutcome outcome)
        {
            switch (outcome)
            {
                case QuestOutcome.Full: return "Perfect!";
                case QuestOutcome.Partial: return "That will do.";
                default: return "That is not what I wanted...";
            }
        }

        // ------------------------------------------------------------------ binding / layout

        private void BindViews()
        {
            if (_playerModel == null || _objectModel == null) return;

            Canvas.ForceUpdateCanvases();

            Vector2 playerSize = playerPanel.rect.size;
            Vector2 objectPanelSize = objectPanel.rect.size;
            _lastPlayerPanelSize = playerSize;
            _lastObjectPanelSize = objectPanelSize;

            // The object view is inset to leave room for the quest bubble and the result strip:
            // size the cells from the REMAINING area.
            Vector2 objectSize = ((RectTransform)_objectView.transform).rect.size;

            Vector2 playerCell = ComputePlayerCellSize(playerSize, _playerModel.Columns, _playerModel.Rows);
            Vector2 objectCell = ComputeObjectCellSize(objectSize, _objectModel.Width, _objectModel.Height);

            _playerView.Unbind();
            _objectView.Unbind();
            _playerView.Bind(_playerModel, playerCell);
            _objectView.Bind(_objectModel, objectCell);

            if (dragController != null) dragController.GhostCellSize = objectCell;
        }

        private void UnbindViews()
        {
            if (_playerView != null) _playerView.Unbind();
            if (_objectView != null) _objectView.Unbind();
        }

        private static Vector2 ComputeObjectCellSize(Vector2 panelSize, int objWidth, int objHeight)
        {
            float availW = Mathf.Max(1f, panelSize.x - PanelPadding * 2f);
            float availH = Mathf.Max(1f, panelSize.y - PanelPadding * 2f);
            float fit = Mathf.Min(availW / Mathf.Max(1, objWidth), availH / Mathf.Max(1, objHeight));
            float size = Mathf.Clamp(fit, ObjectCellMin, ObjectCellMax);
            return new Vector2(size, size);
        }

        private static Vector2 ComputePlayerCellSize(Vector2 panelSize, int columns, int rows)
        {
            float availW = Mathf.Max(1f, panelSize.x - PanelPadding * 2f);
            float availH = Mathf.Max(1f, panelSize.y - PanelPadding * 2f);
            float fit = Mathf.Min(availW / Mathf.Max(1, columns), availH / Mathf.Max(1, rows));
            float size = Mathf.Max(PlayerCellMin, Mathf.Min(fit, PlayerCellMax));
            return new Vector2(size, size);
        }

        // ------------------------------------------------------------------ completion

        private void OnDonePressed()
        {
            if (!_initialised) return;
            if (DragServiceLocator.Current != null && DragServiceLocator.Current.IsDragging) return;

            switch (_state)
            {
                case FlowState.Playing:
                    if (_objectModel != null) ShowResult();
                    break;
                case FlowState.ShowingResult:
                    AdvanceLevel();
                    break;
                case FlowState.Summary:
                    RestartGame();
                    break;
            }
        }

        private bool IsObjectFilled()
        {
            ShapeFillResult result = ShapeFillChecker.IsShapeFilled(_objectModel);
            return result != null && result.IsFilled;
        }

        private void DetachObjectModel()
        {
            _objectModel = null;
            _playerModel = null;
        }

        // ------------------------------------------------------------------ text

        private void SetDoneLabel(string label)
        {
            if (_doneLabel != null) _doneLabel.text = label;
        }

        private void RefreshStatus()
        {
            if (statusText == null) return;
            statusText.text = _baseStatus;
        }

        private void ClearResult()
        {
            if (resultText != null) resultText.text = string.Empty;
            if (scoreText != null) scoreText.text = string.Empty;
        }

        private void SetQuestVisible(bool visible)
        {
            if (_questBubble != null) _questBubble.gameObject.SetActive(visible);
            else if (questText != null) questText.gameObject.SetActive(visible);
        }
    }
}
