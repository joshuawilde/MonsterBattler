using System.Collections;
using MonsterBattler.Sim;
using UnityEngine;

namespace MonsterBattler.Game.Net
{
    /// <summary>IBattleSession over <see cref="NetBattleManager"/> RPCs.</summary>
    public sealed class NetBattleSession : IBattleSession
    {
        readonly NetBattleManager _mgr;
        public int MySide { get; }
        public bool Aborted { get; private set; }

        int? _theirRepl;
        Choice? _theirChoice;

        public NetBattleSession(NetBattleManager mgr, int mySide)
        {
            _mgr = mgr;
            MySide = mySide;
            _mgr.ReplacementsResolved += (s0, s1) => _theirRepl = MySide == 0 ? s1 : s0;
            _mgr.TurnResolved += (c0, c1) => _theirChoice = MySide == 0 ? c1 : c0;
            _mgr.MatchAborted += () => Aborted = true;
        }

        public IEnumerator ExchangeReplacements(int mine, System.Action<int> onTheirs)
        {
            _theirRepl = null;
            _mgr.SubmitReplacement(mine);
            while (_theirRepl == null && !Aborted) yield return null;
            if (_theirRepl != null) onTheirs(_theirRepl.Value);
        }

        public IEnumerator ExchangeTurn(Choice mine, System.Action<Choice> onTheirs)
        {
            _theirChoice = null;
            _mgr.SubmitChoice(mine);
            while (_theirChoice == null && !Aborted) yield return null;
            if (_theirChoice != null) onTheirs(_theirChoice.Value);
        }
    }
}
