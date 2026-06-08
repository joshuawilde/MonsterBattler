using MonsterBattler.Sim.Events;

namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>
    /// Hadron Engine: on switch-in, sets Electric Terrain for 5 turns.
    /// (The Electric-Terrain SpA boost portion is not modeled here.)
    /// </summary>
    public sealed class HadronEngineEffect : Effect
    {
        public override string EffectId => "hadronengine";
        public override string DisplayName => "Hadron Engine";

        public override void OnSwitchIn(SwitchInEvent ev, Pokemon owner)
        {
            if (owner != ev.Pokemon) return;
            ev.Battle.SetTerrain(Terrain.Electric, 5);
        }
    }
}
