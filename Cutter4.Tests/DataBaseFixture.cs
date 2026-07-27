using Cutter4.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Cutter4.Tests
{
    public class DatabaseFixture : IAsyncLifetime
    {
        private SqliteConnection connection = null!;
        private string schema = null!;

        public IDbContextFactory<CutterContext> ContextFactory { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            string schemaPath = Path.Combine(GetProjectRoot(), "../db_schema.sql");
            schema = await File.ReadAllTextAsync(schemaPath);
            
            await ExecuteSchema();
            await PragmaEnforceFK();
            
            ContextFactory = new PooledDbContextFactory<CutterContext>(
                new DbContextOptionsBuilder<CutterContext>()
                    .UseSqlite(connection)
                    .EnableSensitiveDataLogging()
                    .Options
            );
        }

        public async Task ResetDatabaseAsync()
        {
            await PragmaIgnoreFK();
            await ExecuteSchema();
            await PragmaEnforceFK(); 
        }

        public void DumpDbAsync(string path)
        {
            using var fileConn = new SqliteConnection($"Data Source={path}");
            fileConn.Open();
            connection.BackupDatabase(fileConn);
        }

        private async Task PragmaEnforceFK()
        {
            await using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = ON;";
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task PragmaIgnoreFK()
        {
            await using SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "PRAGMA foreign_keys = OFF;";
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task ExecuteSchema()
        {
            await using SqliteCommand sqlCmd = connection.CreateCommand();
            sqlCmd.CommandText = schema;
            await sqlCmd.ExecuteNonQueryAsync();
        }

        public async Task DisposeAsync()
        {
            await connection.DisposeAsync();
        }

        private static string GetProjectRoot([CallerFilePath] string thisFile = "")
            => Path.GetDirectoryName(thisFile)!;
    }
}
