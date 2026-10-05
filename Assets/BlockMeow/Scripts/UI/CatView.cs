using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockMeow
{
    /// <summary>Layered, animated cat face (fur, pattern, eyes, mouth, accessory) with moods and a tap "meow".</summary>
    public sealed class CatView : MonoBehaviour, IPointerClickHandler
    {
        Image _head, _pattern, _ear, _line, _muzzle, _blush, _whisk, _eyes, _mouth, _acc, _shadow;
        RectTransform _body;
        CatMood _mood = CatMood.Idle;
        float _moodT, _blinkT = 2f, _bob;
        bool _silhouette;
        public bool Tappable = true;
        /// <summary>Replaces the default "meow" reaction (the in-game partner fires its skill).</summary>
        public System.Action OnTap;

        public static CatView Create(Transform parent, int cat, float size)
        {
            var rt = UIKit.Rect("Cat", parent);
            rt.sizeDelta = new Vector2(size, size);
            var v = rt.gameObject.AddComponent<CatView>();
            v.Build();
            v.SetCat(cat);
            return v;
        }

        void Build()
        {
            var hit = gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            _shadow = UIKit.Img(transform, "ui_radial", Pal.Ink.WithA(0.14f), "Shadow");
            _shadow.rectTransform.anchorMin = new Vector2(0.15f, -0.02f);
            _shadow.rectTransform.anchorMax = new Vector2(0.85f, 0.14f);
            _shadow.rectTransform.offsetMin = _shadow.rectTransform.offsetMax = Vector2.zero;
            _body = UIKit.Rect("Body", transform).Fill();
            _body.pivot = new Vector2(0.5f, 0.1f);
            _body.anchoredPosition = Vector2.zero;
            _head = Layer("cat_head");
            _pattern = Layer("cat_tabby");
            _ear = Layer("cat_ear_in");
            _line = Layer("cat_line");
            _muzzle = Layer("cat_muzzle");
            _blush = Layer("cat_blush");
            _whisk = Layer("cat_whisk");
            _eyes = Layer("cat_eye_open");
            _mouth = Layer("cat_mouth_w");
            _acc = Layer("acc_bow");
        }

        Image Layer(string sprite)
        {
            var img = UIKit.Img(_body, sprite, Color.white, sprite);
            img.rectTransform.Fill();
            return img;
        }

        public void SetCat(int index)
        {
            var c = Cats.Get(index);
            _head.color = c.Fur;
            _pattern.gameObject.SetActive(c.Pattern != CatPattern.None);
            if (c.Pattern != CatPattern.None)
            {
                _pattern.sprite = Atlas.Get(c.Pattern == CatPattern.Tabby ? "cat_tabby" : c.Pattern == CatPattern.Patch ? "cat_patch" : "cat_mask");
                _pattern.color = c.PatternColor;
            }
            _acc.gameObject.SetActive(!string.IsNullOrEmpty(c.Accessory));
            if (!string.IsNullOrEmpty(c.Accessory)) _acc.sprite = Atlas.Get(c.Accessory);
            SetSilhouette(false);
        }

        /// <summary>Cats not met yet are drawn as a pencil outline with a gray fill.</summary>
        public void SetSilhouette(bool on)
        {
            _silhouette = on;
            foreach (var img in new[] { _pattern, _ear, _muzzle, _blush, _whisk, _eyes, _mouth, _acc }) img.enabled = !on;
            if (on) _head.color = Pal.Hex("#DDE2EC");
        }

        public void SetMood(CatMood mood, float hold = 1.6f)
        {
            _mood = mood;
            _moodT = mood == CatMood.Idle || mood == CatMood.Sad ? 999f : hold;
            Apply();
            if (mood == CatMood.Wow || mood == CatMood.Happy) Tweener.Punch(_body, 0.12f, 0.3f);
        }

        void Apply()
        {
            if (_silhouette) return;
            switch (_mood)
            {
                case CatMood.Happy: _eyes.sprite = Atlas.Get("cat_eye_happy"); _mouth.sprite = Atlas.Get("cat_mouth_smile"); break;
                case CatMood.Wow: _eyes.sprite = Atlas.Get("cat_eye_wow"); _mouth.sprite = Atlas.Get("cat_mouth_o"); break;
                case CatMood.Sad: _eyes.sprite = Atlas.Get("cat_eye_sad"); _mouth.sprite = Atlas.Get("cat_mouth_sad"); break;
                case CatMood.Sleepy: _eyes.sprite = Atlas.Get("cat_eye_blink"); _mouth.sprite = Atlas.Get("cat_mouth_w"); break;
                default: _eyes.sprite = Atlas.Get("cat_eye_open"); _mouth.sprite = Atlas.Get("cat_mouth_w"); break;
            }
        }

        void Update()
        {
            float dt = Clock.Dt;
            _bob += dt;
            float speed = _mood == CatMood.Wow || _mood == CatMood.Happy ? 7f : 2.4f;
            float s = Mathf.Sin(_bob * speed);
            _body.localScale = new Vector3(1f + s * 0.02f, 1f - s * 0.025f, 1f);
            _body.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_bob * 1.3f) * (_mood == CatMood.Sad ? 1f : 3f));

            if (_moodT < 900f)
            {
                _moodT -= dt;
                if (_moodT <= 0f) { _mood = CatMood.Idle; Apply(); }
            }
            if (_mood == CatMood.Idle && !_silhouette)
            {
                _blinkT -= dt;
                if (_blinkT <= 0f && _blinkT > -0.13f) _eyes.sprite = Atlas.Get("cat_eye_blink");
                else if (_blinkT <= -0.13f) { _eyes.sprite = Atlas.Get("cat_eye_open"); _blinkT = Random.Range(2f, 4.5f); }
            }
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (OnTap != null) { OnTap(); return; }
            if (!Tappable || _silhouette) return;
            AudioManager.Play(Sfx.Meow, 1f, Random.Range(0.92f, 1.12f));
            SetMood(CatMood.Happy, 1.2f);
        }
    }
}
