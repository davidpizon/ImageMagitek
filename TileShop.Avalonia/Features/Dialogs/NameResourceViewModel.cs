﻿using CommunityToolkit.Mvvm.ComponentModel;
using TileShop.AvaloniaUI.Windowing;

namespace TileShop.AvaloniaUI.ViewModels;

public partial class NameResourceViewModel : DialogViewModel<string?>
{
    [ObservableProperty] private string? _resourceName;

    public NameResourceViewModel()
    {
        Title = "Name Resource";
        AcceptName = "✓";
        CancelName = "x";
    }

    protected override void Accept()
    {
        _requestResult = ResourceName;
        OnPropertyChanged(nameof(RequestResult));
    }
}
