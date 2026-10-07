using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;

namespace chomik;

public partial class MessageBox : drag_window
{
    private DispatcherTimer? anim_timer;
    private sprite_sheet? sheet;
    private sprite_view view;
    private int cur_frame;

    public MessageBox() : this(string.Empty) { }

    public MessageBox(string msg, sprite_sheet? anim = null)
    {
        InitializeComponent();
        this.FindControl<TextBlock>("msg_text")!.Text = msg;
        view = this.FindControl<sprite_view>("anim_view")!;
        sheet = anim;
        if (sheet == null || sheet.count == 0) return;

        var crop = sheet.union_bounds();
        view.configure(crop, 90.0 / crop.Height, 1.6);
        view.show(sheet, 0);
        anim_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(frame_ms()) };
        anim_timer.Tick += anim_tick;
        anim_timer.Start();
    }

    private int frame_ms()
    {
        int d = sheet!.durations[cur_frame];
        return d > 0 ? d : 100;
    }

    private void anim_tick(object? sender, EventArgs e)
    {
        cur_frame = (cur_frame + 1) % sheet!.count;
        view.show(sheet, cur_frame);
        var next = TimeSpan.FromMilliseconds(frame_ms());
        if (anim_timer!.Interval != next) anim_timer.Interval = next;
    }

    private void on_ok_click(object? sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        anim_timer?.Stop();
        base.OnClosed(e);
    }
}
