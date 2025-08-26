using System;
using System.Configuration;      
using System.Data.SqlClient;

namespace ConnectFourClient
{
    internal static class ReplayDbInitializer
    {
        /// <summary>
        /// opens a new local DB for the replay sessions
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static void EnsureCreated()  
        {
            var cs = ConfigurationManager.ConnectionStrings["ReplayDb"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(cs))
                throw new InvalidOperationException("Missing connection string 'ReplayDb' in App.config.");

            TryCreateDatabaseIfMissing(cs);

            using (var con = new SqlConnection(cs))
            using (var cmd = con.CreateCommand())
            {
                con.Open();
                cmd.CommandText = @"
                IF OBJECT_ID('dbo.ReplaySessions') IS NULL
                BEGIN
                    CREATE TABLE dbo.ReplaySessions(
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        Identifier INT NOT NULL,
                        ServerGameId INT NULL,
                        StartedAt datetime2 NOT NULL,
                        EndedAt datetime2 NULL,
                        Result nvarchar(20) NULL
                    );
                END

                IF OBJECT_ID('dbo.ReplayMoves') IS NULL
                BEGIN
                    CREATE TABLE dbo.ReplayMoves(
                        Id INT IDENTITY(1,1) PRIMARY KEY,
                        SessionId INT NOT NULL,
                        MoveIndex INT NOT NULL,
                        Col INT NOT NULL,
                        Row INT NOT NULL,
                        Player INT NOT NULL,
                        PlayedAt datetime2 NOT NULL
                    );

                    ALTER TABLE dbo.ReplayMoves
                        ADD CONSTRAINT FK_ReplayMoves_Session
                            FOREIGN KEY (SessionId) REFERENCES dbo.ReplaySessions(Id) ON DELETE CASCADE;

                    CREATE UNIQUE INDEX UX_ReplayMoves_Session_MoveIndex ON dbo.ReplayMoves(SessionId, MoveIndex);
                END

                IF NOT EXISTS (SELECT 1 FROM sys.indexes 
                               WHERE name='UX_ReplayMoves_Session_MoveIndex' 
                                 AND object_id = OBJECT_ID('dbo.ReplayMoves'))
                    CREATE UNIQUE INDEX UX_ReplayMoves_Session_MoveIndex ON dbo.ReplayMoves(SessionId, MoveIndex);
                ";
                cmd.ExecuteNonQuery();
            }
        }

        private static void TryCreateDatabaseIfMissing(string fullConnectionString)
        {
            try
            {
                var sb = new SqlConnectionStringBuilder(fullConnectionString);
                var dbName = sb.InitialCatalog;
                var hasAttach = sb.ContainsKey("AttachDbFilename") &&
                                !string.IsNullOrWhiteSpace(Convert.ToString(sb["AttachDbFilename"]));

                if (hasAttach || string.IsNullOrWhiteSpace(dbName))
                    return;

                var masterSb = new SqlConnectionStringBuilder(fullConnectionString) { InitialCatalog = "master" };
                using (var con = new SqlConnection(masterSb.ToString()))
                using (var cmd = con.CreateCommand())
                {
                    con.Open();
                    cmd.CommandText = "IF DB_ID(@db) IS NULL EXEC('CREATE DATABASE [' + @db + ']')";
                    cmd.Parameters.AddWithValue("@db", dbName);
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {

            }
        }
    }
}
