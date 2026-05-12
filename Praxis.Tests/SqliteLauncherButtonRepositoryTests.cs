using Praxis.Core.Logic;
using Praxis.Core.Models;
using Praxis.Data.Entities;
using Praxis.Data.Repositories;
using SQLite;

namespace Praxis.Tests;

public class SqliteLauncherButtonRepositoryTests
{
    [Theory]
    [InlineData("praxis.db3")]
    [InlineData("praxis.db")]
    public async Task InitializeAsync_MigratesV4DatabaseAndReadsExistingButtons(string databaseFileName)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"praxis-db-{Guid.NewGuid():N}");
        var databasePath = Path.Combine(directory, databaseFileName);
        try
        {
            Directory.CreateDirectory(directory);
            await CreateLegacyV4DatabaseAsync(databasePath);

            await using var repository = new SqliteLauncherButtonRepository(databasePath);
            await repository.InitializeAsync();
            var buttons = await repository.GetButtonsAsync();

            var button = Assert.Single(buttons);
            Assert.Equal("docs", button.Command);
            Assert.Equal("Docs", button.ButtonText);
            Assert.Equal("open", button.Tool);
            Assert.Equal("README.md", button.Arguments);
            Assert.Equal("documentation", button.Note);
            Assert.Equal(LauncherButtonColorKey.Default, button.ColorKey);
            Assert.Equal(string.Empty, button.ToolTip);
            Assert.Null(button.LastExecutedAtUtc);
            Assert.Equal(0, button.SortOrder);

            var connection = new SQLiteAsyncConnection(databasePath);
            try
            {
                var version = await connection.ExecuteScalarAsync<int>("PRAGMA user_version;");
                Assert.Equal(DatabaseSchemaVersionPolicy.CurrentVersion, version);
                Assert.True(await ColumnExistsAsync(connection, nameof(LauncherButtonEntity), nameof(LauncherButtonEntity.ColorKey)));
                Assert.True(await ColumnExistsAsync(connection, nameof(LauncherButtonEntity), nameof(LauncherButtonEntity.ToolTip)));
                Assert.True(await ColumnExistsAsync(connection, nameof(LauncherButtonEntity), nameof(LauncherButtonEntity.LastExecutedAtUtc)));
                Assert.True(await ColumnExistsAsync(connection, nameof(LauncherButtonEntity), nameof(LauncherButtonEntity.SortOrder)));
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UpsertButtonAsync_PersistsV2Fields()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"praxis-{Guid.NewGuid():N}.db3");
        try
        {
            await using var repository = new SqliteLauncherButtonRepository(databasePath);
            var executedAt = new DateTime(2026, 5, 12, 10, 20, 30, DateTimeKind.Utc);

            await repository.InitializeAsync();
            await repository.UpsertButtonAsync(new LauncherButtonRecord
            {
                Command = "build",
                ButtonText = "Build",
                Tool = "dotnet",
                Arguments = "build",
                ColorKey = LauncherButtonColorKey.Green,
                ToolTip = "Build solution",
                LastExecutedAtUtc = executedAt,
                SortOrder = 7,
            });

            var button = Assert.Single(await repository.GetButtonsAsync());
            Assert.Equal(LauncherButtonColorKey.Green, button.ColorKey);
            Assert.Equal("Build solution", button.ToolTip);
            Assert.Equal(executedAt, button.LastExecutedAtUtc);
            Assert.Equal(7, button.SortOrder);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task GetByCommandAsync_MatchesCommandCaseInsensitively()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"praxis-{Guid.NewGuid():N}.db3");
        try
        {
            await using var repository = new SqliteLauncherButtonRepository(databasePath);
            await repository.InitializeAsync();
            await repository.UpsertButtonAsync(new LauncherButtonRecord
            {
                Command = "Docs",
                ButtonText = "Docs",
            });

            var button = await repository.GetByCommandAsync("docs");

            Assert.NotNull(button);
            Assert.Equal("Docs", button.Command);
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task DockOrderAndLaunchLogs_ArePersisted()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"praxis-{Guid.NewGuid():N}.db3");
        try
        {
            await using var repository = new SqliteLauncherButtonRepository(databasePath);
            await repository.InitializeAsync();
            var first = new LauncherButtonRecord { Command = "first", ButtonText = "First" };
            var second = new LauncherButtonRecord { Command = "second", ButtonText = "Second" };
            await repository.UpsertButtonAsync(first);
            await repository.UpsertButtonAsync(second);
            await repository.SetDockButtonIdsAsync([second.Id, first.Id, second.Id, Guid.Empty]);
            await repository.AddLogAsync(new LaunchLogEntry
            {
                ButtonId = second.Id,
                Source = "command",
                Tool = "open",
                Arguments = "README.md",
                Succeeded = true,
                Message = "Executed.",
                TimestampUtc = DateTime.UtcNow,
            });

            Assert.Equal([second.Id, first.Id], await repository.GetDockButtonIdsAsync());

            var connection = new SQLiteAsyncConnection(databasePath);
            try
            {
                var logCount = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(1) FROM {nameof(LaunchLogEntity)};");
                Assert.Equal(1, logCount);
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task DeleteButtonAsync_RemovesButtonAndDockReference()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"praxis-{Guid.NewGuid():N}.db3");
        try
        {
            await using var repository = new SqliteLauncherButtonRepository(databasePath);
            var button = new LauncherButtonRecord { Command = "docs", ButtonText = "Docs" };
            await repository.InitializeAsync();
            await repository.UpsertButtonAsync(button);
            await repository.SetDockButtonIdsAsync([button.Id]);

            await repository.DeleteButtonAsync(button.Id);

            Assert.Null(await repository.GetByIdAsync(button.Id, forceReload: true));
            Assert.Empty(await repository.GetDockButtonIdsAsync());
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    private static async Task CreateLegacyV4DatabaseAsync(string databasePath)
    {
        var connection = new SQLiteAsyncConnection(databasePath);
        await connection.ExecuteAsync(
            """
            CREATE TABLE LauncherButtonEntity (
                Id varchar PRIMARY KEY NOT NULL,
                Command varchar,
                ButtonText varchar,
                Tool varchar,
                Arguments varchar,
                ClipText varchar,
                Note varchar,
                X float,
                Y float,
                Width float,
                Height float,
                UseInvertedThemeColors integer NOT NULL DEFAULT 0,
                CreatedAtUtc datetime,
                UpdatedAtUtc datetime
            );
            """);
        await connection.ExecuteAsync(
            """
            CREATE TABLE LaunchLogEntity (
                Id varchar PRIMARY KEY NOT NULL,
                ButtonId varchar,
                Source varchar,
                Tool varchar,
                Arguments varchar,
                Succeeded integer,
                Message varchar,
                TimestampUtc datetime
            );
            """);
        await connection.ExecuteAsync("CREATE TABLE AppSettingEntity (Key varchar PRIMARY KEY NOT NULL, Value varchar);");
        await connection.ExecuteAsync(
            """
            CREATE TABLE ErrorLogEntity (
                Id varchar PRIMARY KEY NOT NULL,
                Level varchar NOT NULL DEFAULT 'Error',
                Context varchar,
                ExceptionType varchar,
                Message varchar,
                StackTrace varchar,
                TimestampUtc datetime
            );
            """);
        await connection.ExecuteAsync(
            """
            INSERT INTO LauncherButtonEntity
                (Id, Command, ButtonText, Tool, Arguments, ClipText, Note, X, Y, Width, Height, UseInvertedThemeColors, CreatedAtUtc, UpdatedAtUtc)
            VALUES
                (?, 'docs', 'Docs', 'open', 'README.md', '', 'documentation', 10, 20, 120, 40, 0, ?, ?);
            """,
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            DateTime.UtcNow);
        await connection.ExecuteAsync("PRAGMA user_version = 4;");
        await connection.CloseAsync();
    }

    private static async Task<bool> ColumnExistsAsync(SQLiteAsyncConnection connection, string tableName, string columnName)
    {
        var count = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM pragma_table_info('{tableName}') WHERE name = ?;",
            columnName);
        return count > 0;
    }
}
