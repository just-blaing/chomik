using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;

namespace chomik;

public partial class AppSelector : drag_window
{
    public string selected_app { get; private set; } = string.Empty;

    public AppSelector()
    {
        InitializeComponent();
        this.FindControl<TextBlock>("title_text")!.Text = localization.t("pick_app");
        this.FindControl<Button>("select_btn")!.Content = localization.t("pick");
        this.FindControl<Button>("cancel_btn")!.Content = localization.t("cancel");
    }

    public AppSelector(List<string> apps) : this()
    {
        var lst = this.FindControl<ListBox>("app_list")!;
        lst.ItemsSource = apps;
        if (apps.Count > 0) lst.SelectedIndex = 0;
    }

    private void on_select_click(object? sender, RoutedEventArgs e)
    {
        var lst = this.FindControl<ListBox>("app_list")!;
        if (lst.SelectedItem == null) return;
        selected_app = lst.SelectedItem.ToString() ?? string.Empty;
        Close(true);
    }

    private void on_cancel_click(object? sender, RoutedEventArgs e) => Close(false);
}
