using System.Collections.Generic;
using MonsterBattler.Sim;
using UnityEngine;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// Type-colored backdrops for monster thumbnails. Each type uses generated art from
    /// StreamingAssets/ui/typebg_{type}.png when present (falling back to a flat muted plate),
    /// and dual-types get a diagonal split — type1 upper-left, type2 lower-right — with a thin
    /// dark seam. Composited once per (t1,t2) pair and cached.
    /// </summary>
    public static class TypeBgSprites
    {
        const int S = 192;
        const float Mute = 0.58f;        // flat-plate darken so the mon sprite stays the hero
        const float ArtDim = 0.80f;      // generated art is busy — dim it a touch
        static readonly Dictionary<(MonType, MonType), Sprite> _cache = new();
        static readonly Dictionary<MonType, Texture2D> _art = new();

        public static Sprite Get(MonType t1, MonType t2)
        {
            if (t2 == t1) t2 = MonType.None;
            var key = (t1, t2);
            if (_cache.TryGetValue(key, out var cached)) return cached;

            var a1 = Art(t1);
            var a2 = t2 == MonType.None ? a1 : Art(t2);
            var c1 = Muted(TypeStyle.BgColor(t1));
            var c2 = t2 == MonType.None ? c1 : Muted(TypeStyle.BgColor(t2));
            var seam = new Color(0.06f, 0.06f, 0.09f, 1f);

            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                float v = (float)y / (S - 1);
                for (int x = 0; x < S; x++)
                {
                    // Diagonal split: type1 fills the upper-left triangle, type2 the lower-right.
                    // (Texture y is bottom-up, so x - y draws as a "/" on screen.)
                    int d = x - y;
                    float u = (float)x / (S - 1);
                    Color c = d < 0
                        ? (a1 != null ? Dim(a1.GetPixelBilinear(u, v)) : c1)
                        : (a2 != null ? Dim(a2.GetPixelBilinear(u, v)) : c2);
                    if (t2 != MonType.None && Mathf.Abs(d) <= 2) c = seam;
                    px[y * S + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
            _cache[key] = sprite;
            return sprite;
        }

        static readonly Dictionary<MonType, Sprite> _stripCache = new();

        /// <summary>Left-edge accent strip for a single type: the type's art (or flat color) with
        /// alpha fading 1→0 left-to-right, so it dissolves into the row it sits on.</summary>
        public static Sprite GetStrip(MonType t)
        {
            if (_stripCache.TryGetValue(t, out var cached)) return cached;
            var art = Art(t);
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                float v = (float)y / (S - 1);
                for (int x = 0; x < S; x++)
                {
                    float u = (float)x / (S - 1);
                    // Full-brightness art (the row needs the accent to read), lightened a touch.
                    Color c = art != null ? art.GetPixelBilinear(u, v) : TypeStyle.BgColor(t);
                    c = new Color(Mathf.Min(1f, c.r * 1.15f), Mathf.Min(1f, c.g * 1.15f), Mathf.Min(1f, c.b * 1.15f), 1f);
                    c.a = u < 0.35f ? 1f : 1f - (u - 0.35f) / 0.65f; // hold solid, then fade out
                    px[y * S + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
            _stripCache[t] = sprite;
            return sprite;
        }

        static readonly Dictionary<MonType, Sprite> _paleCache = new();

        /// <summary>Battle move-card face: near-full-strength type art with a bottom-heavy dark
        /// scrim baked in (text sits on the scrim; light text + outlines on top), plus a thin baked
        /// frame (dark stroke, top highlight). Falls back to a flat tint when no art exists.</summary>
        public static Sprite GetPale(MonType t)
        {
            if (_paleCache.TryGetValue(t, out var cached)) return cached;
            var art = Art(t);
            var flat = TypeStyle.BgColor(t);
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                float v = (float)y / (S - 1);     // 0 = bottom, 1 = top
                // Scrim: heavy at the bottom (type/PP row), easing off toward the top (name band).
                float scrim = Mathf.Lerp(0.66f, 0.22f, Mathf.SmoothStep(0f, 1f, v));
                for (int x = 0; x < S; x++)
                {
                    float u = (float)x / (S - 1);
                    Color c = art != null ? Color.Lerp(art.GetPixelBilinear(u, v), Color.white, 0.18f) : flat;
                    c = Color.Lerp(c, Color.black, scrim);
                    // Baked frame: 2px dark stroke all round + 1px light top edge.
                    if (x < 2 || x >= S - 2 || y < 2 || y >= S - 2) c = Color.Lerp(c, Color.black, 0.45f);
                    else if (y == S - 3) c = Color.Lerp(c, Color.white, 0.13f);
                    c.a = 1f;
                    px[y * S + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
            _paleCache[t] = sprite;
            return sprite;
        }

        static Texture2D Art(MonType t)
        {
            if (t == MonType.None) return null;
            if (_art.TryGetValue(t, out var cached)) return cached;
            Texture2D tex = null;
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "ui",
                                                 $"typebg_{t.ToString().ToLowerInvariant()}.png");
            if (System.IO.File.Exists(path))
            {
                var loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                if (loaded.LoadImage(System.IO.File.ReadAllBytes(path))) tex = loaded;
            }
            _art[t] = tex;
            return tex;
        }

        static Color Dim(Color c) => new(c.r * ArtDim, c.g * ArtDim, c.b * ArtDim, 1f);
        static Color Muted(Color c) => new(c.r * Mute, c.g * Mute, c.b * Mute, 1f);

        /// <summary>Convenience: set an Image to the backdrop for a species (hides on null species).</summary>
        public static void Apply(UnityEngine.UI.Image img, Sim.Data.SpeciesData sp)
        {
            if (img == null) return;
            if (sp == null) { img.enabled = false; return; }
            img.sprite = Get(sp.Type1, sp.Type2);
            img.enabled = true;
            img.color = Color.white;
        }
    }
}
