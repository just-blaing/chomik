using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace chomik;

public partial class MessageBox : Window
{
    private Point mouse_offset;
    private bool is_mouse_down;
    private DispatcherTimer? anim_timer;
    private List<animation_frame>? frames;
    private int cur_frame;
    private Image img_control;
    private const double corner_r = 12;

    public MessageBox()
    {
        InitializeComponent();
        img_control = this.FindControl<Image>("anim_image")!;
    }

    public MessageBox(string msg, List<animation_frame>? anims = null)
    {
        InitializeComponent();
        this.FindControl<TextBlock>("msg_text")!.Text = msg;
        img_control = this.FindControl<Image>("anim_image")!;
        frames = anims;

        if (frames is { Count: > 0 })
        {
            cur_frame = 0;
            img_control.Source = frames[0].image;
            anim_timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(frames[0].duration > 0 ? frames[0].duration : 100)
            };

            anim_timer.Tick += (s, e) =>
            {
                cur_frame = (cur_frame + 1) % frames.Count;
                img_control.Source = frames[cur_frame].image;
                var ms = frames[cur_frame].duration > 0 ? frames[cur_frame].duration : 100;
                var new_interval = TimeSpan.FromMilliseconds(ms);
                if (anim_timer.Interval != new_interval)
                {
                    anim_timer.Stop();
                    anim_timer.Interval = new_interval;
                    anim_timer.Start();
                }
            };

            anim_timer.Start();
        }
    }

    private bool in_rounded_rect(double x, double y)
    {
        double w = Bounds.Width, h = Bounds.Height, r = corner_r;
        if (x < 0 || y < 0 || x > w || y > h) return false;
        if (x < r && y < r)
            return Math.Sqrt(Math.Pow(x - r, 2) + Math.Pow(y - r, 2)) <= r;
        if (x > w - r && y < r)
            return Math.Sqrt(Math.Pow(x - (w - r), 2) + Math.Pow(y - r, 2)) <= r;
        if (x < r && y > h - r)
            return Math.Sqrt(Math.Pow(x - r, 2) + Math.Pow(y - (h - r), 2)) <= r;
        if (x > w - r && y > h - r)
            return Math.Sqrt(Math.Pow(x - (w - r), 2) + Math.Pow(y - (h - r), 2)) <= r;
        return true;
    }

    private void on_ok_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    private void on_pointer_pressed(object? sender, PointerPressedEventArgs e)
    {
        var pos = e.GetPosition(this);
        if (!in_rounded_rect(pos.X, pos.Y)) return;
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var screen_click = this.PointToScreen(pos);
            mouse_offset = new Point(this.Position.X - screen_click.X, this.Position.Y - screen_click.Y);
            is_mouse_down = true;
        }
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

    protected override void OnClosed(EventArgs e)
    {
        anim_timer?.Stop();
        base.OnClosed(e);
    }
}