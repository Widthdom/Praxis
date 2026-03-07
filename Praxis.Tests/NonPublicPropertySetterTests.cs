using Praxis.Core.Logic;

namespace Praxis.Tests;

public class NonPublicPropertySetterTests
{
    private sealed class PublicTarget
    {
        public object? ProtectedCursor { get; set; }
    }

    private sealed class PrivateTarget
    {
        private object? ProtectedCursor { get; set; }
        public object? GetCursor() => ProtectedCursor;
    }

    private sealed class ReadOnlyTarget
    {
        public object? ProtectedCursor { get; } = null;
    }

    private sealed class StringTarget
    {
        public string? ProtectedCursor { get; set; }
    }

    [Fact]
    public void TrySet_ReturnsTrue_ForPublicWritableProperty()
    {
        var target = new PublicTarget();
        var cursor = new object();

        var ok = NonPublicPropertySetter.TrySet(target, "ProtectedCursor", cursor);

        Assert.True(ok);
        Assert.Same(cursor, target.ProtectedCursor);
    }

    [Fact]
    public void TrySet_ReturnsTrue_ForNonPublicWritableProperty()
    {
        var target = new PrivateTarget();
        var cursor = new object();

        var ok = NonPublicPropertySetter.TrySet(target, "ProtectedCursor", cursor);

        Assert.True(ok);
        Assert.Same(cursor, target.GetCursor());
    }

    [Fact]
    public void TrySet_ReturnsFalse_WhenPropertyMissingReadOnlyOrTypeMismatch()
    {
        var readOnly = new ReadOnlyTarget();
        var publicTarget = new PublicTarget();
        var stringTarget = new StringTarget();

        Assert.False(NonPublicPropertySetter.TrySet(readOnly, "ProtectedCursor", new object()));
        Assert.False(NonPublicPropertySetter.TrySet(publicTarget, "Missing", new object()));
        Assert.False(NonPublicPropertySetter.TrySet(stringTarget, "ProtectedCursor", 123));
    }
}
