using System.Collections.Generic;
using UnityEngine;

namespace BlockMeow
{
    /// <summary>Drag &amp; drop: pick a tray piece, show the snapped ghost and line preview, drop or return.</summary>
    public sealed partial class GameSession
    {
        int _dragSlot = -1;
        Vector2 _dragPos;
        float _dragZoom;
        bool _ghostValid;
        int _ghostX, _ghostY;
        readonly List<int> _previewRows = new List<int>(), _previewCols = new List<int>();
        readonly bool[] _sim = new bool[64];

        bool InputEnabled => State == Phase.Playing && !Paused && !Busy && !UIRoot.I.HasPopup;

        int SlotAt(Vector2 w)
        {
            for (int i = 0; i < 3; i++)
            {
                var s = SlotPos(i);
                if (Mathf.Abs(w.x - s.x) < SlotSpacing * 0.5f && Mathf.Abs(w.y - s.y) < 1.7f) return i;
            }
            return -1;
        }

        bool CellAt(Vector2 w, out int x, out int y)
        {
            x = Mathf.RoundToInt(w.x - _origin.x);
            y = Mathf.RoundToInt(w.y - _origin.y);
            return Board.In(x, y);
        }

        float Lift(Piece p) => 1.25f + p.Shape.H * 0.5f;

        public void PointerDown(Vector2 screen)
        {
            if (!InputEnabled) return;
            Vector2 w = ScreenToWorld(screen);
            if (Aiming)
            {
                if (CellAt(w, out int px, out int py)) PunchAt(px, py);
                else if (SlotAt(w) >= 0) CancelAim();
                return;
            }
            if (ActiveTool == Booster.Hammer)
            {
                if (CellAt(w, out int hx, out int hy) && Board.Filled(hx, hy)) HammerAt(hx, hy);
                return;
            }
            int slot = SlotAt(w);
            if (ActiveTool == Booster.Rotate)
            {
                if (slot >= 0 && Tray[slot] != null)
                {
                    Tray[slot].Rotate();
                    _slotZoom[slot] = 0.6f;
                    AudioManager.Play(Sfx.Rotate);
                    if (Stuck) CheckStuck();
                }
                return;
            }
            if (slot < 0 || Tray[slot] == null || _slotDelay[slot] > 0f) return;
            _dragSlot = slot;
            _dragZoom = 0f;
            _dragPos = w + Vector2.up * Lift(Tray[slot]);
            UpdateGhost();
            AudioManager.Play(Sfx.Pick, 0.7f);
            if (Mode == GameMode.Tutorial) GameScreen.I?.HideHand();
        }

        public void PointerDrag(Vector2 screen)
        {
            if (_dragSlot < 0) return;
            if (State != Phase.Playing) { CancelDrag(); return; }
            _dragPos = ScreenToWorld(screen) + Vector2.up * Lift(Tray[_dragSlot]);
            UpdateGhost();
        }

        public void PointerUp(Vector2 screen)
        {
            if (_dragSlot < 0) return;
            int slot = _dragSlot;
            if (State == Phase.Playing && _ghostValid)
            {
                _dragSlot = -1;
                _ghostValid = false;
                _previewRows.Clear(); _previewCols.Clear();
                Place(slot, _ghostX, _ghostY);
                return;
            }
            CancelDrag();
            if (Mode == GameMode.Tutorial && State == Phase.Playing) ShowTutorialHint();
        }

        void CancelDrag()
        {
            if (_dragSlot < 0) return;
            int slot = _dragSlot;
            _slotPos[slot] = _dragPos;
            _slotZoom[slot] = Mathf.Lerp(1f, 1f / TrayScale, _dragZoom);
            _dragSlot = -1;
            _ghostValid = false;
            _previewRows.Clear(); _previewCols.Clear();
            AudioManager.Play(Sfx.Invalid, 0.5f);
        }

        void UpdateGhost()
        {
            var s = Tray[_dragSlot].Shape;
            Vector2 cell0 = _dragPos - new Vector2((s.W - 1) * 0.5f, (s.H - 1) * 0.5f) - _origin;
            int bx = Mathf.RoundToInt(cell0.x), by = Mathf.RoundToInt(cell0.y);
            _ghostValid = false;
            float best = 0.8f * 0.8f;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = bx + dx, cy = by + dy;
                    float d = (cell0.x - cx) * (cell0.x - cx) + (cell0.y - cy) * (cell0.y - cy);
                    if (d >= best || !Board.CanPlace(s, cx, cy)) continue;
                    best = d;
                    _ghostX = cx; _ghostY = cy;
                    _ghostValid = true;
                }
            _previewRows.Clear(); _previewCols.Clear();
            if (!_ghostValid) return;
            for (int i = 0; i < 64; i++) _sim[i] = Board.Color[i] != 0;
            foreach (var c in s.Cells) _sim[(_ghostY + c.y) * Board.N + _ghostX + c.x] = true;
            for (int y = 0; y < Board.N; y++)
            {
                bool full = true;
                for (int x = 0; x < Board.N && full; x++) full = _sim[y * Board.N + x];
                if (full) _previewRows.Add(y);
            }
            for (int x = 0; x < Board.N; x++)
            {
                bool full = true;
                for (int y = 0; y < Board.N && full; y++) full = _sim[y * Board.N + x];
                if (full) _previewCols.Add(x);
            }
        }
    }
}
