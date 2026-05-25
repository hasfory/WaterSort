using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WaterSort
{
    public class HUDController : MonoBehaviour
    {
        [Header("UI Settings")]
        [SerializeField] private Font customFont; // ДОДАНО: Поле для шрифту в інспекторі

        private static readonly Color BgBar     = new Color(0.05f, 0.03f, 0.15f, 0.97f);
        private static readonly Color BtnGreen  = new Color(0.18f, 0.78f, 0.44f, 1f);
        private static readonly Color BtnBlue   = new Color(0.20f, 0.50f, 0.92f, 1f);
        private static readonly Color BtnRed    = new Color(0.88f, 0.22f, 0.22f, 1f);
        private static readonly Color BtnOrange = new Color(0.97f, 0.56f, 0.12f, 1f);
        private static readonly Color Gold      = new Color(1f, 0.84f, 0f, 1f);
        private static readonly Color MenuBg    = new Color(0.06f, 0.04f, 0.18f, 1f);
        private static readonly Color PopupBg   = new Color(0.04f, 0.02f, 0.12f, 0.98f); 

        private Text       _levelText;
        private Text       _moveText;
        private Text       _winMovesText;
        private GameObject _winPanel;
        private GameObject _losePanel;
        
        private GameObject _mainMenuPanel;
        private GameObject _levelSelectPanel;
        private GameObject _aboutPanel;
        private GameObject _hudContainer;

        private void Start()
        {
            if (customFont == null)
            {
                Debug.LogError("[HUDController] ШРИФТ НЕ ПРИЗНАЧЕНО! Додайте шрифт в інспекторі Unity.");
                return;
            }
            StartCoroutine(Build());
        }

        private IEnumerator Build()
        {
            yield return null;

            var cvGO   = new GameObject("HUDCanvas");
            var canvas = cvGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            cvGO.AddComponent<GraphicRaycaster>();
            var sc = cvGO.AddComponent<CanvasScaler>();
            sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1080, 1920);
            sc.matchWidthOrHeight  = 0.5f;

            _hudContainer = new GameObject("HUDContainer", typeof(RectTransform));
            _hudContainer.transform.SetParent(cvGO.transform, false);
            Anchors(_hudContainer.GetComponent<RectTransform>(), 0, 0, 1, 1);

            BuildTopBar(_hudContainer.transform);
            BuildBottomBar(_hudContainer.transform);

            BuildWinPanel(cvGO.transform);
            BuildLosePanel(cvGO.transform);
            
            BuildMainMenu(cvGO.transform);
            BuildLevelSelectPanel(cvGO.transform);
            BuildAboutPanel(cvGO.transform);

            var gm = GameManager.Instance;
            gm.OnMoveCountChanged += n => { if (_moveText)  _moveText.text  = $"Moves: {n}"; };
            gm.OnLevelLoaded      += n => { if (_levelText) _levelText.text = $"Level {n}"; };
            gm.OnWin  += ShowWin;
            gm.OnLose += ShowLose;

            _hudContainer.SetActive(false);
        }

        // ── Main Menu ────────────────────────────────────────────────────────────

        private void BuildMainMenu(Transform root)
        {
            _mainMenuPanel = Panel("MainMenuPanel", root, MenuBg);
            Anchors(_mainMenuPanel.GetComponent<RectTransform>(), 0, 0, 1, 1);

            var title = Txt("Title", _mainMenuPanel.transform, "WATER SORT", 80, TextAnchor.MiddleCenter);
            title.color = BtnBlue;
            title.fontStyle = FontStyle.Bold;
            Anchors(title.rectTransform, 0.1f, 0.70f, 0.9f, 0.85f);

            var playBtn = Btn("▶ Play", _mainMenuPanel.transform, BtnGreen);
            Anchors(playBtn.GetComponent<RectTransform>(), 0.25f, 0.52f, 0.75f, 0.62f);
            playBtn.onClick.AddListener(() => StartGame(1));

            var pickLevelBtn = Btn("Select level", _mainMenuPanel.transform, BtnOrange);
            Anchors(pickLevelBtn.GetComponent<RectTransform>(), 0.25f, 0.38f, 0.75f, 0.48f);
            pickLevelBtn.onClick.AddListener(() => {
                _mainMenuPanel.SetActive(false);
                _levelSelectPanel.SetActive(true);
            });

            var aboutBtn = Btn("About author", _mainMenuPanel.transform, BtnBlue);
            Anchors(aboutBtn.GetComponent<RectTransform>(), 0.25f, 0.24f, 0.75f, 0.34f);
            aboutBtn.onClick.AddListener(() => {
                _mainMenuPanel.SetActive(false);
                _aboutPanel.SetActive(true);
            });
        }

        // ── Level Selection Panel ────────────────────────────────────────────────

        private void BuildLevelSelectPanel(Transform root)
        {
            _levelSelectPanel = Panel("LevelSelectPanel", root, PopupBg);
            Anchors(_levelSelectPanel.GetComponent<RectTransform>(), 0, 0, 1, 1);

            var title = Txt("Title", _levelSelectPanel.transform, "SELECT LEVEL", 56, TextAnchor.MiddleCenter);
            title.color = Gold;
            Anchors(title.rectTransform, 0.1f, 0.75f, 0.9f, 0.85f);

            float yStart = 0.60f;
            float height = 0.08f;
            float spacing = 0.03f;

            for (int i = 1; i <= 3; i++)
            {
                int levelIndex = i;
                float yMax = yStart - (i - 1) * (height + spacing);
                float yMin = yMax - height;

                var lvlBtn = Btn($"Level {levelIndex}", _levelSelectPanel.transform, BtnOrange);
                Anchors(lvlBtn.GetComponent<RectTransform>(), 0.25f, yMin, 0.75f, yMax);
                lvlBtn.onClick.AddListener(() => {
                    _levelSelectPanel.SetActive(false);
                    StartGame(levelIndex);
                });
            }

            var backBtn = Btn("Back", _levelSelectPanel.transform, BtnRed);
            Anchors(backBtn.GetComponent<RectTransform>(), 0.3f, 0.15f, 0.7f, 0.25f);
            backBtn.onClick.AddListener(() => {
                _levelSelectPanel.SetActive(false);
                _mainMenuPanel.SetActive(true);
            });

            _levelSelectPanel.SetActive(false);
        }

        // ── About Author Panel ───────────────────────────────────────────────────

        private void BuildAboutPanel(Transform root)
        {
            _aboutPanel = Panel("AboutPanel", root, PopupBg);
            Anchors(_aboutPanel.GetComponent<RectTransform>(), 0, 0, 1, 1);

            var title = Txt("Title", _aboutPanel.transform, "ABOUT AUTHOR", 56, TextAnchor.MiddleCenter);
            title.color = Gold;
            Anchors(title.rectTransform, 0.1f, 0.70f, 0.9f, 0.80f);

            var infoText = Txt("Info", _aboutPanel.transform, "Developer:\nPlotnikova Vladyslava\nTK-31", 42, TextAnchor.MiddleCenter);
            Anchors(infoText.rectTransform, 0.1f, 0.40f, 0.9f, 0.60f);

            var closeBtn = Btn("Close", _aboutPanel.transform, BtnRed);
            Anchors(closeBtn.GetComponent<RectTransform>(), 0.3f, 0.20f, 0.7f, 0.30f);
            closeBtn.onClick.AddListener(() => {
                _aboutPanel.SetActive(false);
                _mainMenuPanel.SetActive(true);
            });

            _aboutPanel.SetActive(false);
        }

        private void StartGame(int level)
        {
            _mainMenuPanel.SetActive(false);
            _hudContainer.SetActive(true);
            GameManager.Instance.LoadLevel(level);
        }

        private void ReturnToMenu(GameObject currentPanel)
        {
            currentPanel.SetActive(false);
            _hudContainer.SetActive(false);
            _mainMenuPanel.SetActive(true);
            GameManager.Instance.ClearBoard();
        }

        // ── Gameplay UI Panels ───────────────────────────────────────────────────

        private void BuildTopBar(Transform root)
        {
            var bar = Panel("TopBar", root, BgBar);
            Anchors(bar.GetComponent<RectTransform>(), 0,1,1,1, 0,-110,0,0);

            var menuBtn = Btn("Menu", bar.transform, BtnRed);
            Anchors(menuBtn.GetComponent<RectTransform>(), 0.02f, 0.15f, 0.22f, 0.85f);
            menuBtn.onClick.AddListener(() => ReturnToMenu(_hudContainer));

            _levelText = Txt("LT", bar.transform, "Level 1", 38, TextAnchor.MiddleCenter);
            Anchors(_levelText.rectTransform, 0.25f, 0, 0.5f, 1);

            _moveText = Txt("MT", bar.transform, "Moves: 0", 38, TextAnchor.MiddleRight);
            Anchors(_moveText.rectTransform, 0.5f, 0, 0.96f, 1);
        }

        private void BuildBottomBar(Transform root)
        {
            var bar = Panel("BotBar", root, BgBar);
            Anchors(bar.GetComponent<RectTransform>(), 0,0,1,0, 0,0,0,118);

            var u = Btn("↩  Undo", bar.transform, BtnOrange);
            Anchors(u.GetComponent<RectTransform>(), 0.04f,0.1f,0.48f,0.9f);
            u.onClick.AddListener(() => GameManager.Instance.Undo());

            var r = Btn("↺  Restart", bar.transform, BtnBlue);
            Anchors(r.GetComponent<RectTransform>(), 0.52f,0.1f,0.96f,0.9f);
            r.onClick.AddListener(() => GameManager.Instance.Restart());
        }

        private void BuildWinPanel(Transform root)
        {
            _winPanel = Panel("WinPanel", root, new Color(0.05f,0.03f,0.15f,0.96f));
            Anchors(_winPanel.GetComponent<RectTransform>(), 0,0,1,1);

            var t = Txt("T", _winPanel.transform, "🎉 Level Complete!", 56, TextAnchor.MiddleCenter);
            t.color = Gold;
            Anchors(t.rectTransform, 0.05f,0.60f,0.95f,0.76f);

            _winMovesText = Txt("M", _winPanel.transform, "", 38, TextAnchor.MiddleCenter);
            Anchors(_winMovesText.rectTransform, 0.1f,0.48f,0.9f,0.60f);

            var next = Btn("Next Level →", _winPanel.transform, BtnGreen);
            Anchors(next.GetComponent<RectTransform>(), 0.15f,0.34f,0.85f,0.46f);
            next.onClick.AddListener(() => { _winPanel.SetActive(false); GameManager.Instance.NextLevel(); });

            var menu = Btn("Main Menu", _winPanel.transform, BtnBlue);
            Anchors(menu.GetComponent<RectTransform>(), 0.15f,0.19f,0.85f,0.31f);
            menu.onClick.AddListener(() => ReturnToMenu(_winPanel));

            _winPanel.SetActive(false);
        }

        private void BuildLosePanel(Transform root)
        {
            _losePanel = Panel("LosePanel", root, new Color(0.05f,0.03f,0.15f,0.96f));
            Anchors(_losePanel.GetComponent<RectTransform>(), 0,0,1,1);

            var t = Txt("T", _losePanel.transform, "😔 No Moves Left!", 54, TextAnchor.MiddleCenter);
            t.color = new Color(1f,0.38f,0.38f);
            Anchors(t.rectTransform, 0.05f,0.55f,0.95f,0.72f);

            var r = Btn("↺  Try Again", _losePanel.transform, BtnRed);
            Anchors(r.GetComponent<RectTransform>(), 0.15f,0.38f,0.85f,0.52f);
            r.onClick.AddListener(() => { _losePanel.SetActive(false); GameManager.Instance.Restart(); });
            
            var menu = Btn("Main Menu", _losePanel.transform, BtnBlue);
            Anchors(menu.GetComponent<RectTransform>(), 0.15f,0.23f,0.85f,0.37f);
            menu.onClick.AddListener(() => ReturnToMenu(_losePanel));

            _losePanel.SetActive(false);
        }

        private void ShowWin()
        {
            if (_winMovesText)
                _winMovesText.text = $"Completed in {GameManager.Instance.State.MoveCount} moves";
            _winPanel?.SetActive(true);
        }

        private void ShowLose() => _losePanel?.SetActive(true);

        // ── Factory ───────────────────────────────────────────────────────────────
        private GameObject Panel(string n, Transform p, Color c)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(p, false);
            go.GetComponent<Image>().color = c;
            return go;
        }

        private Text Txt(string n, Transform p, string s, int size, TextAnchor a)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(p, false);
            var t = go.GetComponent<Text>();
            t.text = s; t.fontSize = size; t.alignment = a;
            t.color = Color.white;
            t.font  = customFont; 
            t.raycastTarget = false;
            return t;
        }

        private Button Btn(string label, Transform p, Color c)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(p, false);
            go.GetComponent<Image>().color = c;
            var btn = go.GetComponent<Button>();
            var cols = btn.colors; cols.pressedColor = c * 0.75f; btn.colors = cols;

            var tGO = new GameObject("L", typeof(RectTransform), typeof(Text));
            tGO.transform.SetParent(go.transform, false);
            var rt = tGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = tGO.GetComponent<Text>();
            t.text = label; t.fontSize = 34; t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.font  = customFont; 
            t.raycastTarget = false;
            return btn;
        }

        private static void Anchors(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            rt.anchorMin = new Vector2(x0,y0); rt.anchorMax = new Vector2(x1,y1);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void Anchors(RectTransform rt,
            float ax0, float ay0, float ax1, float ay1,
            float ox0, float oy0, float ox1, float oy1)
        {
            rt.anchorMin = new Vector2(ax0,ay0); rt.anchorMax = new Vector2(ax1,ay1);
            rt.offsetMin = new Vector2(ox0,oy0); rt.offsetMax = new Vector2(ox1,oy1);
        }
    }
}