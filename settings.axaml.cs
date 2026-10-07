using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace chomik;

public partial class Settings : drag_window
{
    public bool is_listening_enabled { get; private set; }
    public bool real_eat_files { get; private set; }
    public bool perm_delete { get; private set; }
    public string language { get; private set; } = localization.lang;
    public List<string> music_whitelist { get; private set; } = new();
    private ObservableCollection<string> whitelist_items = new();

    public Settings()
    {
        InitializeComponent();
        this.FindControl<CheckBox>("music_check")!.Content = localization.t("music");
        this.FindControl<CheckBox>("eat_check")!.Content = localization.t("eat");
        this.FindControl<RadioButton>("eat_trash_radio")!.Content = localization.t("trash");
        this.FindControl<RadioButton>("eat_perm_radio")!.Content = localization.t("perm");
        this.FindControl<TextBlock>("whitelist_text")!.Text = localization.t("whitelist");
        this.FindControl<TextBlock>("language_text")!.Text = localization.t("language");
        this.FindControl<Button>("ok_btn")!.Content = localization.t("ok");
        this.FindControl<Button>("cancel_btn")!.Content = localization.t("cancel");
        this.FindControl<RadioButton>(localization.lang == "ru" ? "lang_ru_radio" : "lang_en_radio")!.IsChecked = true;
    }

    public Settings(bool initial_music, List<string> initial_whitelist, bool initial_eat, bool initial_perm_delete) : this()
    {
        is_listening_enabled = initial_music;
        real_eat_files = initial_eat;
        perm_delete = initial_perm_delete;
        music_whitelist.AddRange(initial_whitelist);

        foreach (var app in music_whitelist) whitelist_items.Add(app);
        this.FindControl<ListBox>("whitelist_box")!.ItemsSource = whitelist_items;
        this.FindControl<CheckBox>("music_check")!.IsChecked = is_listening_enabled;
        this.FindControl<CheckBox>("eat_check")!.IsChecked = real_eat_files;

        if (initial_perm_delete)
            this.FindControl<RadioButton>("eat_perm_radio")!.IsChecked = true;
        else
            this.FindControl<RadioButton>("eat_trash_radio")!.IsChecked = true;
    }

    private static bool has_window(Process p)
    {
        try { return !string.IsNullOrEmpty(p.MainWindowTitle); }
        catch { return false; }
    }

    private async void on_add_click(object? sender, RoutedEventArgs e)
    {
        var running = new List<string>();
        try
        {
            var procs = Process.GetProcesses();
            running = procs.Where(has_window).Select(p => p.ProcessName).Distinct().ToList();
            if (running.Count == 0) running = procs.Select(p => p.ProcessName).Distinct().ToList();
        }
        catch { }

        if (running.Count == 0) return;
        running.Sort(StringComparer.OrdinalIgnoreCase);

        var selector = new AppSelector(running);
        var res = await selector.ShowDialog<bool>(this);
        if (res && !string.IsNullOrEmpty(selector.selected_app))
        {
            if (!whitelist_items.Contains(selector.selected_app))
                whitelist_items.Add(selector.selected_app);
        }
    }

    private void on_remove_click(object? sender, RoutedEventArgs e)
    {
        var lst = this.FindControl<ListBox>("whitelist_box")!;
        if (lst.SelectedIndex != -1) whitelist_items.RemoveAt(lst.SelectedIndex);
    }

    private void on_ok_click(object? sender, RoutedEventArgs e)
    {
        is_listening_enabled = this.FindControl<CheckBox>("music_check")!.IsChecked ?? false;
        real_eat_files = this.FindControl<CheckBox>("eat_check")!.IsChecked ?? false;
        perm_delete = this.FindControl<RadioButton>("eat_perm_radio")!.IsChecked ?? false;
        language = this.FindControl<RadioButton>("lang_ru_radio")!.IsChecked == true ? "ru" : "en";
        music_whitelist.Clear();
        foreach (var item in whitelist_items) music_whitelist.Add(item);
        Close(true);
    }

    private void on_cancel_click(object? sender, RoutedEventArgs e) => Close(false);
}
