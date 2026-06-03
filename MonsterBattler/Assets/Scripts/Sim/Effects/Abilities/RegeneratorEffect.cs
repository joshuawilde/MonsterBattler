using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Regenerator: on switching out, heal 1/3 max HP.</summary>
    public sealed class RegeneratorEffect : Effect
    {
        public override string EffectId => "regenerator";
        public override string DisplayName => "Regenerator";

        public override void OnSwitchOut(SwitchOutEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon || owner.IsFainted) return;
            int max = owner.MaxStats[(int)Stat.HP];
            int heal = System.Math.Min(max / 3, max - owner.CurrentHp);
            if (heal <= 0) return;
            owner.CurrentHp += heal;
            ev.Battle.Log.Raw($"|-heal|{owner.Species?.Name ?? owner.Nickname}|{owner.CurrentHp}/{max}|[from] ability: Regenerator");
        }
    }
}
