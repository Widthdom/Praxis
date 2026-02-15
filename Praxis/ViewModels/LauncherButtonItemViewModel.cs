using CommunityToolkit.Mvvm.ComponentModel;
using Praxis.Core.Models;

namespace Praxis.ViewModels;

public partial class LauncherButtonItemViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isSelected;

    private readonly LauncherButtonRecord model;

    public LauncherButtonItemViewModel(LauncherButtonRecord model)
    {
        this.model = model;
    }

    public Guid Id => model.Id;
    public string Command => model.Command;
    public string Tool => model.Tool;
    public string Arguments => model.Arguments;
    public string ClipText => model.ClipText;
    public string Note => model.Note;

    public string ButtonText
    {
        get => model.ButtonText;
        set
        {
            if (model.ButtonText != value)
            {
                model.ButtonText = value;
                OnPropertyChanged();
            }
        }
    }

    public double X { get => model.X; set { model.X = value; OnPropertyChanged(); OnPropertyChanged(nameof(LayoutBounds)); } }
    public double Y { get => model.Y; set { model.Y = value; OnPropertyChanged(); OnPropertyChanged(nameof(LayoutBounds)); } }
    public double Width { get => model.Width; set { model.Width = value; OnPropertyChanged(); OnPropertyChanged(nameof(LayoutBounds)); } }
    public double Height { get => model.Height; set { model.Height = value; OnPropertyChanged(); OnPropertyChanged(nameof(LayoutBounds)); } }

    public Rect LayoutBounds => new(X, Y, Width, Height);

    public LauncherButtonRecord ToRecord() => new()
    {
        Id = model.Id,
        Command = model.Command,
        ButtonText = model.ButtonText,
        Tool = model.Tool,
        Arguments = model.Arguments,
        ClipText = model.ClipText,
        Note = model.Note,
        X = model.X,
        Y = model.Y,
        Width = model.Width,
        Height = model.Height,
        CreatedAtUtc = model.CreatedAtUtc,
        UpdatedAtUtc = DateTime.UtcNow,
    };

    public void Overwrite(LauncherButtonRecord source)
    {
        model.Command = source.Command;
        model.ButtonText = source.ButtonText;
        model.Tool = source.Tool;
        model.Arguments = source.Arguments;
        model.ClipText = source.ClipText;
        model.Note = source.Note;
        model.X = source.X;
        model.Y = source.Y;
        model.Width = source.Width;
        model.Height = source.Height;
        OnPropertyChanged(string.Empty);
    }
}
