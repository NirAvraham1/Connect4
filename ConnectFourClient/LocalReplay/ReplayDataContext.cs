using System.Configuration;
using System.Data.Linq;

namespace ConnectFourClient.LocalReplay
{
    public sealed class ReplayDataContext : DataContext
    {
        public Table<ReplaySessionEntity> ReplaySessions;
        public Table<ReplayMoveEntity> ReplayMoves;

        public ReplayDataContext(string cs) : base(cs)
        {
            ReplaySessions = GetTable<ReplaySessionEntity>();
            ReplayMoves = GetTable<ReplayMoveEntity>();
        }

        public static ReplayDataContext FromConfig(string name = "ReplayDb") //app config
            => new ReplayDataContext(ConfigurationManager.ConnectionStrings[name].ConnectionString);
    }
}
