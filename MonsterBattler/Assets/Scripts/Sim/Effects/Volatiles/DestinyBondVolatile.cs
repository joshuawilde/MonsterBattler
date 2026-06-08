using MonsterBattler.Sim.Events;
namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>Destiny Bond: if the user faints, the Pokemon that KO'd it faints too.</summary>
    public sealed class DestinyBondVolatile : Effect
    {
        public override string EffectId => "destinybond";
        public override string DisplayName => "Destiny Bond";
        public override void OnFaint(FaintEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            var killer = ev.Source;
            if (killer == null || killer.IsFainted) return;
            ev.Battle.Log.Raw($"|-activate|{owner.Species?.Name ?? owner.Nickname}|move: Destiny Bond");
            killer.CurrentHp = 0;
            ev.Battle.Log.Faint(killer);
            killer.FaintLogged = true;
        }
    }
}
