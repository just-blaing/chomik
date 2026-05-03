using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Collections.Generic;

namespace chomik;

public partial class AppSelector : Window
{
    private Point mouse_offset;
    private bool is_mouse_down;
    public string selected_app { get; private set; } = string.Empty;

    public AppSelector()
    {
        InitializeComponent();
    }

    public AppSelector(List<string> apps)
    {
        InitializeComponent();
        var lst = this.FindControl<ListBox>("app_list")!;
        lst.ItemsSource = apps;
        if (apps.Count > 0) lst.SelectedIndex = 0;
    }

    private void on_select_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var lst = this.FindControl<ListBox>("app_list")!;
        if (lst.SelectedItem != null)
        {
            selected_app = lst.SelectedItem.ToString() ?? "";
            Close(true);
        }
    }

    private void on_cancel_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(false);

    private void on_pointer_pressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var screen_click = this.PointToScreen(e.GetPosition(this));
            mouse_offset = new Point(this.Position.X - screen_click.X, this.Position.Y - screen_click.Y);
            is_mouse_down = true;
        }
    }

    private void on_pointer_moved(object? sender, PointerEventArgs e)
    {
        if (is_mouse_down)
        {
            var screen_pos = this.PointToScreen(e.GetPosition(this));
            Position = new Avalonia.PixelPoint((int)(screen_pos.X + mouse_offset.X), (int)(screen_pos.Y + mouse_offset.Y));
        }
    }

    private void on_pointer_released(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left) is_mouse_down = false;
    }
}