using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using WaterSort.Model;

namespace WaterSort
{
    public class FlaskView : MonoBehaviour, IPointerClickHandler
    {
        [Header("Вигляд")]
        [SerializeField] private Color tubeOutlineColor = new Color(1f, 1f, 1f, 0.88f);
        [SerializeField] private Color tubeBgColor      = new Color(0.25f, 0.18f, 0.45f, 0.5f);
        [SerializeField] private Color emptySlotColor   = new Color(1f, 1f, 1f, 0.04f);
        [SerializeField] private Color selectedTint     = new Color(1f, 0.85f, 0f, 1f);

        private int           _index;
        private System.Action _onClick;
        private int           _capacity;
        private List<Image>   _slots = new();
        private Vector2       _basePos;
        private Image         _glowImg;
        private RectTransform _liquidRoot;

        public void Init(int index, FlaskData data, System.Action onClick)
        {
            _index    = index;
            _onClick  = onClick;
            _capacity = data.capacity;
            BuildHierarchy();
            _basePos = RT().anchoredPosition;
            FillSlots(data);
        }

        public void RefreshLayers(FlaskData data)
        {
            _capacity = data.capacity;
            EnsureSlots(_capacity);
            ApplyColors(data);
        }

        public void SetSelected(bool on)
        {
            if (_glowImg) _glowImg.enabled = on;
            StopAllCoroutines();
            Vector2 target = on ? _basePos + Vector2.up * 18f : _basePos;
            StartCoroutine(CoMoveTo(target, 0.12f));
        }

        public void OnPointerClick(PointerEventData e) => _onClick?.Invoke();

        public IEnumerator PlayPourTo(RectTransform dstRT, Color streamColor, float streamDur)
        {
            var canvas   = GetComponentInParent<Canvas>();
            var parentRT = RT().parent as RectTransform;

            Vector2 origin   = _basePos;
            float   flaskH   = RT().sizeDelta.y;
            float   flaskW   = RT().sizeDelta.x;
            
            float   liftH    = flaskH * 0.95f;

            Vector2 dstLocal;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentRT,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, dstRT.position),
                canvas.worldCamera, out dstLocal);

            bool goRight = dstLocal.x > origin.x;

            yield return CoMoveTo(origin + Vector2.up * liftH, 0.16f);

            float centerTweak = 15f; 
            float spoutOffsetX = (flaskH * 0.45f) - centerTweak; 
            
            float targetX = dstLocal.x + (goRight ? -spoutOffsetX : spoutOffsetX);
            Vector2 aboveDst = new Vector2(targetX, origin.y + liftH);
            yield return CoMoveTo(aboveDst, 0.20f);

            float tiltAngle = goRight ? -105f : 105f;
            yield return CoTiltTo(tiltAngle, 0.18f);

            yield return CoVerticalStream(streamColor, dstRT, canvas, streamDur, goRight);

            yield return CoTiltTo(0f, 0.18f);

            yield return CoMoveTo(origin + Vector2.up * liftH, 0.14f);
            yield return CoMoveTo(origin, 0.16f);

            RT().anchoredPosition   = origin;
            transform.localRotation = Quaternion.identity;
        }

        private IEnumerator CoVerticalStream(Color color, RectTransform dstRT, Canvas canvas, float dur, bool goRight)
        {
            var canvasRT = canvas.GetComponent<RectTransform>();

            var sGO  = new GameObject("Stream", typeof(RectTransform), typeof(Image));
            sGO.transform.SetParent(canvasRT, false);
            var sRT  = sGO.GetComponent<RectTransform>();
            var sImg = sGO.GetComponent<Image>();
            sImg.color = color;
            sImg.raycastTarget = false;
            sRT.pivot     = new Vector2(0.5f, 1f);
            sRT.anchorMin = sRT.anchorMax = new Vector2(0.5f, 0.5f);

            var bGO  = new GameObject("Blob", typeof(RectTransform), typeof(Image));
            bGO.transform.SetParent(canvasRT, false);
            var bRT  = bGO.GetComponent<RectTransform>();
            var bImg = bGO.GetComponent<Image>();
            bImg.color = color;
            bImg.raycastTarget = false;
            bRT.anchorMin = bRT.anchorMax = new Vector2(0.5f, 0.5f);
            bRT.sizeDelta = new Vector2(20f, 20f);

            float elapsed = 0f;
            float streamW = 12f;

            while (elapsed < dur)
            {
                float   xOff   = goRight ?  RT().sizeDelta.x * 0.35f : -RT().sizeDelta.x * 0.35f;
                Vector2 spout  = FlaskLocalToCanvas(new Vector2(xOff, RT().sizeDelta.y * 0.45f), canvasRT, canvas);

                Vector2 dstTop = FlaskRTLocalToCanvas(dstRT, new Vector2(0f, dstRT.sizeDelta.y * 0.45f), canvasRT, canvas);

                float streamLen = Mathf.Max(4f, spout.y - dstTop.y);

                sRT.anchoredPosition = spout;
                sRT.sizeDelta        = new Vector2(streamW, streamLen);
                sRT.localRotation    = Quaternion.identity;

                float pulse = 1f + 0.12f * Mathf.Sin(elapsed * 35f);
                sRT.sizeDelta = new Vector2(streamW * pulse, streamLen);

                bRT.anchoredPosition = new Vector2(spout.x, dstTop.y);
                float blobScale = 1f + 0.2f * Mathf.Sin(elapsed * 20f);
                bRT.sizeDelta = new Vector2(20f * blobScale, 20f * blobScale);

                elapsed += Time.deltaTime;
                yield return null;
            }

            Destroy(sGO);
            Destroy(bGO);
        }

        private Vector2 FlaskLocalToCanvas(Vector2 localPoint, RectTransform canvasRT, Canvas canvas)
        {
            return FlaskRTLocalToCanvas(RT(), localPoint, canvasRT, canvas);
        }

        private static Vector2 FlaskRTLocalToCanvas(RectTransform rt, Vector2 localPt, RectTransform canvasRT, Canvas canvas)
        {
            Vector3 world  = rt.TransformPoint(new Vector3(localPt.x, localPt.y, 0f));
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screen, canvas.worldCamera, out Vector2 result);
            return result;
        }

        private IEnumerator CoMoveTo(Vector2 target, float dur)
        {
            var rt = RT(); 
            Vector2 start = rt.anchoredPosition;
            for (float t = 0; t < dur; t += Time.deltaTime)
            { 
                rt.anchoredPosition = Vector2.Lerp(start, target, Mathf.SmoothStep(0, 1, t / dur)); 
                yield return null; 
            }
            rt.anchoredPosition = target;
        }

        private IEnumerator CoTiltTo(float angleDeg, float dur)
        {
            float start = transform.localEulerAngles.z;
            if (start > 180f) start -= 360f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float a = Mathf.LerpAngle(start, angleDeg, Mathf.SmoothStep(0, 1, t / dur));
                transform.localRotation = Quaternion.Euler(0, 0, a);
                yield return null;
            }
            transform.localRotation = Quaternion.Euler(0, 0, angleDeg);
        }

        private void BuildHierarchy()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) 
                Destroy(transform.GetChild(i).gameObject);
            _slots.Clear();

            var selfImg = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            selfImg.color = Color.clear; 
            selfImg.raycastTarget = true;

            _glowImg = MakeImg("Glow", transform, selectedTint);
            _glowImg.rectTransform.anchorMin = new Vector2(-0.1f, -0.03f);
            _glowImg.rectTransform.anchorMax = new Vector2(1.1f, 1.03f);
            _glowImg.rectTransform.offsetMin = _glowImg.rectTransform.offsetMax = Vector2.zero;
            TrySprite(_glowImg, "SelectionGlow", Image.Type.Sliced);
            _glowImg.enabled = false;

            var bg = MakeImg("BG", transform, tubeBgColor);
            Stretch(bg.rectTransform); 
            TrySprite(bg, "TubeMask", Image.Type.Sliced);

            var maskGO = new GameObject("Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGO.transform.SetParent(transform, false);
            Stretch(maskGO.GetComponent<RectTransform>());
            var mImg = maskGO.GetComponent<Image>(); 
            mImg.raycastTarget = false;
            TrySprite(mImg, "TubeMask", Image.Type.Sliced);
            maskGO.GetComponent<Mask>().showMaskGraphic = false;

            var lrGO = new GameObject("LR", typeof(RectTransform));
            lrGO.transform.SetParent(maskGO.transform, false);
            _liquidRoot = lrGO.GetComponent<RectTransform>();
            Stretch(_liquidRoot);

            var outline = MakeImg("Outline", transform, tubeOutlineColor);
            Stretch(outline.rectTransform); 
            TrySprite(outline, "TubeOutline", Image.Type.Sliced);

            var gloss = MakeImg("Gloss", transform, new Color(1, 1, 1, 0.13f));
            Stretch(gloss.rectTransform); 
            TrySprite(gloss, "TubeGloss", Image.Type.Simple);
        }

        private void FillSlots(FlaskData data) 
        { 
            EnsureSlots(_capacity); 
            ApplyColors(data); 
        }

        private void EnsureSlots(int n)
        {
            while (_slots.Count < n) AddSlot(_slots.Count);
            while (_slots.Count > n) RemoveSlot();
            Reposition();
        }

        private void AddSlot(int i)
        {
            var img = MakeImg($"S{i}", _liquidRoot, emptySlotColor);
            img.rectTransform.anchorMin = new Vector2(0, 0);
            img.rectTransform.anchorMax = new Vector2(1, 0);
            img.rectTransform.pivot     = new Vector2(0.5f, 0);
            _slots.Add(img);
        }

        private void RemoveSlot()
        {
            int i = _slots.Count - 1;
            if (_slots[i]) Destroy(_slots[i].gameObject);
            _slots.RemoveAt(i);
        }

        private void Reposition()
        {
            float h = RT().sizeDelta.y; 
            if (h <= 0) h = 210f;
            float segH = h / _capacity;
            for (int i = 0; i < _slots.Count; i++)
            {
                var rt = _slots[i].rectTransform;
                rt.sizeDelta = new Vector2(0, segH);
                rt.anchoredPosition = new Vector2(0, i * segH);
            }
        }

        private void ApplyColors(FlaskData data)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < data.layers.Count)
                { 
                    Color c = GameManager.Instance.GetColor(data.layers[i]); 
                    c.a = 0.90f; 
                    _slots[i].color = c; 
                }
                else 
                {
                    _slots[i].color = emptySlotColor;
                }
            }
        }

        private RectTransform RT() => (RectTransform)transform;

        private static Image MakeImg(string n, Transform p, Color c)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(p, false);
            var img = go.GetComponent<Image>(); 
            img.color = c; 
            img.raycastTarget = false;
            return img;
        }

        private static void Stretch(RectTransform rt)
        { 
            rt.anchorMin = Vector2.zero; 
            rt.anchorMax = Vector2.one; 
            rt.offsetMin = rt.offsetMax = Vector2.zero; 
        }

        private static void TrySprite(Image img, string name, Image.Type type)
        { 
            var s = Resources.Load<Sprite>($"Sprites/{name}"); 
            if (s != null) 
            { 
                img.sprite = s; 
                img.type = type; 
            } 
        }
    }
}