#if MACCATALYST
using CoreAnimation;
using CoreGraphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;

namespace Praxis.Controls;

public class MacEntryHandler : EntryHandler
{
    protected override MauiTextField CreatePlatformView()
    {
        return new MacEntryTextField();
    }

    public class MacEntryTextField : MauiTextField
    {
        private static readonly CGColor LightBorderColor = UIColor.FromRGB(0xCE, 0xCE, 0xCE).CGColor;
        private static readonly CGColor DarkBorderColor = UIColor.FromRGB(0x4E, 0x4E, 0x4E).CGColor;
        private static readonly CGColor LightFocusUnderlineColor = UIColor.FromRGB(0x4A, 0x4A, 0x4A).CGColor;
        private static readonly CGColor DarkFocusUnderlineColor = UIColor.FromRGB(0xA0, 0xA0, 0xA0).CGColor;
        private static readonly nfloat CornerRadius = 4;
        private static readonly nfloat BorderWidth = 1;
        private static readonly nfloat FocusBorderWidth = 1.5f;
        private static readonly nfloat HorizontalInset = 10;
        private readonly CAShapeLayer borderLayer = new();
        private readonly CAShapeLayer focusBorderLayer = new();
        private readonly CALayer focusBorderMaskLayer = new();

        public MacEntryTextField()
        {
            BorderStyle = UITextBorderStyle.None;
            Layer.CornerRadius = CornerRadius;
            Layer.BorderWidth = 0;
            Layer.MasksToBounds = false;

            borderLayer.FillColor = UIColor.Clear.CGColor;
            borderLayer.LineWidth = BorderWidth;
            Layer.AddSublayer(borderLayer);

            focusBorderLayer.FillColor = UIColor.Clear.CGColor;
            focusBorderLayer.LineWidth = FocusBorderWidth;
            focusBorderLayer.Mask = focusBorderMaskLayer;
            focusBorderLayer.Hidden = true;
            Layer.AddSublayer(focusBorderLayer);
        }

        public override CGRect TextRect(CGRect forBounds)
            => forBounds.Inset(HorizontalInset, 0);

        public override CGRect EditingRect(CGRect forBounds)
            => forBounds.Inset(HorizontalInset, 0);

        public override CGRect PlaceholderRect(CGRect forBounds)
            => forBounds.Inset(HorizontalInset, 0);

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            var halfBorderWidth = BorderWidth / 2;
            var borderRect = Bounds.Inset(halfBorderWidth, halfBorderWidth);
            borderLayer.Path = UIBezierPath.FromRoundedRect(borderRect, CornerRadius).CGPath;

            var focusHalfBorderWidth = FocusBorderWidth / 2;
            var focusRect = Bounds.Inset(focusHalfBorderWidth, focusHalfBorderWidth);
            focusBorderLayer.Path = UIBezierPath.FromRoundedRect(focusRect, CornerRadius).CGPath;
            focusBorderLayer.Frame = Bounds;

            var bottomMaskInset = Math.Max(2, CornerRadius + 1);
            var bottomMaskHeight = Math.Max(1, FocusBorderWidth + 1);
            focusBorderMaskLayer.Frame = new CGRect(
                bottomMaskInset,
                Math.Max(0, Bounds.Height - bottomMaskHeight),
                Math.Max(0, Bounds.Width - (bottomMaskInset * 2)),
                bottomMaskHeight);
            focusBorderMaskLayer.BackgroundColor = UIColor.White.CGColor;
            ApplyFocusVisualState();
        }

        public override bool BecomeFirstResponder()
        {
            var result = base.BecomeFirstResponder();
            ApplyFocusVisualState();
            return result;
        }

        public override bool ResignFirstResponder()
        {
            var result = base.ResignFirstResponder();
            ApplyFocusVisualState();
            return result;
        }

        protected void ApplyFocusVisualState()
        {
            var dark = TraitCollection?.UserInterfaceStyle == UIUserInterfaceStyle.Dark;
            var borderColor = dark ? DarkBorderColor : LightBorderColor;
            var focusColor = dark ? DarkFocusUnderlineColor : LightFocusUnderlineColor;
            TintColor = dark ? UIColor.White : UIColor.Black;
            borderLayer.StrokeColor = borderColor;
            focusBorderLayer.StrokeColor = focusColor;
            focusBorderLayer.Hidden = !IsFirstResponder;
        }
    }
}
#endif
