using Praxis.Core.Logic;

namespace Praxis.Tests;

public class PointerButtonClassifierTests
{
    [Fact]
    public void IsPrimaryOnly_ReturnsTrue_ForNullPlatformArgs()
    {
        Assert.True(PointerButtonClassifier.IsPrimaryOnly(null));
    }

    [Fact]
    public void IsMiddle_DetectsOtherMouseEventType()
    {
        var platformArgs = new FakePlatformArgs { Type = "OtherMouseDown" };

        Assert.True(PointerButtonClassifier.IsMiddle(platformArgs));
        Assert.False(PointerButtonClassifier.IsPrimaryOnly(platformArgs));
    }

    [Fact]
    public void IsMiddle_DetectsButtonNumberTwoOrHigher()
    {
        Assert.True(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { ButtonNumber = 2 }));
        Assert.True(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { ButtonNumber = 3 }));
        Assert.False(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { ButtonNumber = 0 }));
        Assert.False(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { ButtonNumber = 1 }));
    }

    [Fact]
    public void IsMiddle_DetectsMiddleButtonMaskBits()
    {
        // 0x4 / 0x8 / 0x10 are the middle/other-mouse bits in the MainPage classifier.
        Assert.True(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { ButtonMask = 0x4UL }));
        Assert.True(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { ButtonMask = 0x8UL }));
        Assert.True(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { ButtonMask = 0x10UL }));
        Assert.False(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { ButtonMask = 0x1UL }));
    }

    [Fact]
    public void IsMiddle_DetectsWindowsIsMiddleButtonPressed()
    {
        Assert.True(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { IsMiddleButtonPressed = true }));
        Assert.False(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { IsMiddleButtonPressed = false }));
    }

    [Fact]
    public void IsMiddle_DetectsTextualPressedButtonMarkers()
    {
        Assert.True(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { PressedButton = "Middle" }));
        Assert.True(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { PressedButton = "Button2" }));
        Assert.True(PointerButtonClassifier.IsMiddle(new FakePlatformArgs { PressedButton = "Auxiliary" }));
    }

    [Fact]
    public void IsMiddle_FollowsCurrentEventChain()
    {
        var inner = new FakePlatformArgs { ButtonNumber = 2 };
        var outer = new FakePlatformArgs { CurrentEvent = inner };

        Assert.True(PointerButtonClassifier.IsMiddle(outer));
    }

    [Fact]
    public void IsMiddle_FollowsGestureRecognizerAndEventChains()
    {
        var gestureHost = new FakePlatformArgs
        {
            GestureRecognizer = new FakePlatformArgs { ButtonNumber = 2 },
        };
        Assert.True(PointerButtonClassifier.IsMiddle(gestureHost));

        var eventHost = new FakePlatformArgs
        {
            Event = new FakePlatformArgs { Type = "OtherMouseUp" },
        };
        Assert.True(PointerButtonClassifier.IsMiddle(eventHost));
    }

    [Fact]
    public void IsSecondary_DetectsRightAndSecondaryMarkers()
    {
        Assert.True(PointerButtonClassifier.IsSecondary(new FakePlatformArgs { IsRightButtonPressed = true }));
        Assert.True(PointerButtonClassifier.IsSecondary(new FakePlatformArgs { PressedButton = "Secondary" }));
        Assert.True(PointerButtonClassifier.IsSecondary(new FakePlatformArgs { Button = "Right" }));
        Assert.True(PointerButtonClassifier.IsSecondary(new FakePlatformArgs { Buttons = "Button1" }));
    }

    [Fact]
    public void IsSecondary_DetectsButtonMaskBitTwo()
    {
        Assert.True(PointerButtonClassifier.IsSecondary(new FakePlatformArgs { ButtonMask = 0x2UL }));
        Assert.False(PointerButtonClassifier.IsSecondary(new FakePlatformArgs { ButtonMask = 0x1UL }));
    }

    [Fact]
    public void IsSecondary_DetectsButtonNumberOne()
    {
        Assert.True(PointerButtonClassifier.IsSecondary(new FakePlatformArgs { ButtonNumber = 1 }));
        Assert.False(PointerButtonClassifier.IsSecondary(new FakePlatformArgs { ButtonNumber = 0 }));
        Assert.False(PointerButtonClassifier.IsSecondary(new FakePlatformArgs { ButtonNumber = 2 }));
    }

    [Fact]
    public void IsSecondary_DoesNotMisfire_ForMiddleButtonValues()
    {
        // PressedButton text containing "Middle" or "Button2" must not be classified as
        // secondary (Right/Button1); the button-value helpers prefer middle classification.
        Assert.False(PointerButtonClassifier.IsSecondary(new FakePlatformArgs { PressedButton = "Middle" }));
        Assert.False(PointerButtonClassifier.IsSecondary(new FakePlatformArgs { PressedButton = "Button2" }));
    }

    [Fact]
    public void IsSecondary_FollowsCurrentEventChain()
    {
        var inner = new FakePlatformArgs { ButtonNumber = 1 };
        var outer = new FakePlatformArgs { CurrentEvent = inner };

        Assert.True(PointerButtonClassifier.IsSecondary(outer));
    }

    [Fact]
    public void IsPrimaryOnly_IsFalse_WhenSecondaryOrMiddleDetected()
    {
        Assert.False(PointerButtonClassifier.IsPrimaryOnly(new FakePlatformArgs { IsRightButtonPressed = true }));
        Assert.False(PointerButtonClassifier.IsPrimaryOnly(new FakePlatformArgs { IsMiddleButtonPressed = true }));
        Assert.False(PointerButtonClassifier.IsPrimaryOnly(new FakePlatformArgs { Type = "OtherMouseDown" }));
        Assert.False(PointerButtonClassifier.IsPrimaryOnly(new FakePlatformArgs { ButtonNumber = 1 }));
        Assert.False(PointerButtonClassifier.IsPrimaryOnly(new FakePlatformArgs { ButtonNumber = 2 }));
    }

    [Fact]
    public void IsPrimaryOnly_IsTrue_ForEmptyAndLeftOnlyArgs()
    {
        Assert.True(PointerButtonClassifier.IsPrimaryOnly(new FakePlatformArgs()));
        Assert.True(PointerButtonClassifier.IsPrimaryOnly(new FakePlatformArgs { IsLeftButtonPressed = true }));
        Assert.True(PointerButtonClassifier.IsPrimaryOnly(new FakePlatformArgs { ButtonNumber = 0 }));
    }

    private sealed class FakePlatformArgs
    {
        public string? Type { get; set; }
        public object? GestureRecognizer { get; set; }
        public object? Event { get; set; }
        public object? CurrentEvent { get; set; }
        public bool IsLeftButtonPressed { get; set; }
        public bool? IsMiddleButtonPressed { get; set; }
        public bool? IsRightButtonPressed { get; set; }
        public object? PressedButton { get; set; }
        public object? Button { get; set; }
        public object? Buttons { get; set; }
        public ulong? ButtonMask { get; set; }
        public int? ButtonNumber { get; set; }
    }
}
