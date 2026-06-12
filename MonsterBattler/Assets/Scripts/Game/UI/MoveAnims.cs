using System.Collections;
using System.Collections.Generic;
using MonsterBattler.Sim;
using MonsterBattler.Sim.Data;
using UnityEngine;

namespace MonsterBattler.Game.UI
{
    /// <summary>
    /// Move choreographies, ported from Pokémon Showdown's battle-animations-moves.ts (MIT) onto
    /// our <see cref="FxScene"/> primitives. Iconic moves get hand-ported anims; everything else
    /// falls back to a category/type default (Showdown's fastanimattack/special/self model).
    /// Positions are world-space around the attacker/defender mon transforms.
    /// </summary>
    public static class MoveAnims
    {
        // PS pixel offsets → world units (mons are ~2.4 world units tall vs PS ~150px).
        const float Px = 0.013f;

        public delegate void Anim(FxScene fx, Vector3 atk, Vector3 def, Color typeColor);

        /// <summary>Play the move's animation; yields for its duration (~0.4-0.8s).</summary>
        public static IEnumerator Play(FxScene fx, MoveData move, Transform attacker, Transform defender)
        {
            if (fx == null || move == null || attacker == null || defender == null) yield break;
            Vector3 atk = attacker.position + Vector3.up * 0.8f;
            Vector3 def = defender.position + Vector3.up * 0.8f;
            var color = TypeStyle.BgColor(move.Type);

            AudioManager.PlayMoveSound(move.Id, move.Type.ToString().ToLowerInvariant());

            // Extracted Showdown data first (full fidelity incl. mon dances/hops); hand-ported
            // registry and category defaults only as fallbacks.
            var atkView = attacker.GetComponent<MonsterView>();
            var defView = defender.GetComponent<MonsterView>();
            if (PsAnims.TryPlay(fx, move.Id, atk, def, atkView, defView)) { }
            else if (Registry.TryGetValue(move.Id, out var anim)) anim(fx, atk, def, color);
            else if (move.Category == MoveCategory.Status) DefaultStatus(fx, atk, def, move, color);
            else if (move.Contact) DefaultContact(fx, atk, def, color);
            else DefaultRanged(fx, atk, def, color);

            yield return fx.WaitDone();
        }

        /// <summary>Item knocked off / consumed: the item pops up off the mon and arcs away
        /// behind it, shrinking and fading. towardPlayer flips the arc direction.</summary>
        public static void ItemFlies(FxScene fx, Vector3 monPos, bool flyLeft)
        {
            if (fx == null) return;
            Vector3 from = monPos + Vector3.up * 0.9f;
            float dirX = flyLeft ? -1f : 1f;
            Vector3 apex = from + new Vector3(dirX * 0.5f, 0.7f, 0f);
            Vector3 land = from + new Vector3(dirX * 1.4f, -0.5f, 0f);
            // Two linear segments approximate the arc: up-and-out, then out-and-down.
            fx.ShowEffect("item",
                FxScene.State.At(from).Scale(0.55f).Alpha(1f),
                FxScene.State.At(apex).Scale(0.5f).Alpha(1f).Time(240f), FxScene.Fade.Linear);
            fx.ShowEffect("item",
                FxScene.State.At(apex).Scale(0.5f).Alpha(1f).Time(240f),
                FxScene.State.At(land).Scale(0.35f).Alpha(0f).Time(620f), FxScene.Fade.Decel);
        }

        // ---- defaults (cover all moves without a custom entry) -------------------------------

        // Contact: type-colored orb flies in fast + impact burst on the target.
        static void DefaultContact(FxScene fx, Vector3 atk, Vector3 def, Color c)
        {
            fx.ShowEffect("orb",
                FxScene.State.At(atk).Scale(0.5f).Tint(c).Alpha(0.8f),
                FxScene.State.At(def).Scale(0.7f).Tint(c).Alpha(0.9f).Time(260f), FxScene.Fade.Linear);
            fx.ShowEffect("impact",
                FxScene.State.At(def).Scale(0.4f).Alpha(0f).Time(260f),
                FxScene.State.At(def).Scale(1.2f).Alpha(1f).Time(480f));
        }

        // Ranged/special: orb charges at the user then streaks to the target with a burst.
        static void DefaultRanged(FxScene fx, Vector3 atk, Vector3 def, Color c)
        {
            fx.ShowEffect("orb",
                FxScene.State.At(atk).Scale(0.2f).Tint(c).Alpha(0.4f),
                FxScene.State.At(atk).Scale(0.7f).Tint(c).Alpha(0.95f).Time(220f), FxScene.Fade.Linear);
            fx.ShowEffect("orb",
                FxScene.State.At(atk).Scale(0.6f).Tint(c).Alpha(0.95f).Time(220f),
                FxScene.State.At(def).Scale(0.55f).Tint(c).Alpha(0.9f).Time(440f), FxScene.Fade.Linear);
            fx.ShowEffect("ring",
                FxScene.State.At(def).Scale(0.2f).Tint(c).Alpha(0.9f).Time(440f),
                FxScene.State.At(def).Scale(1.3f).Tint(c).Alpha(0f).Time(700f), FxScene.Fade.Linear);
        }

        // Status: self-boosts shimmer on the user; foe-status drifts a wisp onto the target.
        static void DefaultStatus(FxScene fx, Vector3 atk, Vector3 def, MoveData move, Color c)
        {
            bool self = move.Target == MoveTarget.Self;
            Vector3 on = self ? atk : def;
            fx.ShowEffect("ring",
                FxScene.State.At(on + Vector3.down * 0.3f).Scale(1.1f).Tint(c).Alpha(0f),
                FxScene.State.At(on + Vector3.up * 0.5f).Scale(0.5f).Tint(c).Alpha(0.9f).Time(450f));
            fx.ShowEffect("orb",
                FxScene.State.At(on + Vector3.down * 0.5f).Scale(0.3f).Tint(c).Alpha(0.7f).Time(120f),
                FxScene.State.At(on + Vector3.up * 0.8f).Scale(0.45f).Tint(c).Alpha(0f).Time(600f), FxScene.Fade.Linear);
        }

        // ---- iconic hand-ported choreographies ------------------------------------------------

        static readonly Color Electric = new(0.97f, 0.82f, 0.18f);

        static readonly Dictionary<string, Anim> Registry = new()
        {
            // Thunderbolt (PS: 3 stretched lightning drops, 200ms apart, dark bg flash)
            ["thunderbolt"] = (fx, atk, def, c) =>
            {
                fx.BackgroundEffect(Color.black, 600f, 0.2f);
                for (int i = 0; i < 3; i++)
                {
                    float dx = i == 0 ? 0f : (i == 1 ? -15f : 15f) * Px;
                    fx.ShowEffect("lightning",
                        FxScene.State.At(def + new Vector3(dx, 150f * Px, 0)).YScale(0f).XScale(2f).Time(i * 200f),
                        FxScene.State.At(def + new Vector3(dx, 50f * Px, 0)).YScale(1f).XScale(1.5f).Alpha(0.8f).Time(i * 200f + 200f));
                }
            },
            ["thunder"] = (fx, atk, def, c) =>
            {
                fx.BackgroundEffect(Color.black, 700f, 0.35f);
                for (int i = 0; i < 2; i++)
                    fx.ShowEffect("lightning",
                        FxScene.State.At(def + Vector3.up * (220f * Px)).YScale(0f).XScale(2.5f).Time(i * 250f),
                        FxScene.State.At(def).YScale(1.4f).XScale(2f).Alpha(0.9f).Time(i * 250f + 250f));
            },

            // Flamethrower: stream of fire orbs attacker → defender.
            ["flamethrower"] = (fx, atk, def, c) =>
            {
                for (int i = 0; i < 4; i++)
                    fx.ShowEffect("orb",
                        FxScene.State.At(atk).Scale(0.35f).Tint(new Color(1f, 0.45f, 0.15f)).Alpha(0.9f).Time(i * 90f),
                        FxScene.State.At(def).Scale(0.55f).Tint(new Color(1f, 0.7f, 0.2f)).Alpha(0.85f).Time(i * 90f + 280f), FxScene.Fade.Linear);
                fx.ShowEffect("impact",
                    FxScene.State.At(def).Scale(0.3f).Alpha(0f).Time(380f).Tint(new Color(1f, 0.6f, 0.2f)),
                    FxScene.State.At(def).Scale(1.1f).Alpha(0.9f).Time(640f).Tint(new Color(1f, 0.8f, 0.3f)));
            },

            // Ice Beam: icicle volley + freezing ring.
            ["icebeam"] = (fx, atk, def, c) =>
            {
                for (int i = 0; i < 3; i++)
                    fx.ShowEffect("icicle",
                        FxScene.State.At(atk).Scale(0.35f).Alpha(0.9f).Time(i * 110f),
                        FxScene.State.At(def).Scale(0.45f).Alpha(0.9f).Time(i * 110f + 260f), FxScene.Fade.Linear);
                fx.ShowEffect("ring",
                    FxScene.State.At(def).Scale(0.2f).Tint(new Color(0.6f, 0.9f, 1f)).Alpha(0.9f).Time(380f),
                    FxScene.State.At(def).Scale(1.4f).Tint(new Color(0.8f, 0.97f, 1f)).Alpha(0f).Time(700f), FxScene.Fade.Linear);
            },

            // Surf / Hydro Pump: big water wall washes over the target.
            ["surf"] = (fx, atk, def, c) => Wave(fx, def, 1.6f),
            ["hydropump"] = (fx, atk, def, c) => Wave(fx, def, 1.2f),

            // Shadow Ball: dark orb charges then lobs in.
            ["shadowball"] = (fx, atk, def, c) =>
            {
                fx.BackgroundEffect(new Color(0.1f, 0.05f, 0.2f), 500f, 0.25f);
                fx.ShowEffect("orb",
                    FxScene.State.At(atk).Scale(0.2f).Tint(new Color(0.4f, 0.2f, 0.6f)).Alpha(0.5f),
                    FxScene.State.At(atk).Scale(0.8f).Tint(new Color(0.3f, 0.1f, 0.5f)).Alpha(1f).Time(300f), FxScene.Fade.Linear);
                fx.ShowEffect("orb",
                    FxScene.State.At(atk).Scale(0.8f).Tint(new Color(0.3f, 0.1f, 0.5f)).Time(300f),
                    FxScene.State.At(def).Scale(0.7f).Tint(new Color(0.25f, 0.05f, 0.45f)).Time(520f), FxScene.Fade.Linear);
                fx.ShowEffect("impact",
                    FxScene.State.At(def).Scale(0.4f).Alpha(0f).Time(520f).Tint(new Color(0.6f, 0.3f, 0.9f)),
                    FxScene.State.At(def).Scale(1.3f).Alpha(1f).Time(760f).Tint(new Color(0.5f, 0.2f, 0.8f)));
            },

            // Close Combat: rapid fist flurry.
            ["closecombat"] = (fx, atk, def, c) =>
            {
                for (int i = 0; i < 4; i++)
                {
                    var off = new Vector3((i % 2 == 0 ? -20f : 20f) * Px, (i < 2 ? 20f : -15f) * Px, 0);
                    fx.ShowEffect("fist",
                        FxScene.State.At(def + off * 3f).Scale(0.5f).Alpha(0f).Time(i * 120f),
                        FxScene.State.At(def + off).Scale(0.55f).Alpha(1f).Time(i * 120f + 120f));
                }
                fx.ShowEffect("impact",
                    FxScene.State.At(def).Scale(0.5f).Alpha(0f).Time(480f),
                    FxScene.State.At(def).Scale(1.4f).Alpha(1f).Time(720f));
            },

            // Earthquake: screen-wide shake ring + rocks.
            ["earthquake"] = (fx, atk, def, c) =>
            {
                fx.BackgroundEffect(new Color(0.5f, 0.4f, 0.2f), 700f, 0.3f);
                var ground = new Vector3(def.x, def.y - 60f * Px, def.z);
                fx.ShowEffect("ring",
                    FxScene.State.At(ground).Scale(0.3f).Tint(new Color(0.8f, 0.65f, 0.3f)).Alpha(0.9f),
                    FxScene.State.At(ground).Scale(2.4f).XScale(1.6f).Alpha(0f).Time(600f), FxScene.Fade.Linear);
                for (int i = 0; i < 3; i++)
                    fx.ShowEffect("rock",
                        FxScene.State.At(ground + new Vector3((i - 1) * 40f * Px, 0, 0)).Scale(0.3f).Alpha(0.9f).Time(i * 90f),
                        FxScene.State.At(ground + new Vector3((i - 1) * 50f * Px, 90f * Px, 0)).Scale(0.4f).Alpha(0f).Time(i * 90f + 420f), FxScene.Fade.Linear);
            },

            // Swords Dance: slashes orbit the user.
            ["swordsdance"] = (fx, atk, def, c) =>
            {
                for (int i = 0; i < 3; i++)
                    fx.ShowEffect("slash",
                        FxScene.State.At(atk + new Vector3(-40f * Px, (20f + i * 15f) * Px, 0)).Scale(0.4f).Alpha(0f).Time(i * 140f),
                        FxScene.State.At(atk + new Vector3(40f * Px, (35f + i * 15f) * Px, 0)).Scale(0.55f).Alpha(0.95f).Time(i * 140f + 240f));
            },
        };

        static void Wave(FxScene fx, Vector3 def, float size)
        {
            var blue = new Color(0.35f, 0.55f, 0.95f);
            fx.ShowEffect("orb",
                FxScene.State.At(def + new Vector3(-180f * Px, -40f * Px, 0)).Scale(size * 0.6f).XScale(1.8f).Tint(blue).Alpha(0.75f),
                FxScene.State.At(def + new Vector3(40f * Px, 0, 0)).Scale(size).XScale(2.2f).Tint(blue).Alpha(0.85f).Time(380f), FxScene.Fade.Linear);
            fx.ShowEffect("orb",
                FxScene.State.At(def + new Vector3(-140f * Px, -20f * Px, 0)).Scale(size * 0.45f).XScale(1.5f).Tint(new Color(0.6f, 0.8f, 1f)).Alpha(0.7f).Time(120f),
                FxScene.State.At(def + new Vector3(60f * Px, 10f * Px, 0)).Scale(size * 0.8f).XScale(1.8f).Tint(new Color(0.6f, 0.8f, 1f)).Alpha(0f).Time(520f), FxScene.Fade.Linear);
        }
    }
}
