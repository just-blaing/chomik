using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace chomik;

public partial class BubbleWindow : Window
{
    private readonly DispatcherTimer close_timer;
    private PixelPoint anchor;

    public BubbleWindow(string text, PixelPoint chomik_anchor)
    {
        InitializeComponent();
        this.FindControl<TextBlock>("bubble_text")!.Text = text;
        anchor = chomik_anchor;
        close_timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
        close_timer.Tick += (_, _) => { close_timer.Stop(); Close(); };
    }

    public void move_to(PixelPoint new_anchor)
    {
        anchor = new_anchor;
        if (!IsVisible) return;
        double scale = RenderScaling;
        Position = new PixelPoint(anchor.X - (int)(Bounds.Width * scale / 2), anchor.Y - (int)(Bounds.Height * scale) - 6);
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        move_to(anchor);
        Dispatcher.UIThread.Post(() => move_to(anchor), DispatcherPriority.Loaded);
        close_timer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        close_timer.Stop();
        base.OnClosed(e);
    }
}
