namespace MonsterBattler.Sim.Effects.Abilities
{
    /// <summary>Mycelium Might: the owner's status moves ignore the target's ability (logic in UseMove).</summary>
    public sealed class MyceliumMightEffect : Effect
    {
        public override string EffectId => "myceliummight";
        public override string DisplayName => "MyceliumMight";
    }
}
