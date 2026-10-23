using System.Collections;
using ShapeInventory.Core;
using ShapeInventory.DragDrop;
using ShapeInventory.UI.ObjectInventory;
using ShapeInventory.UI.PlayerInventory;
using UnityEngine;
using UnityEngine.UI;

namespace ShapeInventory.Game
{
    /// <summary>
    /// Level flow: builds the models for a level, (re)binds the two inventory views, and advances when the
    /// player presses "Done" and <see cref="ShapeFillChecker"/> says the object is filled.
    /// Missing UI references (panels, button, status text, drag controller) are created in code at startup.
    /// </summary>
    [DisallowMultipleComponent]
    public class GameFlowController : MonoBehaviour
    {
        private const float PanelPadding = 24f;
        private const float ObjectCellMin = 40f;
        private const float ObjectCellMax = 110f;
        private const float PlayerCellMax = 80f;
        private const float PlayerCellMin = 16f;

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

        [Header("Behaviour")]
        [Tooltip("If true, the fill check runs after every change of the object inventory (and advances automatically). Off by default.")]
        [SerializeField] private bool autoAdvanceOnChange = false;

        private PlayerInventoryView _playerView;
        private ObjectInventoryView _objectView;
        private IPlayerInventory _playerModel;
        private IObjectInventory _objectModel;
        private Text _doneLabel;

        private int _levelIndex = -1;
        private bool _finished;
        private bool _autoCheckQueued;
        private bool _initialised;
        private string _baseStatus = string.Empty;
        private string _feedback = string.Empty;
        private Vector2 _lastPlayerPanelSize;
        private Vector2 _lastObjectPanelSize;

        public int LevelIndex => _levelIndex;
        public int LevelCount => levels != null ? levels.Length : 0;
        public bool AutoAdvanceOnChange
        {
            get { return autoAdvanceOnChange; }
            set { autoAdvanceOnChange = value; }
        }

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
            if (!_initialised || _finished || _objectModel == null) return;
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
                if (objectPanel == null) objectPanel = ui.ObjectPanel; else DestroyIfUnused(ui.ObjectPanel);
                if (doneButton == null) { doneButton = ui.DoneButton; _doneLabel = ui.DoneLabel; } else DestroyIfUnused(ui.DoneButton.transform);
                if (statusText == null) statusText = ui.StatusText; else DestroyIfUnused(ui.StatusText.transform);
            }

            if (_doneLabel == null) _doneLabel = doneButton.GetComponentInChildren<Text>();

            if (dragController == null)
                dragController = ItemDragController.Create(rootCanvas, new Vector2(ObjectCellMin, ObjectCellMin));

            doneButton.onClick.RemoveListener(OnDonePressed);
            doneButton.onClick.AddListener(OnDonePressed);

            _playerView = PlayerInventoryView.Build(playerPanel);
            _objectView = ObjectInventoryView.Build(objectPanel);

            _initialised = true;
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
                _feedback = string.Empty;
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

            StopAllCoroutines();
            _autoCheckQueued = false;

            UnbindViews();
            DetachObjectModel();

            _levelIndex = index;
            _finished = false;
            _feedback = string.Empty;

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
            _objectModel.Changed += OnObjectChanged;

            _baseStatus = "Object " + (index + 1) + "/" + levels.Length + " - " + level.TargetObject.DisplayName;
            SetDoneLabel("Done");
            RefreshStatus();

            BindViews();
        }

        public void RestartGame()
        {
            LoadLevel(0);
        }

        private void AdvanceLevel()
        {
            if (_levelIndex + 1 < LevelCount)
            {
                LoadLevel(_levelIndex + 1);
            }
            else
            {
                ShowFinished();
            }
        }

        private void ShowFinished()
        {
            StopAllCoroutines();
            _autoCheckQueued = false;
            UnbindViews();
            DetachObjectModel();

            _finished = true;
            _baseStatus = "All objects complete!";
            _feedback = string.Empty;
            SetDoneLabel("Restart");
            RefreshStatus();
        }

        // ------------------------------------------------------------------ binding / layout

        private void BindViews()
        {
            if (_playerModel == null || _objectModel == null) return;

            Canvas.ForceUpdateCanvases();

            Vector2 playerSize = playerPanel.rect.size;
            Vector2 objectSize = objectPanel.rect.size;
            _lastPlayerPanelSize = playerSize;
            _lastObjectPanelSize = objectSize;

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

            if (_finished)
            {
                RestartGame();
                return;
            }

            if (_objectModel == null) return;

            if (IsObjectFilled())
            {
                AdvanceLevel();
            }
            else
            {
                _feedback = "Not filled yet";
                RefreshStatus();
            }
        }

        private bool IsObjectFilled()
        {
            ShapeFillResult result = ShapeFillChecker.IsShapeFilled(_objectModel.PlacedItems);
            return result != null && result.IsFilled;
        }

        private void OnObjectChanged()
        {
            if (!autoAdvanceOnChange || _finished || _autoCheckQueued) return;
            _autoCheckQueued = true;
            // Deferred by a frame so we never rebind views from inside a drop callback.
            StartCoroutine(DeferredAutoCheck());
        }

        private IEnumerator DeferredAutoCheck()
        {
            yield return null;
            _autoCheckQueued = false;
            if (!autoAdvanceOnChange || _finished || _objectModel == null) yield break;
            if (IsObjectFilled()) AdvanceLevel();
        }

        private void DetachObjectModel()
        {
            if (_objectModel != null) _objectModel.Changed -= OnObjectChanged;
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
            statusText.text = string.IsNullOrEmpty(_feedback) ? _baseStatus : _baseStatus + "\n" + _feedback;
        }
    }
}
