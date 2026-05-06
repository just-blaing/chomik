using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace chomik;

public partial class Settings : Window
{
    private Point mouse_offset;
    private bool is_mouse_down;
    private bool is_initialized;
    public bool is_music_listening_enabled { get; private set; }
    public bool real_eat_files { get; private set; }
    public bool permanent_delete { get; private set; }
    public List<string> music_whitelist { get; private set; } = new();
    private ObservableCollection<string> whitelist_items = new();

    public Settings()
    {
        InitializeComponent();
    }

    public Settings(bool initial_music, List<string> initial_whitelist, bool initial_eat, bool initial_perm_delete)
    {
        InitializeComponent();
        is_music_listening_enabled = initial_music;
        real_eat_files = initial_eat;
        permanent_delete = initial_perm_delete;
        music_whitelist.AddRange(initial_whitelist);

        foreach (var app in music_whitelist) whitelist_items.Add(app);
        this.FindControl<ListBox>("whitelist_box")!.ItemsSource = whitelist_items;
        this.FindControl<CheckBox>("music_check")!.IsChecked = is_music_listening_enabled;
        this.FindControl<CheckBox>("eat_check")!.IsChecked = real_eat_files;

        if (initial_perm_delete)
            this.FindControl<RadioButton>("eat_perm_radio")!.IsChecked = true;
        else
            this.FindControl<RadioButton>("eat_trash_radio")!.IsChecked = true;

        is_initialized = true;
    }

    private async void on_add_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var running = new List<string>();
        try
        {
            var procs = Process.GetProcesses().Where(p => !string.IsNullOrEmpty(p.MainWindowTitle));
            foreach (var p in procs) running.Add(p.ProcessName);
        }
        catch { }

        running = running.Distinct().ToList();
        if (running.Count == 0) return;

        var selector = new AppSelector(running);
        var res = await selector.ShowDialog<bool>(this);
        if (res && !string.IsNullOrEmpty(selector.selected_app))
        {
            if (!whitelist_items.Contains(selector.selected_app))
                whitelist_items.Add(selector.selected_app);
        }
    }

    private void on_remove_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var lst = this.FindControl<ListBox>("whitelist_box")!;
        if (lst.SelectedIndex != -1) whitelist_items.RemoveAt(lst.SelectedIndex);
    }

    private void on_ok_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        is_music_listening_enabled = this.FindControl<CheckBox>("music_check")!.IsChecked ?? false;
        real_eat_files = this.FindControl<CheckBox>("eat_check")!.IsChecked ?? false;
        permanent_delete = this.FindControl<RadioButton>("eat_perm_radio")!.IsChecked ?? false;
        music_whitelist.Clear();
        foreach (var item in whitelist_items) music_whitelist.Add(item);
        Close(true);
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