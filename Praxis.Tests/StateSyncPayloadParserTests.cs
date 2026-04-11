using Praxis.Core.Logic;

namespace Praxis.Tests;

public class StateSyncPayloadParserTests
{
    [Fact]
    public void TryParse_ReturnsTrue_ForValidPayload()
    {
        var timestamp = new DateTime(2026, 4, 11, 12, 0, 0, DateTimeKind.Utc);
        var payload = $"instance123|{timestamp.Ticks}";

        var parsed = StateSyncPayloadParser.TryParse(payload, out var source, out var parsedTimestamp);

        Assert.True(parsed);
        Assert.Equal("instance123", source);
        Assert.Equal(timestamp, parsedTimestamp);
    }

    [Fact]
    public void TryParse_ReturnsFalse_ForPayloadWithMissingParts()
    {
        var parsed = StateSyncPayloadParser.TryParse("instance-only", out _, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryParse_ReturnsFalse_ForPayloadWithInvalidTicks()
    {
        var parsed = StateSyncPayloadParser.TryParse("instance|not-a-number", out _, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryParse_ReturnsFalse_ForPayloadWithOutOfRangeTicks()
    {
        var payload = $"instance|{long.MaxValue}";

        var parsed = StateSyncPayloadParser.TryParse(payload, out _, out _);

        Assert.False(parsed);
    }

    [Fact]
    public void TryParse_ReturnsFalse_ForPayloadWithBlankSource()
    {
        var payload = $"   |{DateTime.UtcNow.Ticks}";

        var parsed = StateSyncPayloadParser.TryParse(payload, out _, out _);

        Assert.False(parsed);
    }
}
