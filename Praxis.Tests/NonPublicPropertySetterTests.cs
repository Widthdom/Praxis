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

    private sealed class NullableTarget
    {
        public object? ProtectedCursor { get; set; } = new object();
    }

    private sealed class IndexerTarget
    {
        public object? this[int index]
        {
            get => null;
            set { }
        }
    }

    private sealed class ThrowingSetterTarget
    {
        public object? ProtectedCursor
        {
            get => null;
            set => throw new InvalidOperationException("setter failure");
        }
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

    [Fact]
    public void TrySet_ReturnsFalse_WhenTargetIsNull_OrPropertyNameIsBlank()
    {
        Assert.False(NonPublicPropertySetter.TrySet(null, "ProtectedCursor", new object()));
        Assert.False(NonPublicPropertySetter.TrySet(new PublicTarget(), "", new object()));
        Assert.False(NonPublicPropertySetter.TrySet(new PublicTarget(), "   ", new object()));
    }

    [Fact]
    public void TrySet_ReturnsTrue_WhenAssigningNull_ToNullableProperty()
    {
        var target = new NullableTarget();

        var ok = NonPublicPropertySetter.TrySet(target, "ProtectedCursor", null);

        Assert.True(ok);
        Assert.Null(target.ProtectedCursor);
    }

    [Fact]
    public void TrySet_ReturnsFalse_ForIndexerProperty()
    {
        var target = new IndexerTarget();

        var ok = NonPublicPropertySetter.TrySet(target, "Item", new object());

        Assert.False(ok);
    }

    [Fact]
    public void TrySet_ReturnsFalse_WhenSetterThrows()
    {
        var target = new ThrowingSetterTarget();

        var ok = NonPublicPropertySetter.TrySet(target, "ProtectedCursor", new object());

        Assert.False(ok);
    }
}
