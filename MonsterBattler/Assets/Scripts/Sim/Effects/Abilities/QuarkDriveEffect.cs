using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Quark Drive: on Electric Terrain (or on consuming Booster Energy), boosts the holder's
    /// highest stat — ×1.3, or ×1.5 if that stat is Speed. See <see cref="Volatiles.ParadoxBoostVolatile"/>.
    /// </summary>
    public sealed class QuarkDriveEffect : Effect
    {
        public override string EffectId => "quarkdrive";
        public override string DisplayName => "Quark Drive";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (ev.Battle.Field.Terrain == Terrain.Electric)
            {
                Volatiles.ParadoxBoostVolatile.Activate(owner, ev.Battle, "terrain", DisplayName);
            }
            else if (owner.HasItem("boosterenergy"))
            {
                owner.Item = null;
                ev.Battle.Log.Raw($"|-enditem|{owner.Species?.Name ?? owner.Nickname}|Booster Energy");
                Volatiles.ParadoxBoostVolatile.Activate(owner, ev.Battle, "booster", DisplayName);
            }
        }
    }
}
