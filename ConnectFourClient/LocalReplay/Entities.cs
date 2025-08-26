using System;
using System.Data.Linq.Mapping;

namespace ConnectFourClient.LocalReplay
{
    [Table(Name = "dbo.ReplaySessions")] //stands for one replay session
    public sealed class ReplaySessionEntity
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int Id { get; set; }

        [Column] public int Identifier { get; set; }
        [Column(CanBeNull = true)] public int? ServerGameId { get; set; }
        [Column] public DateTime StartedAt { get; set; }
        [Column(CanBeNull = true)] public DateTime? EndedAt { get; set; }
        [Column(CanBeNull = true)] public string Result { get; set; }
    }

    [Table(Name = "dbo.ReplayMoves")]//stands for one move in a session
    public sealed class ReplayMoveEntity
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int Id { get; set; }

        [Column] public int SessionId { get; set; }
        [Column] public int MoveIndex { get; set; } // represents the order in the session (move number 0 1 2...  in the session)
        [Column] public int Col { get; set; }
        [Column] public int Row { get; set; }
        [Column] public int Player { get; set; } // 1 - Player 2 - Bot
        [Column] public DateTime PlayedAt { get; set; }
    }
}
