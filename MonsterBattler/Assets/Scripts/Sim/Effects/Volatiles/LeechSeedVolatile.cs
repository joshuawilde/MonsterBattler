using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Volatiles
{
    /// <summary>
    /// Leech Seed volatile: each end-of-turn, the seeded Pokemon loses 1/8 max HP and the
    /// seeder (stored in <see cref="VolatileSlot.Source"/>) gains that amount, capped at full HP.
    /// </summary>
    public sealed class LeechSeedVolatile : Effect
    {
        public override string EffectId => "leechseed";
        public override string DisplayName => "Leech Seed";

        public override void OnResidual(ResidualEvent ev, Pokemon owner)
        {
            if (owner != ev.Target || owner.IsFainted) return;
            var slot = owner.GetVolatile("leechseed");
            if (slot == null) return;
            var seeder = slot.Source;
            if (seeder == null || seeder.IsFainted) return;

            int dmg = System.Math.Max(1, owner.MaxStats[(int)Stat.HP] / 8);
            ev.Battle.ApplyDamage(owner, dmg);
            ev.Battle.Log.Raw($"|-damage|{Ident(owner)}|{owner.CurrentHp}/{owner.MaxStats[(int)Stat.HP]}|[from] Leech Seed");

            int seederMax = seeder.MaxStats[(int)Stat.HP];
            int heal = System.Math.Min(dmg, seederMax - seeder.CurrentHp);
            if (heal > 0)
            {
                seeder.CurrentHp += heal;
                ev.Battle.Log.Raw($"|-heal|{Ident(seeder)}|{seeder.CurrentHp}/{seederMax}|[from] Leech Seed");
            }
        }

        static string Ident(Pokemon mon) => mon?.Nickname ?? mon?.Species?.Name ?? "?";
    }
}
