using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WaterSort.Model;

namespace WaterSort
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Flask Layout")]
        [SerializeField] private FlaskView      flaskViewPrefab;
        [SerializeField] private RectTransform  flaskContainer;
        [SerializeField] private float flaskWidth   = 88f;
        [SerializeField] private float flaskHeight  = 210f;
        [SerializeField] private float spacingX     = 16f;
        [SerializeField] private float spacingY     = 28f;
        [SerializeField] private int   maxPerRow    = 5;

        [Header("Colors")]
        [SerializeField] private Color[] liquidColors = new Color[]
        {
            new Color(0.95f, 0.27f, 0.27f),
            new Color(0.25f, 0.52f, 0.95f),
            new Color(0.22f, 0.82f, 0.38f),
            new Color(0.98f, 0.86f, 0.15f),
            new Color(0.75f, 0.22f, 0.95f),
            new Color(0.98f, 0.55f, 0.12f),
            new Color(0.18f, 0.92f, 0.92f),
            new Color(0.95f, 0.25f, 0.68f),
            new Color(0.52f, 0.85f, 0.22f),
            new Color(0.55f, 0.32f, 0.12f),
        };

        private GameState       _state;
        private List<FlaskData> _initialSnapshot;
        private List<FlaskView> _views = new();
        private int  _selectedIndex = -1;
        private int  _currentLevel  = 1;
        private bool _inputLocked;

        public event System.Action<int> OnMoveCountChanged;
        public event System.Action<int> OnLevelLoaded;
        public event System.Action      OnWin;
        public event System.Action      OnLose;
        public GameState State  => _state;
        public int CurrentLevel => _currentLevel;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

       private void Start()
        {
            SetupBackground();
            // LoadLevel(1); // <-- Закоментовано: тепер рівень запускається з меню
        }

        // ── Додано: Очищення ігрового поля для повернення в меню ──
        public void ClearBoard()
        {
            foreach (var v in _views) if (v) Destroy(v.gameObject);
            _views.Clear();
            _currentLevel = 0;
            _state = null;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void LoadLevel(int levelIndex)
        {
            _currentLevel    = Mathf.Max(1, levelIndex);
            _state           = LevelGenerator.Generate(_currentLevel);
            _initialSnapshot = _state.CloneFlasks();
            _selectedIndex   = -1;
            _inputLocked     = false;

            RebuildViews();
            OnLevelLoaded?.Invoke(_currentLevel);
            OnMoveCountChanged?.Invoke(0);
        }

        public void OnFlaskClicked(int index)
        {
            if (_inputLocked || _state.IsWon || _state.IsLost) return;

            if (_selectedIndex == -1)
            {
                if (!_state.Flasks[index].IsEmpty)
                {
                    _selectedIndex = index;
                    _views[index].SetSelected(true);
                }
            }
            else if (_selectedIndex == index)
            {
                _views[index].SetSelected(false);
                _selectedIndex = -1;
            }
            else
            {
                int from = _selectedIndex, to = index;
                _views[from].SetSelected(false);
                _selectedIndex = -1;

                if (_state.CanPour(from, to))
                {
                    _inputLocked = true;

                    // Snapshot color before state changes
                    Color pourColor = GetColor(_state.Flasks[from].TopColor);
                    pourColor.a = 0.92f;

                    _state.TryPour(from, to);
                    OnMoveCountChanged?.Invoke(_state.MoveCount);

                    StartCoroutine(DoPour(from, to, pourColor));
                }
            }
        }

        public void Undo()
        {
            if (_inputLocked || !_state.CanUndo) return;
            DeselectAll();
            _state.Undo();
            OnMoveCountChanged?.Invoke(_state.MoveCount);
            RefreshAll();
        }

        public void Restart()
        {
            DeselectAll();
            _state.Reset(_initialSnapshot);
            _initialSnapshot = _state.CloneFlasks();
            OnMoveCountChanged?.Invoke(0);
            RefreshAll();
        }

        public void NextLevel() => LoadLevel(_currentLevel + 1);

        public Color GetColor(int id) =>
            (id >= 0 && id < liquidColors.Length) ? liquidColors[id] : Color.white;

        // ── Pour sequence ─────────────────────────────────────────────────────────

        private IEnumerator DoPour(int from, int to, Color pourColor)
        {
            var srcView = _views[from];
            var dstRT   = _views[to].GetComponent<RectTransform>();

            // Full animation: lift → move above → tilt → stream → return
            // Stream hold time ~0.4s
            yield return srcView.PlayPourTo(dstRT, pourColor, 0.40f);

            // Update visuals after animation
            _views[from].RefreshLayers(_state.Flasks[from]);
            _views[to].RefreshLayers(_state.Flasks[to]);

            _inputLocked = false;

            if (_state.IsWon)
            {
                SaveSystem.TrySaveBestMoves(_currentLevel, _state.MoveCount);
                OnWin?.Invoke();
            }
            else if (_state.IsLost)
                OnLose?.Invoke();
        }

        // ── View building ─────────────────────────────────────────────────────────

        private void RebuildViews()
        {
            foreach (var v in _views) if (v) Destroy(v.gameObject);
            _views.Clear();

            int count = _state.Flasks.Count;
            int cols  = Mathf.Min(count, maxPerRow);
            int rows  = Mathf.CeilToInt((float)count / cols);

            float totalW = cols * flaskWidth  + (cols - 1) * spacingX;
            float totalH = rows * flaskHeight + (rows - 1) * spacingY;
            float yOff   = -20f;

            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = i / cols;

                float x = -totalW * 0.5f + col * (flaskWidth + spacingX) + flaskWidth * 0.5f;
                float y =  totalH * 0.5f - row * (flaskHeight + spacingY) - flaskHeight * 0.5f + yOff;

                var view = Instantiate(flaskViewPrefab, flaskContainer);
                var rt   = view.GetComponent<RectTransform>();
                rt.sizeDelta        = new Vector2(flaskWidth, flaskHeight);
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, y);

                int captured = i;
                view.Init(i, _state.Flasks[i], () => OnFlaskClicked(captured));
                _views.Add(view);
            }
        }

        private void RefreshAll()
        {
            for (int i=0; i<_views.Count; i++)
                _views[i].RefreshLayers(_state.Flasks[i]);
        }

        private void DeselectAll()
        {
            if (_selectedIndex >= 0 && _selectedIndex < _views.Count)
                _views[_selectedIndex].SetSelected(false);
            _selectedIndex = -1;
        }

        // ── Background ────────────────────────────────────────────────────────────

        private void SetupBackground()
        {
            if (Camera.main) Camera.main.backgroundColor = new Color(0.06f, 0.04f, 0.18f);

            var canvas = flaskContainer?.GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var existing = canvas.transform.Find("Background");
            if (existing) Destroy(existing.gameObject);

            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(canvas.transform, false);
            bgGO.transform.SetAsFirstSibling();

            var rt = bgGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = bgGO.GetComponent<Image>();
            var sprite = Resources.Load<Sprite>("Sprites/Background");
            if (sprite != null)
            {
                img.sprite         = sprite;
                img.type           = Image.Type.Simple;
                img.preserveAspect = false;
            }
            else img.color = new Color(0.06f, 0.04f, 0.18f, 1f);
        }
    }
}
