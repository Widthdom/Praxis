#if WINDOWS
using Microsoft.UI.Dispatching;
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Praxis.Core.Logic;

namespace Praxis.Controls;

public class CommandEntryHandler : EntryHandler
{
    private static readonly InputScope AlphanumericHalfWidthInputScope = CreateAlphanumericHalfWidthInputScope();
    private DispatcherQueueTimer? inputScopeEnforcementTimer;
    private TextBox? timerAttachedTextBox;
    private bool filteringText;

    protected override void ConnectHandler(TextBox platformView)
    {
        base.ConnectHandler(platformView);
        ApplyAlphanumericInputScope(platformView);
        platformView.GotFocus += PlatformView_GotFocus;
        platformView.LostFocus += PlatformView_LostFocus;
        platformView.TextChanging += PlatformView_TextChanging;
    }

    protected override void DisconnectHandler(TextBox platformView)
    {
        platformView.GotFocus -= PlatformView_GotFocus;
        platformView.LostFocus -= PlatformView_LostFocus;
        platformView.TextChanging -= PlatformView_TextChanging;
        StopInputScopeEnforcementTimer();
        base.DisconnectHandler(platformView);
    }

    private static InputScope CreateAlphanumericHalfWidthInputScope()
    {
        var scope = new InputScope();
        scope.Names.Add(new InputScopeName(InputScopeNameValue.AlphanumericHalfWidth));
        return scope;
    }

    private static void ApplyAlphanumericInputScope(TextBox textBox)
    {
        textBox.InputScope = AlphanumericHalfWidthInputScope;
    }

    private void PlatformView_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
        {
            return;
        }

        ApplyAlphanumericInputScope(textBox);
        StartInputScopeEnforcementTimer(textBox);
    }

    private void PlatformView_LostFocus(object sender, RoutedEventArgs e)
    {
        StopInputScopeEnforcementTimer();
    }

    private void PlatformView_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
    {
        ApplyAlphanumericInputScope(sender);

        if (filteringText)
        {
            return;
        }

        var current = sender.Text ?? string.Empty;
        if (AsciiInputFilter.IsAsciiOnly(current))
        {
            return;
        }

        var filtered = AsciiInputFilter.FilterToAscii(current);
        filteringText = true;
        try
        {
            sender.Text = filtered;
            sender.SelectionStart = WindowsCommandInputImePolicy.ClampSelectionStart(sender.SelectionStart, filtered.Length);
            sender.SelectionLength = 0;
        }
        finally
        {
            filteringText = false;
        }
    }

    private void StartInputScopeEnforcementTimer(TextBox textBox)
    {
        StopInputScopeEnforcementTimer();
        timerAttachedTextBox = textBox;
        var dispatcherQueue = textBox.DispatcherQueue ?? DispatcherQueue.GetForCurrentThread();
        if (dispatcherQueue is null)
        {
            return;
        }

        var timer = dispatcherQueue.CreateTimer();
        timer.Interval = WindowsCommandInputImePolicy.FocusedInputScopeEnforcementInterval;
        timer.IsRepeating = true;
        timer.Tick += InputScopeEnforcementTimer_Tick;
        timer.Start();
        inputScopeEnforcementTimer = timer;
    }

    private void StopInputScopeEnforcementTimer()
    {
        var timer = inputScopeEnforcementTimer;
        if (timer is not null)
        {
            timer.Tick -= InputScopeEnforcementTimer_Tick;
            timer.Stop();
        }

        inputScopeEnforcementTimer = null;
        timerAttachedTextBox = null;
    }

    private void InputScopeEnforcementTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        var textBox = timerAttachedTextBox;
        if (textBox is null || !WindowsCommandInputImePolicy.ShouldEnforceInputScope(textBox.FocusState != FocusState.Unfocused))
        {
            StopInputScopeEnforcementTimer();
            return;
        }

        ApplyAlphanumericInputScope(textBox);
    }
}
#endif
