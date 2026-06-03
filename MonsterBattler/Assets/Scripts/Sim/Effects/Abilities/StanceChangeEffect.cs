using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Stance Change: before an attacking move, Aegislash swaps to Blade form (the high-Atk/SpA
    /// form). King's Shield reverts it to Shield form.
    /// </summary>
    public sealed class StanceChangeEffect : Effect
    {
        public override string EffectId => "stancechange";
        public override string DisplayName => "Stance Change";

        public override void OnBeforeMove(BeforeMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User || ev.Move == null) return;

            if (ev.Move.Id == "kingsshield")
            {
                if (owner.Species?.Id == "aegislashblade")
                    ev.Battle.ChangeForm(owner, "aegislash");
                return;
            }

            // Any other attacking move → swap into the offensive form.
            if (ev.Move.Category == MoveCategory.Status) return;
            if (owner.Species?.Id == "aegislash")
                ev.Battle.ChangeForm(owner, "aegislashblade");
        }
    }
}
