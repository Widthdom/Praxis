using Praxis.Core.Logic;
using Praxis.Core.Models;

namespace Praxis.Tests;

public class ModelDefaultsTests
{
    [Fact]
    public void LauncherButtonRecord_Defaults_AreInitializedAsExpected()
    {
        var record = new LauncherButtonRecord();

        Assert.NotEqual(Guid.Empty, record.Id);
        Assert.Equal(string.Empty, record.Command);
        Assert.Equal(string.Empty, record.ButtonText);
        Assert.Equal(string.Empty, record.Tool);
        Assert.Equal(string.Empty, record.Arguments);
        Assert.Equal(string.Empty, record.ClipText);
        Assert.Equal(string.Empty, record.Note);
        Assert.Equal(0, record.X);
        Assert.Equal(0, record.Y);
        Assert.Equal(ButtonLayoutDefaults.Width, record.Width);
        Assert.Equal(ButtonLayoutDefaults.Height, record.Height);
        Assert.Equal(DateTimeKind.Utc, record.CreatedAtUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, record.UpdatedAtUtc.Kind);
    }

    [Fact]
    public void LauncherButtonRecord_DefaultIds_AreUniqueAcrossInstances()
    {
        var a = new LauncherButtonRecord();
        var b = new LauncherButtonRecord();

        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void LaunchLogEntry_Defaults_AreInitializedAsExpected()
    {
        var entry = new LaunchLogEntry();

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Null(entry.ButtonId);
        Assert.Equal(string.Empty, entry.Source);
        Assert.Equal(string.Empty, entry.Tool);
        Assert.Equal(string.Empty, entry.Arguments);
        Assert.False(entry.Succeeded);
        Assert.Equal(string.Empty, entry.Message);
        Assert.Equal(DateTimeKind.Utc, entry.TimestampUtc.Kind);
    }
}
