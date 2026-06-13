using System.Collections.Generic;
using MonsterBattler.Sim;

namespace MonsterBattler.BattleServer
{
    // Wire DTOs (System.Text.Json). Mirrors the Unity client's NetTeamSpec / Choice so both
    // ends run identical deterministic sims — only inputs cross the wire.

    public sealed class TeamSpec
    {
        public string Uid { get; set; }
        public string Username { get; set; }
        public int Elo { get; set; }
        public string[] Species { get; set; }
        public int[] Levels { get; set; }
        public string[] MovesCsv { get; set; }
    }

    public sealed class ChoiceDto
    {
        public string Kind { get; set; }     // "move" | "switch"
        public string MoveId { get; set; }
        public int SwitchToIndex { get; set; }
        public bool Terastallize { get; set; }
        public int PivotToIndexPlusOne { get; set; }

        public Choice ToChoice() => Kind == "switch"
            ? Choice.SwitchTo(SwitchToIndex)
            : new Choice { Kind = ChoiceKind.Move, MoveId = MoveId, Terastallize = Terastallize, PivotToIndexPlusOne = PivotToIndexPlusOne };

        public static ChoiceDto From(Choice c) => new()
        {
            Kind = c.Kind == ChoiceKind.Switch ? "switch" : "move",
            MoveId = c.MoveId, SwitchToIndex = c.SwitchToIndex,
            Terastallize = c.Terastallize, PivotToIndexPlusOne = c.PivotToIndexPlusOne,
        };
    }

    // client → server
    public sealed class ClientMsg
    {
        public string T { get; set; }             // join | choice | replace | chat
        public string MatchId { get; set; }
        public string Uid { get; set; }
        public TeamSpec Team { get; set; }
        public ChoiceDto Choice { get; set; }
        public int Index { get; set; }            // replacement team index (-1 = none)
        public string Text { get; set; }          // chat
    }

    // server → client
    public sealed class ServerMsg
    {
        public string T { get; set; }             // start | turn | replace | abort | chat | error | waiting
        public int Side { get; set; }             // start: which sim side this client is
        public ulong Seed { get; set; }           // start
        public TeamSpec Team0 { get; set; }       // start
        public TeamSpec Team1 { get; set; }       // start
        public ChoiceDto S0 { get; set; }         // turn
        public ChoiceDto S1 { get; set; }         // turn
        public int P0 { get; set; }               // replace: side-0 pick
        public int P1 { get; set; }               // replace: side-1 pick
        public string From { get; set; }          // chat: sender username
        public string Text { get; set; }          // chat | error
    }

    // The match record the backend registers before clients connect.
    public sealed class MatchRegistration
    {
        public string MatchId { get; set; }
        public string Uid0 { get; set; }
        public string Uid1 { get; set; }
        public bool Bot { get; set; } // solo match: fill the opponent with a bot as soon as the player joins
    }
}
