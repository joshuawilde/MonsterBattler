using System.Collections;
using MonsterBattler.Sim;

namespace MonsterBattler.Game.Net
{
    /// <summary>
    /// Online battle transport. BattleView runs a deterministic mirror of the server's sim
    /// (same seed, same canonical side order); this interface only exchanges the inputs.
    /// Local bot battles don't use a session at all (BattleView._session == null).
    /// </summary>
    public interface IBattleSession
    {
        /// <summary>Sim side index of the local player (0 or 1), assigned by the server.</summary>
        int MySide { get; }

        /// <summary>True once the opponent disconnected or the server ended the match early.</summary>
        bool Aborted { get; }

        /// <summary>Replacement phase after faints: submit our forced-switch team index
        /// (-1 when our side doesn't need one) and yield until the opponent's arrives.</summary>
        IEnumerator ExchangeReplacements(int mine, System.Action<int> onTheirs);

        /// <summary>Turn phase: submit our choice, yield until the opponent's arrives.</summary>
        IEnumerator ExchangeTurn(Choice mine, System.Action<Choice> onTheirs);
    }
}
