using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WaterSort
{
    public class HUDController : MonoBehaviour
    {
        private static readonly Color BgBar    = new Color(0.05f, 0.03f, 0.15f, 0.97f);
        private static readonly Color BtnGreen = new Color(0.18f, 0.78f, 0.44f, 1f);
        private static readonly Color BtnBlue  = new Color(0.20f, 0.50f, 0.92f, 1f);
        private static readonly Color BtnRed   = new Color(0.88f, 0.22f, 0.22f, 1f);
        private static readonly Color BtnOrange= new Color(0.97f, 0.56f, 0.12f, 1f);
        private static readonly Color Gold     = new Color(1f, 0.84f, 0f, 1f);

        private Text       _levelText;
        private Text       _moveText;
        private Text       _winMovesText;
        private GameObject _winPanel;
        private GameObject _losePanel;

        private void Start() => StartCoroutine(Build());

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

            BuildTopBar(cvGO.transform);
            BuildBottomBar(cvGO.transform);
            BuildWinPanel(cvGO.transform);
            BuildLosePanel(cvGO.transform);

            var gm = GameManager.Instance;
            gm.OnMoveCountChanged += n => { if (_moveText)  _moveText.text  = $"Ходів: {n}"; };
            gm.OnLevelLoaded      += n => { if (_levelText) _levelText.text = $"Рівень {n}"; };
            gm.OnWin  += ShowWin;
            gm.OnLose += ShowLose;

            if (_levelText) _levelText.text = $"Рівень {gm.CurrentLevel}";
            if (_moveText)  _moveText.text  = "Ходів: 0";
        }

        private void BuildTopBar(Transform root)
        {
            var bar = Panel("TopBar", root, BgBar);
            Anchors(bar.GetComponent<RectTransform>(), 0,1,1,1, 0,-110,0,0);

            _levelText = Txt("LT", bar.transform, "Рівень 1", 42, TextAnchor.MiddleLeft);
            Anchors(_levelText.rectTransform, 0.04f,0,0.5f,1);

            _moveText = Txt("MT", bar.transform, "Ходів: 0", 40, TextAnchor.MiddleRight);
            Anchors(_moveText.rectTransform, 0.5f,0,0.96f,1);
        }

        private void BuildBottomBar(Transform root)
        {
            var bar = Panel("BotBar", root, BgBar);
            Anchors(bar.GetComponent<RectTransform>(), 0,0,1,0, 0,0,0,118);

            var u = Btn("↩  Відмінити", bar.transform, BtnOrange);
            Anchors(u.GetComponent<RectTransform>(), 0.04f,0.1f,0.48f,0.9f);
            u.onClick.AddListener(() => GameManager.Instance.Undo());

            var r = Btn("↺  Спочатку", bar.transform, BtnBlue);
            Anchors(r.GetComponent<RectTransform>(), 0.52f,0.1f,0.96f,0.9f);
            r.onClick.AddListener(() => GameManager.Instance.Restart());
        }

        private void BuildWinPanel(Transform root)
        {
            _winPanel = Panel("WinPanel", root, new Color(0.05f,0.03f,0.15f,0.96f));
            Anchors(_winPanel.GetComponent<RectTransform>(), 0,0,1,1);

            var t = Txt("T", _winPanel.transform, "🎉 Рівень пройдено!", 56, TextAnchor.MiddleCenter);
            t.color = Gold;
            Anchors(t.rectTransform, 0.05f,0.60f,0.95f,0.76f);

            _winMovesText = Txt("M", _winPanel.transform, "", 38, TextAnchor.MiddleCenter);
            Anchors(_winMovesText.rectTransform, 0.1f,0.48f,0.9f,0.60f);

            var next = Btn("Наступний рівень  →", _winPanel.transform, BtnGreen);
            Anchors(next.GetComponent<RectTransform>(), 0.15f,0.34f,0.85f,0.46f);
            next.onClick.AddListener(() => { _winPanel.SetActive(false); GameManager.Instance.NextLevel(); });

            var again = Btn("↺  Зіграти знову", _winPanel.transform, BtnBlue);
            Anchors(again.GetComponent<RectTransform>(), 0.15f,0.19f,0.85f,0.31f);
            again.onClick.AddListener(() => { _winPanel.SetActive(false); GameManager.Instance.Restart(); });

            _winPanel.SetActive(false);
        }

        private void BuildLosePanel(Transform root)
        {
            _losePanel = Panel("LosePanel", root, new Color(0.05f,0.03f,0.15f,0.96f));
            Anchors(_losePanel.GetComponent<RectTransform>(), 0,0,1,1);

            var t = Txt("T", _losePanel.transform, "😔 Немає ходів!", 54, TextAnchor.MiddleCenter);
            t.color = new Color(1f,0.38f,0.38f);
            Anchors(t.rectTransform, 0.05f,0.55f,0.95f,0.72f);

            var r = Btn("↺  Спробувати знову", _losePanel.transform, BtnRed);
            Anchors(r.GetComponent<RectTransform>(), 0.15f,0.38f,0.85f,0.52f);
            r.onClick.AddListener(() => { _losePanel.SetActive(false); GameManager.Instance.Restart(); });

            _losePanel.SetActive(false);
        }

        private void ShowWin()
        {
            if (_winMovesText)
                _winMovesText.text = $"Виконано за {GameManager.Instance.State.MoveCount} ходів";
            _winPanel?.SetActive(true);
        }

        private void ShowLose() => _losePanel?.SetActive(true);

        // ── Factory ───────────────────────────────────────────────────────────────

        private static GameObject Panel(string n, Transform p, Color c)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(p, false);
            go.GetComponent<Image>().color = c;
            return go;
        }

        private static Text Txt(string n, Transform p, string s, int size, TextAnchor a)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(p, false);
            var t = go.GetComponent<Text>();
            t.text = s; t.fontSize = size; t.alignment = a;
            t.color = Color.white;
            t.font  = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.raycastTarget = false;
            return t;
        }

        private static Button Btn(string label, Transform p, Color c)
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
            t.font  = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
