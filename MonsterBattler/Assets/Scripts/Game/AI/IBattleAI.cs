using MonsterBattler.Sim;

namespace MonsterBattler.Game.AI
{
    /// <summary>
    /// One side's decision-maker. Returns the <see cref="Choice"/> to submit for the upcoming turn.
    /// Implementations should be deterministic given the battle's PRNG (or their own seeded PRNG)
    /// so battles remain reproducible.
    /// </summary>
    public interface IBattleAI
    {
        Choice ChooseAction(Battle battle, Side ownSide, Side opponentSide);
    }
}
