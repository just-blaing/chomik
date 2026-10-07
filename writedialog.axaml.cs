using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System;

namespace chomik;

public partial class WriteDialog : drag_window
{
    public WriteDialog()
    {
        InitializeComponent();
        this.FindControl<TextBlock>("title_text")!.Text = localization.t("write_title");
        this.FindControl<Button>("cancel_btn")!.Content = localization.t("cancel");
        this.FindControl<Button>("ok_btn")!.Content = localization.t("ok");
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        this.FindControl<TextBox>("input_box")!.Focus();
    }

    private void on_text_changed(object? sender, TextChangedEventArgs e)
    {
        int len = this.FindControl<TextBox>("input_box")!.Text?.Length ?? 0;
        this.FindControl<TextBlock>("counter_label")!.Text = $"{len}/60";
    }

    private void on_key_down(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return) submit();
        else if (e.Key == Key.Escape) Close(null);
    }

    private void on_ok_click(object? sender, RoutedEventArgs e) => submit();

    private void on_cancel_click(object? sender, RoutedEventArgs e) => Close(null);

    private void submit()
    {
        string? text = this.FindControl<TextBox>("input_box")!.Text?.Trim();
        if (string.IsNullOrEmpty(text)) return;
        Close(text);
    }
}
