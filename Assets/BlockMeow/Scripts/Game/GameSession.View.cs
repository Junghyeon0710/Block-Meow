using UnityEngine;

namespace BlockMeow
{
    /// <summary>Board rendering, layout (fits any aspect ratio between the HUD and the banner) and animations.</summary>
    public sealed partial class GameSession
    {
        Camera _cam;
        public Effects Fx { get; private set; }
        QuadBatch _bg, _bgAdd, _boardB, _blocks, _hl, _drag, _fxAlpha, _fxAdd;
        UV _uvWhite, _uvBoard, _uvCell, _uvCellGlow, _uvGlow, _uvBoardLine;
        readonly UV[] _uvGem = new UV[4];
        // three hand-drawn variants each, so neighbouring cells never look stamped
        readonly UV[] _uvCellLine = new UV[3], _uvNoteLine = new UV[3];

        float _ppu = 100f, _time, _shake, _overT = -1f, _fever;
        Vector2 _origin, _camPos;
        float _trayY;
        public Vector2 BoardCenter { get; private set; }
        public const float TrayScale = 0.52f;
        public const float SlotSpacing = 2.95f;

        readonly float[] _placeT = new float[64];
        readonly Vector2[] _slotPos = new Vector2[3];
        readonly float[] _slotZoom = new float[3];
        readonly float[] _slotDelay = new float[3];

        public ThemeDef Theme => Themes.Current;

        void InitView(Camera cam)
        {
            _cam = cam;
            Fx = new Effects();
            var t = transform;
            _bg = new QuadBatch("BG", Atlas.SpriteMat, -100, 160, t);
            _bgAdd = new QuadBatch("BGGlow", Atlas.AdditiveMat, -90, 32, t);
            _boardB = new QuadBatch("Board", Atlas.SpriteMat, -50, 112, t);
            _blocks = new QuadBatch("Blocks", Atlas.SpriteMat, 0, 400, t);
            _hl = new QuadBatch("Highlight", Atlas.AdditiveMat, 5, 64, t);
            _drag = new QuadBatch("Drag", Atlas.SpriteMat, 10, 64, t);
            _fxAlpha = new QuadBatch("FxAlpha", Atlas.SpriteMat, 20, 512, t);
            _fxAdd = new QuadBatch("FxAdd", Atlas.AdditiveMat, 30, 512, t);
            _uvWhite = Atlas.Uv("white"); _uvBoard = Atlas.Uv("board"); _uvCell = Atlas.Uv("cell");
            _uvCellGlow = Atlas.Uv("cell_glow"); _uvGlow = Atlas.Uv("p_glow");
            for (int i = 1; i <= 3; i++) _uvGem[i] = Atlas.Uv("gem_" + i);
            _uvBoardLine = Atlas.Uv("board_line");
            for (int i = 0; i < 3; i++)
            {
                _uvCellLine[i] = Atlas.Uv(i == 0 ? "cell_line" : "cell_line" + i);
                _uvNoteLine[i] = Atlas.Uv(i == 0 ? "blk_note_line" : "blk_note_line" + i);
            }
            for (int i = 0; i < 64; i++) _placeT[i] = 10f;
            SetWorldVisible(false);
        }

        void ResetView()
        {
            Fx.Clear();
            _overT = -1f;
            _fever = 0f;
            _dragSlot = -1;
            _ghostValid = false;
            for (int i = 0; i < 64; i++) _placeT[i] = 10f;
        }

        public void SetWorldVisible(bool on)
        {
            foreach (var b in new[] { _bg, _bgAdd, _boardB, _blocks, _hl, _drag, _fxAlpha, _fxAdd }) b.Go.SetActive(on);
        }

        void StartDealAnim()
        {
            for (int i = 0; i < 3; i++)
            {
                _slotDelay[i] = i * 0.06f;
                _slotZoom[i] = 0f;
                _slotPos[i] = SlotPos(i) + new Vector2(5f + i, 0f);
            }
        }

        void MarkPlaced(int x, int y) => _placeT[y * Board.N + x] = 0f;
        public Vector2 CellPos(int x, int y) => _origin + new Vector2(x, y);
        public Vector2 SlotPos(int i) => new Vector2((i - 1) * SlotSpacing, _trayY);
        void Shake(float amount) => _shake = Mathf.Max(_shake, amount);
        void StartGrayOut() => _overT = 0f;
        void StopGrayOut() => _overT = -1f;

        public Vector2 WorldToScreen(Vector2 w)
            => new Vector2(Screen.width * 0.5f + (w.x - _camPos.x) * _ppu, Screen.height * 0.5f + (w.y - _camPos.y) * _ppu);

        public Vector2 ScreenToWorld(Vector2 s)
            => new Vector2((s.x - Screen.width * 0.5f) / _ppu + _camPos.x, (s.y - Screen.height * 0.5f) / _ppu + _camPos.y);

        /// <summary>Fits the board + tray between the HUD (top) and boosters/banner (bottom) on any screen.</summary>
        void Layout()
        {
            float w = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
            float top = GameScreen.TopPx, bottom = GameScreen.BottomPx;
            float availH = Mathf.Max(100f, h - top - bottom);
            const float needW = 9.1f, needH = 8.55f + 0.45f + 3.1f;
            _ppu = Mathf.Min(w / needW, availH / needH);
            _cam.orthographicSize = h / (2f * _ppu);
            float worldTop = (h * 0.5f - top) / _ppu, worldBottom = -(h * 0.5f - bottom) / _ppu;
            float mid = (worldTop + worldBottom) * 0.5f;
            float contentTop = mid + needH * 0.5f;
            BoardCenter = new Vector2(0f, contentTop - 8.55f * 0.5f);
            _origin = BoardCenter - new Vector2(3.5f, 3.5f);
            _trayY = BoardCenter.y - 8.55f * 0.5f - 0.45f - 1.55f;
        }

        void Update()
        {
            if (State == Phase.Idle) return;
            float dt = Mathf.Min(Clock.Dt, 0.05f);
            _time += dt;
            for (int i = 0; i < 64; i++) if (_placeT[i] < 10f) _placeT[i] += dt;
            for (int i = 0; i < 3; i++)
            {
                if (_slotDelay[i] > 0f) { _slotDelay[i] -= dt; continue; }
                if (i == _dragSlot) continue;
                _slotPos[i] = Vector2.Lerp(_slotPos[i], SlotPos(i), 1f - Mathf.Exp(-dt * 16f));
                _slotZoom[i] = Mathf.Lerp(_slotZoom[i], 1f, 1f - Mathf.Exp(-dt * 14f));
            }
            if (_dragSlot >= 0) _dragZoom = Mathf.Lerp(_dragZoom, 1f, 1f - Mathf.Exp(-dt * 20f));
            if (_overT >= 0f) _overT += dt;
            _fever = Mathf.MoveTowards(_fever, Fever && State == Phase.Playing ? 1f : 0f, dt * 2f);
            _shake = Mathf.Max(0f, _shake - dt * 1.8f);
            UpdateSkillTimers(dt);
            Fx.Update(dt, Theme);
        }

        void LateUpdate()
        {
            if (State == Phase.Idle) return;
            Layout();
            float s2 = _shake * _shake;
            _camPos = new Vector2((Mathf.PerlinNoise(_time * 30f, 1.3f) - 0.5f) * s2 * 0.8f, (Mathf.PerlinNoise(2.7f, _time * 30f) - 0.5f) * s2 * 0.8f);
            _cam.transform.position = new Vector3(_camPos.x, _camPos.y, -10f);

            _bg.Clear(); _bgAdd.Clear(); _boardB.Clear(); _blocks.Clear(); _hl.Clear(); _drag.Clear(); _fxAlpha.Clear(); _fxAdd.Clear();
            var th = Theme;
            DrawBackground(th);
            DrawBoard(th);
            DrawTray(th);
            Fx.Draw(_fxAlpha, _fxAlpha, _fxAdd, th);
            _bg.Upload(); _bgAdd.Upload(); _boardB.Upload(); _blocks.Upload(); _hl.Upload(); _drag.Upload(); _fxAlpha.Upload(); _fxAdd.Upload();
        }

        /// <summary>Graph paper; during fever the page gets a pink highlighter wash.</summary>
        void DrawBackground(ThemeDef th)
        {
            float vh = _cam.orthographicSize * 2f + 2f, vw = vh * _cam.aspect + 2f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 4f);
            _bg.Add(_camPos.x, _camPos.y, vw, vh, 0f, _uvWhite, Pal.Paper);
            const float step = 0.4f, w = 0.028f;
            Color32 line = Pal.GridLine;
            float left = _camPos.x - vw * 0.5f, bottom = _camPos.y - vh * 0.5f;
            for (float x = Mathf.Floor(left / step) * step; x < left + vw; x += step) _bg.Add(x, _camPos.y, w, vh, 0f, _uvWhite, line);
            for (float y = Mathf.Floor(bottom / step) * step; y < bottom + vh; y += step) _bg.Add(_camPos.x, y, vw, w, 0f, _uvWhite, line);
            if (_fever > 0f) _bg.Add(_camPos.x, _camPos.y, vw, vh, 0f, _uvWhite, Pal.Pink.WithA(0.12f * _fever * (0.7f + 0.3f * pulse)));
        }

        void DrawBoard(ThemeDef th)
        {
            var baseUv = Atlas.Uv(th.Block);
            var hiUv = Atlas.Uv(th.Overlay);
            var white = new Color32(255, 255, 255, 255);
            float rimPulse = 0.5f + 0.5f * Mathf.Sin(_time * 8f);
            // the board is a card on the page: a soft shadow, the card, then a pen frame (fever adds a pink halo)
            if (_fever > 0.01f)
            {
                float halo = 9.05f + 0.14f * rimPulse;
                _boardB.Add(BoardCenter.x, BoardCenter.y, halo, halo, 0f, _uvBoard, Pal.Pink.WithA(0.8f * _fever));
            }
            _boardB.Add(BoardCenter.x + 0.08f, BoardCenter.y - 0.12f, 8.62f, 8.62f, 0f, _uvBoard, Pal.Ink.WithA(0.1f));
            _boardB.Add(BoardCenter.x, BoardCenter.y, 8.6f, 8.6f, 0f, _uvBoard, th.Board);
            for (int y = 0; y < Board.N; y++)
                for (int x = 0; x < Board.N; x++)
                {
                    var p = CellPos(x, y);
                    if (th.Sketch) _boardB.Add(p.x, p.y, 0.97f, 0.97f, 0f, _uvCellLine[(x * 5 + y * 3) % 3], white);
                    else _boardB.Add(p.x, p.y, 0.93f, 0.93f, 0f, _uvCell, th.Cell);
                }

            // line preview: rows/columns the dragged piece would complete light up in its color
            Color previewCol = Color.white;
            bool preview = _dragSlot >= 0 && _ghostValid && (_previewRows.Count > 0 || _previewCols.Count > 0);
            if (preview) previewCol = th.Colors[Mathf.Clamp(Tray[_dragSlot].Color - 1, 0, 7)];
            if (preview && th.Sketch)
            {
                // a highlighter pass over the lines that will clear
                var wash = previewCol.WithA(0.38f + 0.12f * Mathf.Sin(_time * 10f));
                foreach (int y in _previewRows) { var a = CellPos(0, y); _boardB.Add(BoardCenter.x, a.y, 8.1f, 0.9f, 0f, _uvWhite, wash); }
                foreach (int x in _previewCols) { var a = CellPos(x, 0); _boardB.Add(a.x, BoardCenter.y, 0.9f, 8.1f, 0f, _uvWhite, wash); }
            }
            _boardB.Add(BoardCenter.x, BoardCenter.y, 8.68f, 8.68f, 0f, _uvBoardLine, white);

            bool hammer = ActiveTool == Booster.Hammer || Aiming;
            for (int y = 0; y < Board.N; y++)
                for (int x = 0; x < Board.N; x++)
                {
                    int i = y * Board.N + x;
                    byte c = Board.Color[i];
                    if (c == 0) continue;
                    var p = CellPos(x, y);
                    p.y += _fall[i];
                    Color col = th.Colors[Mathf.Clamp(c - 1, 0, 7)];
                    float s = 1f;
                    float t = _placeT[i];
                    if (t < 0.2f) s = 1f + 0.14f * (1f - t / 0.2f);
                    if (preview && (_previewRows.Contains(y) || _previewCols.Contains(x))) col = Color.Lerp(previewCol, Color.white, 0.15f);
                    if (_overT >= 0f)
                    {
                        float g = Mathf.Clamp01((_overT - (Board.N - 1 - y) * 0.07f) / 0.3f);
                        float lum = col.r * 0.3f + col.g * 0.59f + col.b * 0.11f;
                        col = Color.Lerp(col, new Color(lum * 0.55f, lum * 0.55f, lum * 0.65f), g * 0.85f);
                    }
                    if (hammer) s *= 1f + 0.05f * Mathf.Sin(_time * 14f + i);
                    _blocks.Add(p.x, p.y, s, s, 0f, baseUv, col);
                    _blocks.Add(p.x, p.y, s, s, 0f, th.Sketch ? _uvNoteLine[(x * 7 + y * 3) % 3] : hiUv, white);
                    byte gem = Board.Gem[i];
                    if (gem > 0)
                    {
                        float gs = 0.66f * s * (1f + 0.06f * Mathf.Sin(_time * 5f + i));
                        _blocks.Add(p.x, p.y, gs, gs, 0f, _uvGem[gem], white);
                    }
                }

            if (_dragSlot >= 0 && _ghostValid)
            {
                var piece = Tray[_dragSlot];
                Color gc = th.Colors[Mathf.Clamp(piece.Color - 1, 0, 7)].WithA(0.38f);
                foreach (var c in piece.Shape.Cells)
                {
                    var p = CellPos(_ghostX + c.x, _ghostY + c.y);
                    _blocks.Add(p.x, p.y, 1f, 1f, 0f, baseUv, gc);
                    if (th.Sketch) _blocks.Add(p.x, p.y, 1f, 1f, 0f, hiUv, new Color(1f, 1f, 1f, 0.45f));
                }
                if (th.Sketch) return;
                float pulse = 0.65f + 0.35f * Mathf.Sin(_time * 10f);
                var glow = previewCol.WithA(0.45f * pulse);
                foreach (int y in _previewRows)
                    for (int x = 0; x < Board.N; x++) { var p = CellPos(x, y); _hl.Add(p.x, p.y, 1.3f, 1.3f, 0f, _uvCellGlow, glow); }
                foreach (int x in _previewCols)
                    for (int y = 0; y < Board.N; y++) { var p = CellPos(x, y); _hl.Add(p.x, p.y, 1.3f, 1.3f, 0f, _uvCellGlow, glow); }
            }
        }

        void DrawPiece(QuadBatch batch, Piece piece, Vector2 center, float cell, ThemeDef th, float alpha)
        {
            var s = piece.Shape;
            var baseUv = Atlas.Uv(th.Block);
            var hiUv = Atlas.Uv(th.Overlay);
            Color col = th.Colors[Mathf.Clamp(piece.Color - 1, 0, 7)];
            col.a = alpha;
            var white = new Color(1f, 1f, 1f, alpha);
            for (int k = 0; k < s.Cells.Length; k++)
            {
                var c = s.Cells[k];
                float px = center.x + (c.x - (s.W - 1) * 0.5f) * cell;
                float py = center.y + (c.y - (s.H - 1) * 0.5f) * cell;
                batch.Add(px, py, cell, cell, 0f, baseUv, col);
                batch.Add(px, py, cell, cell, 0f, hiUv, white);
                if (piece.Gems[k] > 0) batch.Add(px, py, cell * 0.66f, cell * 0.66f, 0f, _uvGem[piece.Gems[k]], white);
            }
        }

        void DrawTray(ThemeDef th)
        {
            bool rotating = ActiveTool == Booster.Rotate;
            for (int i = 0; i < 3; i++)
            {
                var p = Tray[i];
                if (p == null || i == _dragSlot || _slotDelay[i] > 0f) continue;
                bool fits = Board.AnyFit(p.Shape);
                float zoom = _slotZoom[i];
                if (rotating) zoom *= 1f + 0.06f * Mathf.Sin(_time * 8f + i);
                float alpha = fits || State != Phase.Playing ? 1f : 0.35f;
                DrawPiece(_blocks, p, _slotPos[i], TrayScale * zoom, th, alpha);
            }
            if (_dragSlot >= 0 && Tray[_dragSlot] != null)
            {
                float cell = Mathf.Lerp(TrayScale, 1f, _dragZoom);
                var shadow = Tray[_dragSlot];
                var s = shadow.Shape;
                foreach (var c in s.Cells)
                {
                    float px = _dragPos.x + 0.1f + (c.x - (s.W - 1) * 0.5f) * cell;
                    float py = _dragPos.y - 0.16f + (c.y - (s.H - 1) * 0.5f) * cell;
                    _drag.Add(px, py, cell * 0.95f, cell * 0.95f, 0f, Atlas.Uv(th.Block), Pal.Ink.WithA(0.16f));
                }
                DrawPiece(_drag, Tray[_dragSlot], _dragPos, cell, th, 1f);
            }
        }
    }
}
