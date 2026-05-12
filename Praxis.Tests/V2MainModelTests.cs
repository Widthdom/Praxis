using Praxis.Core.Models;
using Praxis.Core.Services;

namespace Praxis.Tests;

public class V2MainModelTests
{
    [Fact]
    public void AddButton_AddsModelOwnedButtonState()
    {
        var model = new MainModel(
            new StubLauncherExecutionService(),
            new InMemoryLauncherButtonRepository());

        model.AddButton();

        var button = Assert.Single(model.Buttons);
        Assert.Same(button, Assert.Single(model.VisibleButtons));
        Assert.Equal("New 1", button.Text);
        Assert.Equal(LauncherButtonColorKey.Default, button.ColorKey);
        Assert.Equal(LauncherStatusKind.Success, model.Status.Kind);
    }

    [Fact]
    public async Task ExecuteButtonAsync_UpdatesStatusAndRecentButtonsFromExecutionResult()
    {
        var button = new LauncherButtonModel { Text = "Build" };
        var model = new MainModel(new StubLauncherExecutionService(
                LauncherExecutionResult.Success("Executed.")),
            new InMemoryLauncherButtonRepository());
        model.Buttons.Add(button);

        await model.ExecuteButtonAsync(button);

        Assert.False(button.IsExecuting);
        Assert.NotNull(button.LastExecutedAtUtc);
        Assert.Equal(LauncherStatusKind.Success, model.Status.Kind);
        Assert.Equal("Executed.", model.Status.Message);
        Assert.Same(button, Assert.Single(model.RecentButtons));
    }

    [Fact]
    public async Task ExecuteButtonAsync_DoesNotAddFailedExecutionToRecentButtons()
    {
        var button = new LauncherButtonModel { Text = "Build" };
        var model = new MainModel(new StubLauncherExecutionService(
                LauncherExecutionResult.Failure("Failed.")),
            new InMemoryLauncherButtonRepository());

        await model.ExecuteButtonAsync(button);

        Assert.Equal(LauncherStatusKind.Error, model.Status.Kind);
        Assert.Empty(model.RecentButtons);
    }

    [Fact]
    public async Task InitializeAsync_LoadsRepositoryButtonsAndAppliesSearchFilter()
    {
        var repository = new InMemoryLauncherButtonRepository();
        await repository.UpsertButtonAsync(new LauncherButtonRecord
        {
            Command = "docs",
            ButtonText = "Docs",
            Tool = "open",
            Arguments = "README.md",
            Note = "documentation",
            X = 10,
            Y = 20,
            ColorKey = LauncherButtonColorKey.Blue,
        });
        var model = new MainModel(new StubLauncherExecutionService(), repository)
        {
            SearchText = "doc",
        };

        await model.InitializeAsync();

        var button = Assert.Single(model.VisibleButtons);
        Assert.Equal("Docs", button.Text);
        Assert.Equal(LauncherButtonColorKey.Blue, button.ColorKey);
        Assert.Equal(10, button.X);
        Assert.Equal(20, button.Y);
    }

    [Fact]
    public async Task InitializeAsync_RestoresPersistedDockOrder()
    {
        var first = new LauncherButtonRecord
        {
            Command = "first",
            ButtonText = "First",
            X = 0,
            Y = 0,
        };
        var second = new LauncherButtonRecord
        {
            Command = "second",
            ButtonText = "Second",
            X = 10,
            Y = 0,
        };
        var repository = new InMemoryLauncherButtonRepository();
        await repository.UpsertButtonAsync(first);
        await repository.UpsertButtonAsync(second);
        await repository.SetDockButtonIdsAsync([second.Id, first.Id]);

        var model = new MainModel(new StubLauncherExecutionService(), repository);

        await model.InitializeAsync();

        Assert.Equal(["Second", "First"], model.RecentButtons.Select(static button => button.Text));
    }

    [Fact]
    public async Task CommandText_RefreshesSuggestionsAndExecutesMatchingButton()
    {
        var repository = new InMemoryLauncherButtonRepository();
        await repository.UpsertButtonAsync(new LauncherButtonRecord
        {
            Command = "docs",
            ButtonText = "Docs",
            Tool = "open",
            Arguments = "README.md",
        });
        var execution = new StubLauncherExecutionService(LauncherExecutionResult.Success("Executed."));
        var model = new MainModel(execution, repository);
        await model.InitializeAsync();

        model.CommandText = "do";

        var suggestion = Assert.Single(model.CommandSuggestions);
        Assert.Equal("docs", suggestion.Command);

        model.CommandText = "docs";
        await model.ExecuteCommandInputAsync();

        Assert.Single(execution.ExecutedButtons);
        Assert.NotNull(model.Buttons[0].LastExecutedAtUtc);
        Assert.Same(model.Buttons[0], Assert.Single(model.RecentButtons));
        Assert.Equal([model.Buttons[0].Id], await repository.GetDockButtonIdsAsync());
    }

    [Fact]
    public async Task DeleteButtonAsync_RemovesButtonAndPersistedDockEntry()
    {
        var repository = new InMemoryLauncherButtonRepository();
        var record = new LauncherButtonRecord
        {
            Command = "docs",
            ButtonText = "Docs",
        };
        await repository.UpsertButtonAsync(record);
        await repository.SetDockButtonIdsAsync([record.Id]);
        var model = new MainModel(new StubLauncherExecutionService(), repository);
        await model.InitializeAsync();

        await model.DeleteButtonAsync(model.Buttons.Single());

        Assert.Empty(model.Buttons);
        Assert.Empty(model.VisibleButtons);
        Assert.Empty(model.RecentButtons);
        Assert.Empty(await repository.GetDockButtonIdsAsync());
    }

    [Fact]
    public async Task MoveButtonAsync_SnapsCoordinatesAndPersists()
    {
        var repository = new InMemoryLauncherButtonRepository();
        var model = new MainModel(new StubLauncherExecutionService(), repository);
        model.AddButton();
        var button = model.Buttons.Single();

        await model.MoveButtonAsync(button, 23, 37);

        var persisted = await repository.GetByIdAsync(button.Id, forceReload: true);
        Assert.NotNull(persisted);
        Assert.Equal(20, persisted.X);
        Assert.Equal(40, persisted.Y);
    }

    [Fact]
    public void ApplySelection_SelectsButtonsFullyInsideRectangle()
    {
        var model = new MainModel(
            new StubLauncherExecutionService(),
            new InMemoryLauncherButtonRepository());
        var inside = new LauncherButtonModel { Text = "Inside", X = 20, Y = 20, Width = 100, Height = 40 };
        var crossing = new LauncherButtonModel { Text = "Crossing", X = 120, Y = 20, Width = 100, Height = 40 };
        var outside = new LauncherButtonModel { Text = "Outside", X = 260, Y = 20, Width = 100, Height = 40 };
        model.Buttons.Add(inside);
        model.Buttons.Add(crossing);
        model.Buttons.Add(outside);

        model.ApplySelection(new SelectionPayload
        {
            StartX = 10,
            StartY = 10,
            CurrentX = 140,
            CurrentY = 80,
            Status = InteractionStatus.Started,
        });

        Assert.True(inside.IsSelected);
        Assert.False(crossing.IsSelected);
        Assert.False(outside.IsSelected);
    }

    [Fact]
    public async Task HandleButtonDragAsync_MovesSelectedButtonGroupAndPersists()
    {
        var repository = new InMemoryLauncherButtonRepository();
        var model = new MainModel(new StubLauncherExecutionService(), repository);
        var first = new LauncherButtonModel { Text = "First", X = 20, Y = 20, IsSelected = true };
        var second = new LauncherButtonModel { Text = "Second", X = 160, Y = 20, IsSelected = true };
        model.Buttons.Add(first);
        model.Buttons.Add(second);

        await model.HandleButtonDragAsync(new ButtonDragPayload
        {
            Button = first,
            Status = InteractionStatus.Started,
        });
        await model.HandleButtonDragAsync(new ButtonDragPayload
        {
            Button = first,
            Status = InteractionStatus.Completed,
            TotalX = 23,
            TotalY = 37,
        });

        Assert.Equal(40, first.X);
        Assert.Equal(60, first.Y);
        Assert.Equal(180, second.X);
        Assert.Equal(60, second.Y);

        var persistedFirst = await repository.GetByIdAsync(first.Id, forceReload: true);
        var persistedSecond = await repository.GetByIdAsync(second.Id, forceReload: true);
        Assert.NotNull(persistedFirst);
        Assert.NotNull(persistedSecond);
        Assert.Equal(40, persistedFirst.X);
        Assert.Equal(180, persistedSecond.X);
    }

    [Fact]
    public async Task AddButtonAsync_OpensEditorAndSavePersistsNewButton()
    {
        var repository = new InMemoryLauncherButtonRepository();
        var model = new MainModel(new StubLauncherExecutionService(), repository);

        await model.AddButtonAsync();

        Assert.True(model.IsEditorOpen);
        Assert.NotNull(model.EditorButton);
        model.EditorButton.Text = "Docs";
        model.EditorButton.Command = "docs";
        await model.SaveEditorAsync();

        var button = Assert.Single(model.Buttons);
        Assert.Equal("Docs", button.Text);
        Assert.False(model.IsEditorOpen);
        Assert.NotNull(await repository.GetByIdAsync(button.Id, forceReload: true));
    }

    [Fact]
    public async Task UndoAsync_RevertsSavedAdd()
    {
        var repository = new InMemoryLauncherButtonRepository();
        var model = new MainModel(new StubLauncherExecutionService(), repository);
        model.OpenNewButtonEditor(new NewButtonPayload { X = 40, Y = 40, HasPosition = true });
        Assert.NotNull(model.EditorButton);
        var id = model.EditorButton.Id;

        await model.SaveEditorAsync();
        await model.UndoAsync();

        Assert.Empty(model.Buttons);
        Assert.Null(await repository.GetByIdAsync(id, forceReload: true));
    }

    [Fact]
    public void CoreV2Models_DoNotExposeAvaloniaTypes()
    {
        var modelTypes = new[]
        {
            typeof(MainModel),
            typeof(LauncherButtonModel),
            typeof(StatusModel),
        };

        var exposedTypes = modelTypes
            .SelectMany(static type => type.GetProperties())
            .Select(static property => property.PropertyType)
            .Where(static type => type.FullName?.StartsWith("Avalonia.", StringComparison.Ordinal) == true)
            .ToList();

        Assert.Empty(exposedTypes);
    }

    private sealed class StubLauncherExecutionService : ILauncherExecutionService
    {
        private readonly LauncherExecutionResult result;

        public List<LauncherButtonModel> ExecutedButtons { get; } = [];

        public StubLauncherExecutionService()
            : this(LauncherExecutionResult.Success("OK"))
        {
        }

        public StubLauncherExecutionService(LauncherExecutionResult result)
        {
            this.result = result;
        }

        public Task<LauncherExecutionResult> ExecuteAsync(
            LauncherButtonModel button,
            CancellationToken cancellationToken = default)
        {
            ExecutedButtons.Add(button);
            return Task.FromResult(result);
        }
    }
}
