using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockMeow.EditorTools
{
    /// <summary>
    /// README media, recorded in Play mode: screenshots of the main screens and a scripted highlight run.
    /// Frames go to Temp/Media; Tools/media/make_media.py edits them (captions, cuts, GIF) and
    /// "Encode Highlight Video" turns the edited frames into docs/media/highlight.mp4.
    /// While recording, animations step at a fixed rate (Time.captureDeltaTime, see <see cref="Clock"/>),
    /// and the player's save file is put back when the run ends.
    /// </summary>
    public static class MediaCapture
    {
        public const int Fps = 30;
        static readonly Vector2Int FrameSize = new Vector2Int(720, 1280);

        static string ProjectDir => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        static string MediaTemp => Path.Combine(ProjectDir, "Temp", "Media");
        static string RawDir => Path.Combine(MediaTemp, "raw");
        static string ShotsDir => Path.Combine(MediaTemp, "shots");
        static string EditedDir => Path.Combine(MediaTemp, "edited");
        static string VideoPath => Path.Combine(ProjectDir, "docs", "media", "highlight.mp4");
        static string SavePath => Path.Combine(Application.persistentDataPath, "blockmeow.json");
        static string BackupPath => SavePath + ".capture";

        static readonly Stack<IEnumerator> Script = new Stack<IEnumerator>();
        static readonly StringBuilder Marks = new StringBuilder();
        static int _lastFrame, _wait, _frames, _skipped;
        static bool _running, _recording;
        static Texture2D _readback;
        static Image _hand, _caption;
        static Vector2 _handAt;
        static RectTransform _strip, _endCard;

        [MenuItem("BlockMeow/Media/Capture Screenshots")]
        public static void CaptureScreenshots() => Begin(Screenshots());

        [MenuItem("BlockMeow/Media/Record Highlight")]
        public static void RecordHighlight() => Begin(Highlight());

        [MenuItem("BlockMeow/Media/Capture Screenshots", true)]
        [MenuItem("BlockMeow/Media/Record Highlight", true)]
        static bool CanRun() => EditorApplication.isPlaying && !_running;

        public static bool Running => _running;

        // ------------------------------------------------------------------ runner

        static void Begin(IEnumerator script)
        {
            if (!EditorApplication.isPlaying || GameSession.I == null || UIRoot.I == null) { Debug.LogError("[Media] Enter Play mode first."); return; }
            if (_running) { Debug.LogWarning("[Media] A capture is already running."); return; }
            Profile.Save();
            if (File.Exists(SavePath)) File.Copy(SavePath, BackupPath, true);
            Script.Clear();
            Script.Push(script);
            Marks.Clear();
            _lastFrame = Time.frameCount;
            _wait = _frames = _skipped = 0;
            _recording = false;
            _running = true;
            Application.runInBackground = true;
            Time.captureDeltaTime = 1f / Fps;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        static void Tick()
        {
            if (!_running) return;
            int f = Time.frameCount;
            if (f == _lastFrame) return; // one script step per rendered frame
            if (_recording && f - _lastFrame > 1) _skipped += f - _lastFrame - 1;
            _lastFrame = f;
            if (_recording) Grab(Path.Combine(RawDir, $"f{_frames++:D5}.png"), FrameSize);
            if (_wait > 0) { _wait--; return; }
            bool more;
            try { more = Step(); }
            catch (Exception e) { Debug.LogException(e); more = false; }
            if (!more) End();
        }

        /// <summary>Runs the script until it yields. Yield an int to wait that many frames, or an IEnumerator to run it first.</summary>
        static bool Step()
        {
            while (Script.Count > 0)
            {
                var top = Script.Peek();
                if (!top.MoveNext()) { Script.Pop(); continue; }
                if (top.Current is IEnumerator nested) { Script.Push(nested); continue; }
                if (top.Current is int frames) _wait = Mathf.Max(0, frames - 1);
                return true;
            }
            return false;
        }

        static void End()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            _running = false;
            _recording = false;
            Script.Clear();
            Time.captureDeltaTime = 0f;
            if (_hand) UnityEngine.Object.Destroy(_hand.gameObject);
            if (_strip) UnityEngine.Object.Destroy(_strip.gameObject);
            if (_endCard) UnityEngine.Object.Destroy(_endCard.gameObject);
            if (_readback) UnityEngine.Object.DestroyImmediate(_readback);
            if (Marks.Length > 0)
            {
                Directory.CreateDirectory(MediaTemp);
                File.WriteAllText(Path.Combine(MediaTemp, "markers.txt"), Marks.ToString());
            }
            RestoreSave(true);
            Debug.Log($"[Media] done: {_frames} frames ({_skipped} skipped) in {MediaTemp}");
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingPlayMode) return;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayMode;
            _running = false;
            Time.captureDeltaTime = 0f;
            // the game saves on the way out, so put the file back once the editor is idle again
            EditorApplication.delayCall += () => RestoreSave(false);
        }

        static void RestoreSave(bool reload)
        {
            if (!File.Exists(BackupPath)) return;
            File.Copy(BackupPath, SavePath, true);
            File.Delete(BackupPath);
            if (!reload || !EditorApplication.isPlaying) return;
            if (GameSession.I != null && GameSession.I.State != Phase.Idle) GameSession.I.LeaveToHome();
            Profile.Load();
            UIRoot.I.CloseAll();
            UIRoot.I.ShowHome();
        }

        static int Sec(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * Fps));

        static void Mark(string name) => Marks.AppendLine($"{name} {_frames}");

        static void Record(bool on)
        {
            if (on) Directory.CreateDirectory(RawDir);
            _recording = on;
        }

        /// <summary>Copies the last rendered frame, scaled to <paramref name="size"/>, into a PNG.</summary>
        static void Grab(string path, Vector2Int size)
        {
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            // the capture holds display (sRGB) values in a linear texture: copy without any color conversion
            var rt = RenderTexture.GetTemporary(size.x, size.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(shot, rt);
            if (_readback == null || _readback.width != size.x || _readback.height != size.y)
            {
                if (_readback) UnityEngine.Object.DestroyImmediate(_readback);
                _readback = new Texture2D(size.x, size.y, TextureFormat.RGB24, false, true);
            }
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            _readback.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            _readback.Apply(false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.Destroy(shot);
            File.WriteAllBytes(path, _readback.EncodeToPNG());
        }

        static void Shot(string name)
        {
            Directory.CreateDirectory(ShotsDir);
            Grab(Path.Combine(ShotsDir, name + ".png"), new Vector2Int(Screen.width, Screen.height));
        }

        // ------------------------------------------------------------------ showcase state

        // a suspended classic game set up for a combo: rows listed top (y = 7) to bottom, digits are palette colors
        static readonly string[] Board =
        {
            "22..44.6",
            "2..54..6",
            "1133557.",
            "1833557.",
            ".88.77..",
            "33...227",
            "44666..1",
            "44655..1",
        };
        // tray: vertical 2 (finishes rows 4 and 5), 2x2 (rows 0 and 1), horizontal 3 (row 2)
        static readonly int[] TrayShapes = { 3, 2, 9 }, TrayColors = { 5, 8, 3 };

        /// <summary>A save that shows the game off: a few cats, coins, and the combo board above as the game to resume.</summary>
        static void SetupShowcase()
        {
            if (GameSession.I.State != Phase.Idle) GameSession.I.LeaveToHome();
            UIRoot.I.CloseAll();
            var d = Profile.D;
            d.coins = 2480;
            d.bestClassic = 9870;
            d.theme = 0;
            d.tutorialDone = d.skillTipShown = true;
            d.trialCat = -1;
            d.lastLoginDay = Profile.Today;
            d.catBoxFreeDay = -1;
            d.catBoxesOpened = Profile.GuaranteedNewBoxes;
            d.bestSeed = 20261005;
            d.gamesSinceInterstitial = 0;
            d.cats.Clear();
            Array.Clear(d.catLevel, 0, d.catLevel.Length);
            foreach (var (cat, level) in new[] { (0, 3), (1, 2), (2, 1), (3, 1), (5, 2), (9, 1), (11, 1) })
            {
                d.cats.Add(cat);
                d.catLevel[cat] = level;
            }
            d.mascot = 0;
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                {
                    char ch = Board[7 - y][x];
                    d.savedColors[y * 8 + x] = ch == '.' ? 0 : ch - '0';
                    d.savedGems[y * 8 + x] = 0;
                }
            for (int i = 0; i < 3; i++) { d.savedTray[i] = TrayShapes[i]; d.savedTrayColor[i] = TrayColors[i]; }
            d.savedScore = 1280;
            d.savedCombo = 1;
            d.savedMiss = 0;
            d.savedSeed = 20261005;
            d.savedGauge = 0;
            d.savedSkillUses = 0;
            d.savedPlaced = 24;
            d.hasSaved = true;
            Profile.MarkDirty();
            UIRoot.I.ShowHome();
        }

        // ------------------------------------------------------------------ scripts

        static IEnumerator Highlight()
        {
            SetupShowcase();
            Caption("냥이와 함께하는 블록 퍼즐");
            yield return Sec(0.6f);
            Record(true);

            Mark("home");
            yield return Sec(1.2f);
            yield return Tap(Button("클래식"));
            yield return Sec(0.8f);
            GameSession.I.DebugChargeSkill(2);
            Caption("끌어다 놓고, 한 줄을 채우면 펑!");
            yield return Sec(0.3f);

            Mark("play");
            yield return Drag(1, 7, 4);
            yield return Settle(0.6f);
            Mark("combo");
            Caption("연달아 지우면 콤보 → 피버 x2", Pal.Pink);
            yield return Drag(2, 5, 0);
            yield return Settle(0.5f);
            yield return Drag(0, 2, 2);
            Mark("fever");
            yield return Settle(0.8f);

            Caption("지운 줄만큼 냥이 스킬 충전, 톡!", Pal.Blue);
            yield return Sec(0.4f);
            Mark("skill");
            yield return Tap(GameObject.Find("Partner/Cat"));
            yield return Settle(1.6f);

            Mark("more");
            Caption("24마리 냥이, 저마다 다른 스킬");
            for (int i = 0; i < 3; i++)
            {
                if (!GameSession.I.DebugBestMove(out int slot, out int x, out int y)) break;
                yield return Drag(slot, x, y);
                yield return Settle(0.4f);
            }
            yield return Sec(0.4f);
            HideHand();

            GameSession.I.LeaveToHome();
            UIRoot.I.ShowCats();
            Caption("매일 무료 냥이 상자", Pal.Green);
            Mark("cats");
            yield return Sec(1.0f);
            UnityEngine.Random.InitState(LegendarySeed());
            yield return Tap(Button("매일 무료"));
            Mark("catbox");
            yield return Sec(3.2f);
            yield return Tap(Button("확인"));
            yield return Sec(0.4f);

            UIRoot.I.ShowHome();
            Caption("도전장 보내고 같은 조각으로 승부!", Pal.Purple);
            yield return Sec(0.5f);
            yield return Tap(Button("친구 대결"));
            Mark("challenge");
            yield return Sec(2.2f);
            HideHand();

            Mark("endcard");
            EndCard();
            yield return Sec(3.0f);
            Mark("end");
            Record(false);
        }

        // ------------------------------------------------------------------ captions and the end card (drawn with the game's own UI)

        /// <summary>A paper strip over the test banner with a sticky note caption; the previous caption flips away.</summary>
        static void Caption(string text, Color? color = null)
        {
            if (_strip == null)
            {
                _strip = UIKit.Rect("CaptureStrip", UIRoot.I.TopLayer).Bottom(0, UIRoot.BannerH);
                UIKit.Img(_strip, null, Pal.Paper, "Paper").rectTransform.Fill();
                UIKit.Img(_strip, null, Pal.Ink, "Rule").rectTransform.Top(0, 3);
            }
            _strip.SetAsLastSibling();
            if (_caption)
            {
                var old = _caption;
                Tweener.Scale(old.transform, Vector3.zero, 0.16f, Ease.InBack).OnComplete(() => { if (old) UnityEngine.Object.Destroy(old.gameObject); });
            }
            _caption = UIKit.Sticky(_strip, color ?? Pal.Sticky, UnityEngine.Random.Range(-2.5f, -1f), true, "Caption");
            _caption.rectTransform.At(0.5f, 0.5f, 0, 6, 980, 104);
            var t = UIKit.Txt(_caption.transform, text, 46, Pal.Ink);
            t.rectTransform.Fill(24, 0, 24, 0);
            _caption.transform.localScale = Vector3.one * 0.5f;
            Tweener.Scale(_caption.transform, Vector3.one, 0.28f, Ease.OutBack);
        }

        /// <summary>Closing card: the logo, the mascot and three highlighted features on graph paper.</summary>
        static void EndCard()
        {
            HideHand();
            _endCard = UIKit.Rect("CaptureEnd", UIRoot.I.TopLayer).Fill();
            var cg = _endCard.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            Tweener.Fade(cg, 1f, 0.3f);
            UIKit.Img(_endCard, null, Pal.Paper, "Paper").rectTransform.Fill();
            var grid = UIKit.Img(_endCard, "ui_graph", Color.white, "Grid");
            grid.type = Image.Type.Tiled;
            grid.pixelsPerUnitMultiplier = 1.45f;
            grid.rectTransform.Fill();

            var logo = UIKit.Img(_endCard, "logo_note", Color.white, "Logo");
            logo.preserveAspect = true;
            logo.rectTransform.At(0.5f, 0.5f, 0, 560, 640, 340);
            Tweener.PopIn(logo.transform, 0.45f, 0.1f);
            var sub = UIKit.Txt(_endCard, "BLOCK MEOW · 블록 퍼즐", 40, Pal.TextDim);
            sub.rectTransform.At(0.5f, 0.5f, 0, 345, 900, 60);
            var cat = CatView.Create(_endCard, 0, 400);
            cat.GetComponent<RectTransform>().At(0.5f, 0.5f, 0, 80, 400, 400);
            cat.SetMood(CatMood.Happy, 99f);

            string[] lines = { "냥이 스킬로 위기 탈출", "친구와 같은 조각으로 대결", "매일 무료 냥이 상자" };
            Color[] marks = { Pal.Pink, Pal.Blue, Pal.Yellow };
            for (int i = 0; i < lines.Length; i++)
            {
                var row = UIKit.Rect("Feature" + i, _endCard);
                row.At(0.5f, 0.5f, 0, -240 - i * 110, 760, 90);
                var swipe = UIKit.Img(row, "ui_hl_swipe", marks[i], "Swipe");
                swipe.rectTransform.Fill(40, 30, 40, 4);
                swipe.rectTransform.localRotation = Quaternion.Euler(0, 0, i % 2 == 0 ? -1f : 1f);
                var t = UIKit.Txt(row, lines[i], 50, Pal.Ink);
                t.rectTransform.Fill();
                Tweener.PopIn(row, 0.35f, 0.35f + i * 0.18f);
            }
            var note = UIKit.Sticky(_endCard, null, -3f, true, "Free");
            note.rectTransform.At(0.5f, 0.5f, 0, -640, 620, 110);
            var nt = UIKit.Txt(note.transform, "인앱 결제 없이 100% 무료", 44, Pal.Ink);
            nt.rectTransform.Fill(20, 0, 20, 0);
            Tweener.PopIn(note.transform, 0.35f, 1.0f);
        }

        static IEnumerator Screenshots()
        {
            SetupShowcase();
            yield return Sec(1.2f);
            Shot("home");

            GameSession.I.StartClassic(true);
            yield return Sec(1.0f);
            GameSession.I.DebugChargeSkill(2);
            yield return Sec(0.2f);
            // first move clears two rows; then hold the second piece over the board to show the line preview
            yield return Drag(1, 7, 4);
            yield return Settle(0.5f);
            yield return Drag(2, 5, 0, hold: true);
            HideHand();
            yield return 1;
            Shot("game");
            GameSession.I.PointerUp(GameSession.I.DebugDropScreen(2, 5, 0));
            yield return Settle(0.4f);
            yield return Drag(0, 2, 2);
            yield return Settle(0.4f);
            yield return Tap(GameObject.Find("Partner/Cat"));
            yield return Sec(0.45f);
            HideHand();
            yield return 1;
            Shot("skill");
            yield return Settle(1.5f);

            GameSession.I.LeaveToHome();
            UIRoot.I.ShowCats();
            yield return Sec(1.0f);
            Shot("cats");
            UnityEngine.Random.InitState(LegendarySeed());
            yield return Tap(Button("매일 무료"));
            HideHand();
            yield return Sec(2.6f);
            Shot("catbox");
            UIRoot.I.CloseAll();
            yield return Sec(0.4f);

            UIRoot.I.ShowHome();
            UIRoot.I.Open<ChallengePopup>();
            yield return Sec(1.0f);
            Shot("challenge");
            UIRoot.I.CloseAll();
            UIRoot.I.ShowAdventure();
            yield return Sec(1.0f);
            Shot("adventure");
        }

        /// <summary>A seed whose next cat box roll is legendary, so the reveal in the video is the shiny one.</summary>
        static int LegendarySeed()
        {
            for (int seed = 1; seed < 10000; seed++)
            {
                UnityEngine.Random.InitState(seed);
                if (UnityEngine.Random.value * 100f < Profile.CatBoxOdds[2]) return seed;
            }
            return 1;
        }

        // ------------------------------------------------------------------ input (a drawn finger shows what happens)

        static IEnumerator Drag(int slot, int x, int y, bool hold = false)
        {
            var s = GameSession.I;
            if (s.Tray[slot] == null) yield break;
            Vector2 from = s.DebugSlotScreen(slot), to = s.DebugDropScreen(slot, x, y);
            yield return MoveHand(from, 0.28f);
            SetPressed(true);
            s.PointerDown(from);
            yield return Sec(0.08f);
            int n = Sec(0.42f);
            for (int i = 1; i <= n; i++)
            {
                float k = i / (float)n;
                k = k * k * (3f - 2f * k);
                Vector2 p = Vector2.Lerp(from, to, k) + new Vector2(0f, Mathf.Sin(k * Mathf.PI) * 60f);
                s.PointerDrag(p);
                PlaceHand(p);
                yield return 1;
            }
            yield return Sec(0.16f);
            if (hold) yield break;
            s.PointerUp(to);
            SetPressed(false);
        }

        static IEnumerator Tap(GameObject go)
        {
            if (go == null) { Debug.LogWarning("[Media] tap target missing"); yield break; }
            var rt = (RectTransform)go.transform;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rt.TransformPoint(rt.rect.center));
            yield return MoveHand(screen, 0.32f);
            var e = new PointerEventData(EventSystem.current) { position = screen, button = PointerEventData.InputButton.Left };
            SetPressed(true);
            ExecuteEvents.Execute(go, e, ExecuteEvents.pointerDownHandler);
            yield return Sec(0.12f);
            ExecuteEvents.Execute(go, e, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(go, e, ExecuteEvents.pointerClickHandler);
            SetPressed(false);
        }

        /// <summary>Waits until the board animation is done, then a little longer.</summary>
        static IEnumerator Settle(float extra)
        {
            for (int i = 0; i < Fps * 4 && GameSession.I.Busy; i++) yield return 1;
            yield return Sec(extra);
        }

        static GameObject Button(string label)
        {
            foreach (var b in UIRoot.I.GetComponentsInChildren<UnityEngine.UI.Button>())
                if (b.name == "Btn_" + label && b.gameObject.activeInHierarchy) return b.gameObject;
            return null;
        }

        static IEnumerator MoveHand(Vector2 to, float seconds)
        {
            bool fresh = _hand == null || !_hand.gameObject.activeSelf;
            if (fresh)
            {
                ShowHand();
                _handAt = to + new Vector2(140f, -220f);
            }
            Vector2 from = _handAt;
            int n = Sec(seconds);
            for (int i = 1; i <= n; i++)
            {
                float k = i / (float)n;
                k = 1f - (1f - k) * (1f - k);
                PlaceHand(Vector2.Lerp(from, to, k));
                if (fresh) _hand.color = new Color(1f, 1f, 1f, Mathf.Clamp01(k * 2f));
                yield return 1;
            }
        }

        static void ShowHand()
        {
            if (_hand == null)
            {
                _hand = UIKit.Img(UIRoot.I.TopLayer, "hand", Color.white, "CaptureHand");
                var rt = _hand.rectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.45f, 0.95f); // the fingertip
                rt.sizeDelta = new Vector2(150f, 150f);
            }
            _hand.gameObject.SetActive(true);
            _hand.transform.SetAsLastSibling();
        }

        static void HideHand()
        {
            if (_hand) _hand.gameObject.SetActive(false);
        }

        static void PlaceHand(Vector2 screen)
        {
            _handAt = screen;
            if (_hand == null) return;
            var top = UIRoot.I.TopLayer;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(top, screen, null, out var local);
            _hand.rectTransform.anchoredPosition = local - top.rect.min;
        }

        static void SetPressed(bool down)
        {
            if (_hand) _hand.rectTransform.localScale = Vector3.one * (down ? 0.88f : 1f);
        }

        // ------------------------------------------------------------------ video

        /// <summary>Encodes Temp/Media/edited/*.png (from make_media.py) into docs/media/highlight.mp4 (H.264).</summary>
        [MenuItem("BlockMeow/Media/Encode Highlight Video")]
        public static void EncodeVideo()
        {
            var files = Directory.Exists(EditedDir) ? Directory.GetFiles(EditedDir, "*.png") : new string[0];
            if (files.Length == 0) { Debug.LogError("[Media] No edited frames in " + EditedDir); return; }
            Array.Sort(files, StringComparer.Ordinal);
            var frame = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            frame.LoadImage(File.ReadAllBytes(files[0]));
            int w = frame.width, h = frame.height;
            var attrs = new VideoTrackAttributes
            {
                frameRate = new MediaRational(Fps),
                width = (uint)w,
                height = (uint)h,
                includeAlpha = false,
                bitRateMode = VideoBitrateMode.Medium,
            };
            Directory.CreateDirectory(Path.GetDirectoryName(VideoPath));
            var rgba = new Texture2D(w, h, TextureFormat.RGBA32, false);
            try
            {
                using (var encoder = new MediaEncoder(VideoPath, attrs))
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar("Encoding highlight", Path.GetFileName(files[i]), i / (float)files.Length)) break;
                        frame.LoadImage(File.ReadAllBytes(files[i]));
                        rgba.SetPixels32(frame.GetPixels32());
                        rgba.Apply(false);
                        encoder.AddFrame(rgba);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                UnityEngine.Object.DestroyImmediate(frame);
                UnityEngine.Object.DestroyImmediate(rgba);
            }
            Debug.Log($"[Media] wrote {VideoPath} ({files.Length} frames, {w}x{h} @ {Fps}fps)");
        }
    }
}
