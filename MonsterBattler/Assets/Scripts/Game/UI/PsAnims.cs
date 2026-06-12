using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// Data-driven player for the full Pokémon Showdown move-animation set, extracted to
    /// StreamingAssets/fx/anims.json by tools/ps-anim-extract (one entry per move: showEffect /
    /// backgroundEffect steps with every scalar solved as ca*attacker + cd*defender + k in PS
    /// pixel space). Playback maps PS space onto the world battle plane: the horizontal axis runs
    /// attacker→defender in world XZ, y is world up, PS px → world via ×0.013. PS sprite names
    /// resolve to our small fx library with a per-name tint.
    /// </summary>
    public static class PsAnims
    {
        const float Px = 0.013f;

        static JObject _root;
        static bool _loadFailed;
        static readonly Dictionary<string, JToken> _cache = new();

        /// <summary>Play moveId's extracted animation. False if the table or move is missing.</summary>
        public static bool TryPlay(FxScene fx, string moveId, Vector3 atk, Vector3 def)
        {
            var steps = Lookup(moveId) as JArray;
            if (steps == null || fx == null) return false;

            // PS-space frame: attacker at u=0, defender at u=L (same px scale as PS's ~350px gap).
            Vector3 flat = def - atk; flat.y = 0f;
            float worldDist = flat.magnitude;
            if (worldDist < 0.01f) return false;
            Vector3 dirU = flat / worldDist;
            float L = worldDist / Px;
            float ay = atk.y / Px, dy = def.y / Px;

            bool played = false;
            foreach (var step in steps)
            {
                switch ((string)step["type"])
                {
                    case "effect":
                        played |= PlayEffect(fx, step, atk, dirU, L, ay, dy);
                        break;
                    case "bg":
                        fx.BackgroundEffect(ParseColor((string)step["color"]),
                            (float?)step["duration"] ?? 600f, (float?)step["opacity"] ?? 0.3f,
                            (float?)step["delay"] ?? 0f);
                        played = true;
                        break;
                    // monAnim/monDelay lunges are covered by MonsterView's own beat anims.
                }
            }
            return played;
        }

        static bool PlayEffect(FxScene fx, JToken step, Vector3 atk, Vector3 dirU, float L, float ay, float dy)
        {
            string psName = step["sprite"]?.Type == JTokenType.String ? (string)step["sprite"] : null;
            if (psName == null || !SpriteMap.TryGetValue(psName, out var m)) return false; // mon-sprite copies / inline sprites: skip

            var from = ReadState(step["from"], atk, dirU, L, ay, dy, m.tint);
            var to = ReadState(step["to"], atk, dirU, L, ay, dy, m.tint);

            string easeStr = (string)step["ease"] ?? "linear";
            float arc = easeStr switch
            {
                "ballistic" or "ballisticUp" or "ballistic2Back" or "ballistic2back" => 0.7f,
                "ballistic2" or "ballistic2Under" => -0.45f,
                _ => 0f,
            };
            var ease = easeStr switch
            {
                "swing" => FxScene.Ease.Swing,
                "accel" => FxScene.Ease.Accel,
                "decel" => FxScene.Ease.Decel,
                "linear" => FxScene.Ease.Linear,
                _ => FxScene.Ease.Decel, // ballistic family: decelerating flight + parabolic arc
            };
            var fade = (string)step["fade"] switch
            {
                "explode" => FxScene.Fade.Explode,
                "gone" => FxScene.Fade.Gone,
                "fade" => FxScene.Fade.Fade,
                _ => FxScene.Fade.Linear,
            };
            fx.ShowEffect(m.sprite, from, to, ease, fade, arc);
            return true;
        }

        static FxScene.State ReadState(JToken s, Vector3 atk, Vector3 dirU, float L, float ay, float dy, Color tint)
        {
            float u = Eval(s?["x"], 0f, L);
            float y = Eval(s?["y"], ay, dy);
            Vector3 pos = atk + dirU * (u * Px);
            pos.y = y * Px;

            var st = FxScene.State.At(pos).Tint(tint);
            st.scale = Eval(s?["scale"], 0f, 0f, 1f);
            st.xscale = s?["xscale"] != null ? Eval(s["xscale"], 0f, 0f, 1f) / Mathf.Max(0.01f, st.scale) : 1f;
            st.yscale = s?["yscale"] != null ? Eval(s["yscale"], 0f, 0f, 1f) / Mathf.Max(0.01f, st.scale) : 1f;
            st.opacity = Eval(s?["opacity"], 0f, 0f, 1f);
            st.timeMs = Eval(s?["time"], 0f, 0f, 0f);
            return st;
        }

        // Scalar coefficient object {ca, cd, k} (missing keys = 0) → ca*A + cd*D + k.
        static float Eval(JToken c, float a, float d, float fallback = 0f)
        {
            if (c == null) return fallback;
            return ((float?)c["ca"] ?? 0f) * a + ((float?)c["cd"] ?? 0f) * d + ((float?)c["k"] ?? 0f);
        }

        static Color ParseColor(string hex)
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex, out var c)) return c;
            return Color.black;
        }

        static JToken Lookup(string moveId)
        {
            if (_cache.TryGetValue(moveId, out var hit)) return hit;
            if (_root == null && !_loadFailed)
            {
                try
                {
                    var path = Path.Combine(Application.streamingAssetsPath, "fx", "anims.json");
                    _root = JObject.Parse(File.ReadAllText(path));
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PsAnims] anims.json unavailable: {e.Message}");
                    _loadFailed = true;
                }
            }
            JToken steps = null;
            var entry = _root?[moveId] as JObject;
            if (entry != null && entry["error"] == null) steps = entry["steps"];
            _cache[moveId] = steps;
            return steps;
        }

        // PS fx sprite name → (our fx library sprite, tint). PS uses ~30 colored-orb variants;
        // one white orb asset + tint covers them. Unmapped names (mon-sprite copies) are skipped.
        static readonly Dictionary<string, (string sprite, Color tint)> SpriteMap = new()
        {
            ["wisp"] = ("orb", Color.white),
            ["shine"] = ("orb", new Color(1f, 1f, 0.72f)),
            ["poisonwisp"] = ("orb", new Color(0.65f, 0.3f, 0.85f)),
            ["blackwisp"] = ("orb", new Color(0.22f, 0.2f, 0.27f)),
            ["waterwisp"] = ("orb", new Color(0.35f, 0.62f, 1f)),
            ["mudwisp"] = ("orb", new Color(0.62f, 0.45f, 0.25f)),
            ["fireball"] = ("orb", new Color(1f, 0.45f, 0.15f)),
            ["flareball"] = ("orb", new Color(1f, 0.58f, 0.2f)),
            ["bluefireball"] = ("orb", new Color(0.4f, 0.6f, 1f)),
            ["electroball"] = ("orb", new Color(1f, 0.92f, 0.3f)),
            ["iceball"] = ("orb", new Color(0.65f, 0.9f, 1f)),
            ["energyball"] = ("orb", new Color(0.45f, 0.9f, 0.35f)),
            ["shadowball"] = ("orb", new Color(0.45f, 0.25f, 0.6f)),
            ["mistball"] = ("orb", new Color(1f, 0.7f, 0.85f)),
            ["heart"] = ("orb", new Color(1f, 0.5f, 0.7f)),
            ["pokeball"] = ("orb", new Color(1f, 0.35f, 0.3f)),
            ["moon"] = ("orb", new Color(0.92f, 0.92f, 1f)),
            ["rainbow"] = ("ring", Color.white),
            ["gear"] = ("ring", new Color(0.8f, 0.8f, 0.85f)),
            ["greenmetal1"] = ("ring", new Color(0.5f, 1f, 0.6f)),
            ["greenmetal2"] = ("ring", new Color(0.5f, 1f, 0.6f)),
            ["lightning"] = ("lightning", Color.white),
            ["fist"] = ("fist", Color.white),
            ["fist1"] = ("fist", Color.white),
            ["foot"] = ("fist", Color.white),
            ["bone"] = ("fist", new Color(0.95f, 0.95f, 0.85f)),
            ["impact"] = ("impact", Color.white),
            ["angry"] = ("impact", new Color(1f, 0.35f, 0.35f)),
            ["stare"] = ("impact", new Color(1f, 0.25f, 0.25f)),
            ["zsymbol"] = ("impact", new Color(1f, 0.9f, 0.3f)),
            ["leftslash"] = ("slash", Color.white),
            ["rightslash"] = ("slash", Color.white),
            ["leftchop"] = ("slash", Color.white),
            ["rightchop"] = ("slash", Color.white),
            ["leftclaw"] = ("slash", Color.white),
            ["rightclaw"] = ("slash", Color.white),
            ["topbite"] = ("slash", new Color(0.9f, 0.9f, 1f)),
            ["bottombite"] = ("slash", new Color(0.9f, 0.9f, 1f)),
            ["sword"] = ("slash", Color.white),
            ["pointer"] = ("slash", new Color(1f, 1f, 0.8f)),
            ["icicle"] = ("icicle", Color.white),
            ["pinkicicle"] = ("icicle", new Color(1f, 0.7f, 0.85f)),
            ["leaf1"] = ("leaf", Color.white),
            ["leaf2"] = ("leaf", Color.white),
            ["petal"] = ("leaf", new Color(1f, 0.6f, 0.8f)),
            ["feather"] = ("leaf", new Color(1f, 1f, 0.95f)),
            ["rock1"] = ("rock", Color.white),
            ["rock2"] = ("rock", Color.white),
            ["rock3"] = ("rock", Color.white),
            ["rocks"] = ("rock", Color.white),
            ["shell"] = ("rock", new Color(0.9f, 0.75f, 0.5f)),
            ["web"] = ("web", Color.white),
            ["caltrop"] = ("spike", Color.white),
            ["poisoncaltrop"] = ("spike", new Color(0.72f, 0.32f, 0.86f)),
        };
    }
}
