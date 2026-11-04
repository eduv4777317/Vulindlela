using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Vulindlela.Clearance.Api.Data;
using Vulindlela.Clearance.Api.Data.Repositories;
using Vulindlela.Clearance.Api.Data.Seedings;
using Vulindlela.Clearance.Api.Queue;
using Vulindlela.Clearance.Api.Services;

namespace Vulindlela.Tests.Clearance
{
    public sealed class ClearanceTestDatabase : IDisposable
    {
        private readonly string _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"clearance-test-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        }.ToString();

        private readonly SqliteConnection _keepAlive;

        public ClearanceTestDatabase()
        {
            _keepAlive = new SqliteConnection(_connectionString);
            _keepAlive.Open();

            using var db = CreateContext();
            db.Database.EnsureCreated();
            new ClearanceSeeder(db).SeedAsync().GetAwaiter().GetResult();
        }

        public ChannelVerificationQueue Queue { get; } = new();

        public ClearanceDBContext CreateContext() =>
            new(new DbContextOptionsBuilder<ClearanceDBContext>().UseSqlite(_connectionString).Options);

        public ClearanceService CreateService(ClearanceDBContext db) =>
            new(new ClearanceRepository(db), Queue, new SimulatedDocumentScanner(TimeSpan.Zero));

        public void Dispose() => _keepAlive.Dispose();
    }
}
