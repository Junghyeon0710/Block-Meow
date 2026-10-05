using UnityEngine;
using UnityEngine.Rendering;

namespace BlockMeow
{
    /// <summary>
    /// Immediate-mode sprite batch. Everything pushed in a frame becomes one dynamic mesh,
    /// so the whole board, every effect and the tray cost a handful of draw calls.
    /// </summary>
    public sealed class QuadBatch
    {
        readonly Mesh _mesh;
        Vector3[] _v;
        Color32[] _c;
        Vector2[] _uv;
        int[] _idx;
        int _count, _cap;
        static readonly Bounds Huge = new Bounds(Vector3.zero, new Vector3(10000f, 10000f, 100f));
        public readonly GameObject Go;

        public QuadBatch(string name, Material mat, int order, int capacity, Transform parent)
        {
            Go = new GameObject(name);
            Go.transform.SetParent(parent, false);
            var mf = Go.AddComponent<MeshFilter>();
            var mr = Go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.sortingOrder = order;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _mesh = new Mesh { name = name };
            _mesh.MarkDynamic();
            mf.sharedMesh = _mesh;
            Grow(capacity);
        }

        void Grow(int cap)
        {
            var v = new Vector3[cap * 4]; var c = new Color32[cap * 4]; var uv = new Vector2[cap * 4];
            if (_v != null)
            {
                System.Array.Copy(_v, v, _count * 4);
                System.Array.Copy(_c, c, _count * 4);
                System.Array.Copy(_uv, uv, _count * 4);
            }
            _v = v; _c = c; _uv = uv;
            _idx = new int[cap * 6];
            for (int i = 0; i < cap; i++)
            {
                int b = i * 4, k = i * 6;
                _idx[k] = b; _idx[k + 1] = b + 1; _idx[k + 2] = b + 2;
                _idx[k + 3] = b; _idx[k + 4] = b + 2; _idx[k + 5] = b + 3;
            }
            _cap = cap;
        }

        public void Clear() => _count = 0;

        public void Add(float x, float y, float w, float h, float rot, in UV uv, Color32 col)
            => Add4(x, y, w, h, rot, uv, col, col, col, col);

        /// <summary>Vertical gradient quad (bottom color, top color).</summary>
        public void AddGradient(float x, float y, float w, float h, in UV uv, Color32 bottom, Color32 top)
            => Add4(x, y, w, h, 0f, uv, bottom, top, top, bottom);

        void Add4(float x, float y, float w, float h, float rot, in UV uv, Color32 c0, Color32 c1, Color32 c2, Color32 c3)
        {
            if (_count >= _cap) Grow(_cap * 2);
            float hw = w * 0.5f, hh = h * 0.5f, cs = 1f, sn = 0f;
            if (rot != 0f) { cs = Mathf.Cos(rot); sn = Mathf.Sin(rot); }
            float ax = hw * cs, ay = hw * sn, bx = -hh * sn, by = hh * cs;
            int i = _count * 4;
            _v[i] = new Vector3(x - ax - bx, y - ay - by, 0f);
            _v[i + 1] = new Vector3(x - ax + bx, y - ay + by, 0f);
            _v[i + 2] = new Vector3(x + ax + bx, y + ay + by, 0f);
            _v[i + 3] = new Vector3(x + ax - bx, y + ay - by, 0f);
            _uv[i] = uv.Min;
            _uv[i + 1] = new Vector2(uv.Min.x, uv.Max.y);
            _uv[i + 2] = uv.Max;
            _uv[i + 3] = new Vector2(uv.Max.x, uv.Min.y);
            _c[i] = c0; _c[i + 1] = c1; _c[i + 2] = c2; _c[i + 3] = c3;
            _count++;
        }

        public void Line(Vector2 a, Vector2 b, float thickness, in UV uv, Color32 col)
        {
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 0.0001f) return;
            Add((a.x + b.x) * 0.5f, (a.y + b.y) * 0.5f, len, thickness, Mathf.Atan2(d.y, d.x), uv, col);
        }

        public void Upload()
        {
            _mesh.Clear(true);
            if (_count == 0) return;
            int n = _count * 4;
            _mesh.SetVertices(_v, 0, n, MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontValidateIndices);
            _mesh.SetColors(_c, 0, n);
            _mesh.SetUVs(0, _uv, 0, n);
            _mesh.SetIndices(_idx, 0, _count * 6, MeshTopology.Triangles, 0, false);
            _mesh.bounds = Huge;
        }
    }
}
