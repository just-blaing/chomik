using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace chomik;

public partial class BubbleWindow : Window
{
    private readonly DispatcherTimer close_timer;
    private readonly PixelPoint anchor;

    public BubbleWindow(string text, PixelPoint hamster_screen_anchor)
    {
        InitializeComponent();
        this.FindControl<TextBlock>("bubble_text")!.Text = text;
        anchor = hamster_screen_anchor;
        close_timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        close_timer.Tick += (_, _) => { close_timer.Stop(); Close(); };
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        int bw = (int)Bounds.Width;
        int bh = (int)Bounds.Height;
        Position = new PixelPoint(anchor.X - bw / 2, anchor.Y - bh - 6);
        close_timer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        close_timer.Stop();
        base.OnClosed(e);
    }
}
