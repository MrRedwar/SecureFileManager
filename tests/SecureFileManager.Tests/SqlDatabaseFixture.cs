using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecureFileManager.Data;

namespace SecureFileManager.Tests;

public sealed class SqlFactAttribute : FactAttribute
{
    public SqlFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("SFM_RUN_SQL_TESTS") != "1")
            Skip = "Для реального локального SQL Express явно задайте SFM_RUN_SQL_TESTS=1.";
    }
}

[CollectionDefinition("SQL Server", DisableParallelization = true)]
public sealed class SqlCollection : ICollectionFixture<SqlDatabaseFixture>;

public sealed class SqlDatabaseFixture : IAsyncLifetime
{
    private readonly string? _existing = Environment.GetEnvironmentVariable("SFM_EXISTING_TEST_DATABASE");
    public string DatabaseName { get; } = Environment.GetEnvironmentVariable("SFM_EXISTING_TEST_DATABASE") ??
        "SecureFileManagerTests_" + Guid.NewGuid().ToString("N");
    public string ConnectionString => new SqlConnectionStringBuilder
    {
        DataSource = @".\SQLEXPRESS", InitialCatalog = DatabaseName, IntegratedSecurity = true,
        Encrypt = SqlConnectionEncryptOption.Mandatory, TrustServerCertificate = true
    }.ConnectionString;

    public async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("SFM_RUN_SQL_TESTS") != "1") return;
        if (Environment.GetEnvironmentVariable("SFM_ENVIRONMENT") != "Development")
            throw new InvalidOperationException("SQL-тесты с локальным доверием сертификату требуют Development.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(DatabaseName, "^SecureFileManagerTests_[a-f0-9]{32}$"))
            throw new InvalidOperationException("Разрешена только специально именованная изолированная тестовая БД.");
        var masterString = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" }.ConnectionString;
        await using var master = new SqlConnection(masterString);
        await master.OpenAsync();
        await using var command = new SqlCommand("SELECT DB_ID(@name)", master);
        command.Parameters.AddWithValue("@name", DatabaseName);
        var exists = await command.ExecuteScalarAsync() is not DBNull;
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);
        if (_existing is not null)
        {
            if (!exists || (await db.Database.GetPendingMigrationsAsync()).Any())
                throw new InvalidOperationException("Существующая тестовая БД должна быть доступна и иметь актуальную схему. Создание и миграция запрещены.");
            return;
        }
        if (exists)
            throw new InvalidOperationException("Тестовая БД уже существует: изменения запрещены.");
        await db.Database.MigrateAsync();
        // No EnsureDeleted/DROP: leave the uniquely named empty test database for inspection.
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task<SqlTestTransaction> BeginAsync()
    {
        var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        return new SqlTestTransaction(connection, transaction);
    }
}

public sealed class SqlTestTransaction(SqlConnection connection, SqlTransaction transaction)
    : IDbContextFactory<AppDbContext>, IAsyncDisposable
{
    public AppDbContext CreateDbContext()
    {
        var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
        context.Database.UseTransaction(transaction);
        return context;
    }

    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateDbContext());
    }

    public async ValueTask DisposeAsync()
    {
        await transaction.RollbackAsync();
        await transaction.DisposeAsync();
        await connection.DisposeAsync();
    }
}
