using System.ComponentModel;

using Praxis.Core.Logic;
using Praxis.ViewModels;

namespace Praxis;

public partial class MainPage
{
    private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Editor))
        {
            AttachEditorPropertyChanged(viewModel.Editor);
            UpdateModalEditorHeights();
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.SelectedTheme))
        {
            ApplyNeutralStatusBackground();
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.StatusRevision))
        {
            TriggerStatusFlash(viewModel.StatusText);
#if MACCATALYST
            RefocusMainCommandAfterCommandNotFound(viewModel.StatusText);
#endif
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.IsCommandSuggestionOpen))
        {
            Dispatcher.Dispatch(UpdateCommandSuggestionPopupPlacement);
            return;
        }

        if (e.PropertyName == nameof(MainViewModel.IsContextMenuOpen))
        {
            App.SetContextMenuOpenState(viewModel.IsContextMenuOpen);
            if (viewModel.IsContextMenuOpen)
            {
                CloseCommandSuggestionPopup();
                HideQuickLookPopup();
#if MACCATALYST
                ResignMainInputFirstResponder();
#endif
            }
            ApplyTabPolicy();
            if (viewModel.IsContextMenuOpen)
            {
#if MACCATALYST
                SetMacContextMenuPseudoFocus(ContextMenuFocusTarget.Edit);
#endif
                Dispatcher.DispatchDelayed(UiTimingPolicy.ContextMenuFocusInitialDelay, () =>
                {
                    if (!viewModel.IsContextMenuOpen || viewModel.IsEditorOpen || IsConflictDialogOpen())
                    {
                        return;
                    }

                    FocusContextActionButton(ContextEditButton);
#if MACCATALYST
                    EnsureMacFirstResponder();
#endif
                    ApplyContextActionButtonFocusVisuals();
                });
                Dispatcher.DispatchDelayed(UiTimingPolicy.ContextMenuFocusRetryDelay, () =>
                {
                    if (!viewModel.IsContextMenuOpen || IsButtonFocused(ContextEditButton))
                    {
                        return;
                    }

                    FocusContextActionButton(ContextEditButton);
                    ApplyContextActionButtonFocusVisuals();
                });
            }
            else
            {
#if MACCATALYST
                ClearMacContextMenuPseudoFocus();
#endif
            }

            ApplyContextActionButtonFocusVisuals();

            return;
        }

        if (e.PropertyName != nameof(MainViewModel.IsEditorOpen) || !viewModel.IsEditorOpen)
        {
            if (e.PropertyName == nameof(MainViewModel.IsEditorOpen))
            {
                App.SetEditorOpenState(viewModel.IsEditorOpen);
                if (!viewModel.IsEditorOpen)
                {
                    modalPrimaryFieldSelectAllPending = false;
                }
                ApplyTabPolicy();
                UpdateModalEditorHeights();
#if MACCATALYST
                if (!viewModel.IsEditorOpen)
                {
                    ClearMacModalPseudoFocus();
                    macGuidLockedText = string.Empty;
                }
#endif
            }
            return;
        }

        App.SetEditorOpenState(true);
        HideQuickLookPopup();
        ApplyTabPolicy();
        UpdateModalEditorHeights();
#if MACCATALYST
        macGuidLockedText = viewModel.Editor.GuidText ?? string.Empty;
#endif
        modalPrimaryFieldSelectAllPending = !viewModel.Editor.IsExistingRecord;
        Dispatcher.DispatchDelayed(UiTimingPolicy.EditorOpenFocusDelay, () =>
        {
            if (!viewModel.IsEditorOpen || IsConflictDialogOpen())
            {
                return;
            }

            FocusModalPrimaryEditorField();
#if MACCATALYST
            EnsureMacFirstResponder();
            ApplyMacEditorKeyCommands();
            Dispatcher.DispatchDelayed(UiTimingPolicy.MacEditorKeyCommandsRetryDelay, ApplyMacEditorKeyCommands);
#endif
        });
    }

    private void AttachEditorPropertyChanged(ButtonEditorViewModel? editorViewModel)
    {
        if (ReferenceEquals(observedEditorViewModel, editorViewModel))
        {
            return;
        }

        if (observedEditorViewModel is not null)
        {
            observedEditorViewModel.PropertyChanged -= EditorViewModelOnPropertyChanged;
        }

        observedEditorViewModel = editorViewModel;
        if (observedEditorViewModel is not null)
        {
            observedEditorViewModel.PropertyChanged += EditorViewModelOnPropertyChanged;
        }
    }

    private void DetachEditorPropertyChanged()
    {
        if (observedEditorViewModel is null)
        {
            return;
        }

        observedEditorViewModel.PropertyChanged -= EditorViewModelOnPropertyChanged;
        observedEditorViewModel = null;
    }

    private void EditorViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(ButtonEditorViewModel.ClipText) or nameof(ButtonEditorViewModel.Note) or ""))
        {
            return;
        }

        Dispatcher.Dispatch(() => UpdateModalEditorHeights());
    }
}
