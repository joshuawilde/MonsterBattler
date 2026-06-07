using MonsterBattler.Sim;

namespace MonsterBattler.Game.AI
{
    /// <summary>
    /// Deterministic strong-play AI: always takes the highest-valued action from
    /// <see cref="HeuristicEvaluator"/>. This is the skill ceiling that <see cref="EloBattleAI"/>
    /// weakens toward a target Elo.
    /// </summary>
    public sealed class HeuristicAI : IBattleAI
    {
        public Choice ChooseAction(Battle battle, Side ownSide, Side opponentSide)
        {
            var scored = HeuristicEvaluator.Score(battle, ownSide, opponentSide);
            if (scored.Count == 0) return Choice.UseMove("tackle");
            var best = scored[0];
            for (int i = 1; i < scored.Count; i++)
                if (scored[i].Value > best.Value) best = scored[i];
            return best.Choice;
        }
    }
}
