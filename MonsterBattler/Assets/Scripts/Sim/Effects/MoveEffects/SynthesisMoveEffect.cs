using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.MoveEffects
{
    /// <summary>
    /// Synthesis / Morning Sun / Moonlight: heal varies by weather.
    /// Sun → 2/3 max HP; Rain / Sand / Snow → 1/4 max HP; otherwise → 1/2 max HP.
    /// </summary>
    public sealed class SynthesisMoveEffect : Effect
    {
        public override string EffectId => "synthesismove";

        public override void OnHit(HitEvent ev, Pokemon owner)
        {
            var u = ev.User;
            if (u == null || u.IsFainted) return;
            int max = u.MaxStats[(int)Stat.HP];
            int healNum, healDen;
            switch (ev.Battle.Field.Weather)
            {
                case Weather.Sun: case Weather.HarshSun:
                    healNum = 2; healDen = 3; break;
                case Weather.Rain: case Weather.HeavyRain:
                case Weather.Sandstorm: case Weather.Snow:
                    healNum = 1; healDen = 4; break;
                default:
                    healNum = 1; healDen = 2; break;
            }
            int heal = System.Math.Min(max * healNum / healDen, max - u.CurrentHp);
            if (heal <= 0)
            {
                ev.Battle.Log.Raw($"|-fail|{u.Species?.Name ?? u.Nickname}|heal");
                return;
            }
            u.CurrentHp += heal;
            ev.Battle.Log.Raw($"|-heal|{u.Species?.Name ?? u.Nickname}|{u.CurrentHp}/{max}");
        }
    }
}
