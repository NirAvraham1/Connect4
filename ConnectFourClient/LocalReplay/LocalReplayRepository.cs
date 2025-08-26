using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace ConnectFourClient.LocalReplay
{
    public sealed class ReplayMoveDto
    {
        public int MoveIndex { get; set; }
        public int Col { get; set; }
        public int Row { get; set; }
        public int Player { get; set; }
        public DateTime PlayedAt { get; set; }
    }

    public sealed class ReplaySessionDto
    {
        public int Id { get; set; }
        public int Identifier { get; set; }
        public int? ServerGameId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Result { get; set; } = "";
    }

    public sealed class LocalReplayRepository
    {
        private readonly string _cs;

        public LocalReplayRepository()
        {
            _cs = ConfigurationManager.ConnectionStrings["ReplayDb"]?.ConnectionString
                  ?? throw new InvalidOperationException("Missing connection string 'ReplayDb' in App.config.");
        }

        private ReplayDataContext Create() { return new ReplayDataContext(_cs); }

        public int CreateSession(int identifier, int? serverGameId = null)
        {
            using (var dc = Create())
            {
                var session = new ReplaySessionEntity
                {
                    Identifier = identifier,
                    ServerGameId = serverGameId,
                    StartedAt = DateTime.UtcNow
                };
                dc.ReplaySessions.InsertOnSubmit(session);
                dc.SubmitChanges();
                return session.Id;
            }
        }

        public void AddMove(int sessionId, int moveIndex, int col, int row, int player)
        {
            using (var dc = Create())
            {
                var m = new ReplayMoveEntity
                {
                    SessionId = sessionId,
                    MoveIndex = moveIndex,
                    Col = col,
                    Row = row,
                    Player = player,
                    PlayedAt = DateTime.UtcNow
                };
                dc.ReplayMoves.InsertOnSubmit(m);
                dc.SubmitChanges();
            }
        }

        public void EndSession(int sessionId, string result)
        {
            using (var dc = Create())
            {
                dc.ExecuteCommand(
                    "UPDATE [ReplaySessions] SET [Result] = {0}, [EndedAt] = {1} WHERE [Id] = {2}",
                    result ?? "", DateTime.UtcNow, sessionId);
            }
        }


        public IList<ReplaySessionDto> ListSessions(int? identifier = null, int top = 50)
        {
            using (var dc = Create())
            {
                var q = dc.ReplaySessions.AsQueryable();
                if (identifier.HasValue)
                    q = q.Where(s => s.Identifier == identifier.Value);

                return q.OrderByDescending(s => s.StartedAt)
                        .Take(top)
                        .Select(s => new ReplaySessionDto
                        {
                            Id = s.Id,
                            Identifier = s.Identifier,
                            ServerGameId = s.ServerGameId,
                            StartedAt = s.StartedAt,
                            EndedAt = s.EndedAt,
                            Result = s.Result ?? ""
                        })
                        .ToList();
            }
        }

        public ReplaySessionDto GetSession(int sessionId)
        {
            using (var dc = Create())
            {
                return dc.ReplaySessions
                         .Where(s => s.Id == sessionId)
                         .Select(s => new ReplaySessionDto
                         {
                             Id = s.Id,
                             Identifier = s.Identifier,
                             ServerGameId = s.ServerGameId,
                             StartedAt = s.StartedAt,
                             EndedAt = s.EndedAt,
                             Result = s.Result ?? ""
                         })
                         .FirstOrDefault();
            }
        }

        public IList<ReplayMoveDto> LoadSessionMoves(int sessionId)
        {
            using (var dc = Create())
            {
                return dc.ReplayMoves
                         .Where(m => m.SessionId == sessionId)
                         .OrderBy(m => m.MoveIndex)
                         .Select(m => new ReplayMoveDto
                         {
                             MoveIndex = m.MoveIndex,
                             Col = m.Col,
                             Row = m.Row,
                             Player = m.Player,
                             PlayedAt = m.PlayedAt
                         })
                         .ToList();
            }
        }
        
        public int DeleteAllForIdentifier(int identifier)
        {
            using (var dc = Create())
            {
                dc.ExecuteCommand(@"
                    DELETE FROM [ReplayMoves]
                    WHERE [SessionId] IN (
                        SELECT [Id] FROM [ReplaySessions]
                        WHERE [Identifier] = {0}
                    );", identifier);

                int sessionsDeleted = dc.ExecuteCommand(@"
                    DELETE FROM [ReplaySessions]
                    WHERE [Identifier] = {0};", identifier);

                return sessionsDeleted;
            }
        }
        

        public int DeleteByServerGameId(int identifier, int serverGameId)
        {
            using (var dc = Create())
            {
                dc.ExecuteCommand(@"
                    DELETE FROM [ReplayMoves]
                    WHERE [SessionId] IN (
                        SELECT [Id] FROM [ReplaySessions]
                        WHERE [Identifier] = {0} AND [ServerGameId] = {1}
                    );", identifier, serverGameId);

                int sessionsDeleted = dc.ExecuteCommand(@"
                    DELETE FROM [ReplaySessions]
                    WHERE [Identifier] = {0} AND [ServerGameId] = {1};",
                    identifier, serverGameId);

                return sessionsDeleted;
            }
        }
    }
}
