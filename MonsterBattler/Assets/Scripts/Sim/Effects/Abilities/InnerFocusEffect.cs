using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Inner Focus: owner can't flinch. Implemented by removing any flinch volatile on every BeforeMove.</summary>
    public sealed class InnerFocusEffect : Effect
    {
        public override string EffectId => "innerfocus";
        public override string DisplayName => "Inner Focus";

        public override void OnBeforeMove(BeforeMoveEvent ev, Pokemon owner)
        {
            if (owner != ev.User) return;
            if (owner.Volatiles.ContainsKey("flinch"))
                ev.Battle.RemoveVolatile(owner, "flinch");
        }
    }
}
