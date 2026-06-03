using System.Collections.Generic;
using MonsterBattler.Sim.Data;

namespace MonsterBattler.Sim.Log
{
    /// <summary>
    /// Pokemon-Showdown-style battle log: a list of pipe-delimited lines that fully describe
    /// what happened. The Unity layer reads this to drive animations/UI; later it's also our
    /// parity-test diff target against the PS reference engine.
    /// </summary>
    public sealed class BattleLog
    {
        public readonly List<string> Lines = new();

        public void Turn(int n) => Lines.Add($"|turn|{n}");
        public void Move(Pokemon user, MoveData move, Pokemon target) =>
            Lines.Add($"|move|{Ident(user)}|{move.Name}|{Ident(target)}");
        public void Damage(Pokemon mon, int amount) =>
            Lines.Add($"|-damage|{Ident(mon)}|{mon.CurrentHp}/{mon.MaxStats[(int)Stat.HP]}");
        public void Faint(Pokemon mon) => Lines.Add($"|faint|{Ident(mon)}");
        public void Raw(string line) => Lines.Add(line);

        static string Ident(Pokemon mon) => mon?.Nickname ?? mon?.Species?.Name ?? "?";
    }
}
