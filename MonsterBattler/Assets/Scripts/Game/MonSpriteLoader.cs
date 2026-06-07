using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MonsterBattler.Game
{
    /// <summary>
    /// Loads a species' pixel sprite from StreamingAssets/mons/{id}_{front|back}.png at runtime
    /// (point-filtered, bottom-center pivot). Sprites are mapped + copied by tools/build-mon-sprites.py.
    /// Cached so each species/view loads once.
    /// </summary>
    public static class MonSpriteLoader
    {
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        public static Sprite Load(string speciesId, bool back)
        {
            if (string.IsNullOrEmpty(speciesId)) return null;
            string key = speciesId.ToLowerInvariant() + (back ? "_back" : "_front");
            if (_cache.TryGetValue(key, out var cached)) return cached;

            Sprite sprite = null;
            string path = Path.Combine(Application.streamingAssetsPath, "mons", key + ".png");
            if (File.Exists(path))
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                if (tex.LoadImage(File.ReadAllBytes(path)))
                {
                    tex.Apply();
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                           new Vector2(0.5f, 0f), 64f); // pivot bottom-center
                }
            }
            _cache[key] = sprite;
            return sprite;
        }
    }
}
