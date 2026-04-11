using Praxis.Core.Logic;

namespace Praxis.Tests;

public class DockOrderValueCodecTests
{
    [Fact]
    public void Parse_IgnoresInvalidDuplicateAndEmptyGuids()
    {
        var first = Guid.Parse("64822CE6-2DCB-41E7-B260-7637AD30449D");
        var second = Guid.Parse("A31B579C-0DAD-43A6-83CF-55AB2E721DAA");
        var csv = $"{first},invalid,{Guid.Empty},{second},{first}";

        var result = DockOrderValueCodec.Parse(csv);

        Assert.Equal([first, second], result);
    }

    [Fact]
    public void Parse_ReturnsEmpty_ForBlankValue()
    {
        Assert.Empty(DockOrderValueCodec.Parse("   "));
    }

    [Fact]
    public void Parse_TrimsWrappingQuotes_FromWholeCsv_AndEntries()
    {
        var first = Guid.Parse("64822CE6-2DCB-41E7-B260-7637AD30449D");
        var second = Guid.Parse("A31B579C-0DAD-43A6-83CF-55AB2E721DAA");

        var result = DockOrderValueCodec.Parse($" \"'{first}',\"{second}\"\" ");

        Assert.Equal([first, second], result);
    }

    [Fact]
    public void Serialize_DeduplicatesAndSkipsEmptyGuids()
    {
        var first = Guid.Parse("64822CE6-2DCB-41E7-B260-7637AD30449D");
        var second = Guid.Parse("A31B579C-0DAD-43A6-83CF-55AB2E721DAA");

        var result = DockOrderValueCodec.Serialize([first, Guid.Empty, second, first]);

        Assert.Equal($"{first},{second}", result);
    }
}
