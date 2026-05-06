using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace chomik;

public partial class WriteDialog : Window
{
    private Point mouse_offset;
    private bool is_mouse_down;

    public WriteDialog()
    {
        InitializeComponent();
    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        this.FindControl<TextBox>("input_box")!.Focus();
    }

    private void on_text_changed(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        int len = this.FindControl<TextBox>("input_box")!.Text?.Length ?? 0;
        this.FindControl<TextBlock>("counter_label")!.Text = $"{len}/60";
    }

    private void on_key_down(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) submit();
        else if (e.Key == Key.Escape) Close(null);
    }

    private void on_ok_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => submit();

    private void on_cancel_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close(null);

    private void submit()
    {
        string? text = this.FindControl<TextBox>("input_box")!.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        Close(text);
    }

    private void on_pointer_pressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var pos = e.GetPosition(this);
        var screen_click = this.PointToScreen(pos);
        mouse_offset = new Point(this.Position.X - screen_click.X, this.Position.Y - screen_click.Y);
        is_mouse_down = true;
    }

    private void on_pointer_moved(object? sender, PointerEventArgs e)
    {
        if (!is_mouse_down) return;
        var screen_pos = this.PointToScreen(e.GetPosition(this));
        Position = new PixelPoint((int)(screen_pos.X + mouse_offset.X), (int)(screen_pos.Y + mouse_offset.Y));
    }

    private void on_pointer_released(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left) is_mouse_down = false;
    }
}
