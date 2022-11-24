using System;
using Avalonia.Controls;
using Avalonia.Input;
using TileShop.AvaloniaUI.Input;
using TileShop.AvaloniaUI.ViewModels;
using TileShop.Shared.Input;

namespace TileShop.AvaloniaUI.Views;
public partial class SequentialArrangerEditorView : UserControl, IStateViewDriver<SequentialArrangerEditorViewModel>
{
    public SequentialArrangerEditorViewModel? ViewModel { get; private set; }

    public SequentialArrangerEditorView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (DataContext is SequentialArrangerEditorViewModel vm)
        {
            ViewModel = vm;
            ViewModel.OnImageModified = () => _image.InvalidateVisual();
        }
        base.OnDataContextChanged(e);
    }

    public void OnKeyUp(object? sender, KeyEventArgs e) { }

    public void OnKeyDown(object? sender, KeyEventArgs e) { }

    public void OnPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse && ViewModel is not null)
        {
            var point = e.GetCurrentPoint(_image);
            var state = InputAdapter.CreateMouseState(point, e.KeyModifiers);
            ViewModel.MouseDown(point.Position.X, point.Position.Y, state);
        }
    }

    public void OnPointerReleased(object sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse && ViewModel is not null)
        {
            var point = e.GetCurrentPoint(_image);
            var state = InputAdapter.CreateMouseState(point, e.KeyModifiers);
            ViewModel.MouseUp(point.Position.X, point.Position.Y, state);
        }
    }

    public void OnPointerMoved(object sender, PointerEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse && ViewModel is not null)
        {
            var point = e.GetCurrentPoint(_image);

            var state = InputAdapter.CreateMouseState(point, e.KeyModifiers);
            ViewModel.MouseMove(point.Position.X, point.Position.Y, state);
        }
    }

    public void OnPointerExited(object sender, PointerEventArgs e)
    {
        ViewModel?.MouseLeave();
    }

    public void OnPointerWheelChanged(object sender, PointerWheelEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse && ViewModel is not null)
        {
            var modifiers = InputAdapter.CreateKeyModifiers(e.KeyModifiers);

            if (e.Delta.Y > 0)
            {
                ViewModel.MouseWheel(MouseWheelDirection.Up, modifiers);
            }
            else if (e.Delta.Y < 0)
            {
                ViewModel.MouseWheel(MouseWheelDirection.Down, modifiers);
            }
        }
    }
}
