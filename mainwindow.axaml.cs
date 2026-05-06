using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace chomik;

public partial class MainWindow : Window
{
    private Point mouse_offset;
    private bool is_mouse_down = false;
    private Dictionary<string, List<animation_frame>> loaded_animations = new();
    private Dictionary<Bitmap, byte[]> alpha_cache = new();
    private List<animation_frame> current_animation_frames = new();
    private int current_frame_index = 0;
    private DispatcherTimer? animation_timer;
    private Random rnd = new();
    private string current_animation_name = "AnimMainIdle";
    private int idle_loop_counter = 0;
    private int max_idle_loops = 1;
    private bool is_random_idle_sequence = false;
    private string current_idle_start = "";
    private string current_idle_loop = "";
    private string current_idle_finish = "";
    private bool is_spotify_music_playing = false;
    private string current_music_start = "AnimMusicStart";
    private string current_music_loop = "AnimMusicLoop";
    private string current_music_finish = "AnimMusicFinish";
    private DispatcherTimer? music_check_timer;
    private bool is_dragging_file = false;
    private bool is_character_dragging_animation = false;
    private List<string> one_off_random_idle_animations = new() { "AnimIdle1", "AnimIdle3", "AnimIdle4", "AnimIdle5", "AnimIdle6" };
    private HashSet<string> uninterruptible_animations = new();
    private bool is_screenshot_animation_active = false;
    private double idle_delay_seconds = 3.0;
    private bool is_music_listening_enabled = true;
    private List<string> music_whitelist = new();
    private DateTime last_user_activity_time = DateTime.Now;
    private DispatcherTimer? afk_check_timer;
    private bool is_in_afk_mode = false;
    private int afk_timeout_minutes = 3;
    private string afk_start_anim = "AnimIdleStart3";
    private string afk_loop_anim = "AnimIdleLoop3";
    private string afk_finish_anim = "AnimIdleFinish3";
    private bool real_eat_files = false;
    private bool permanent_delete = false;
    private bool is_write_mode_active = false;
    private string write_bubble_text = "";
    private BubbleWindow? bubble_window;
    private DispatcherTimer? bubble_follow_timer;
    private PixelPoint bubble_target;
    private const int wh_keyboard_ll = 13;
    private const int wm_keydown = 0x0100;
    private const int wm_syskeydown = 0x0104;

    [StructLayout(LayoutKind.Sequential)]
    private struct Kbdllhookstruct
    {
        public uint vk_code;
        public uint scan_code;
        public uint flags;
        public uint time;
        public IntPtr dw_extra_info;
    }

    private delegate IntPtr low_level_keyboard_proc(int n_code, IntPtr w_param, IntPtr l_param);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int n_index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int n_index, IntPtr new_long);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr prev_wnd_func, IntPtr hwnd, uint msg, IntPtr w_param, IntPtr l_param);

    [DllImport("user32.dll")]
    private static extern bool ScreenToClient(IntPtr hwnd, ref win32_point pt);

    [StructLayout(LayoutKind.Sequential)]
    private struct win32_point { public int x, y; }

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int id_hook, low_level_keyboard_proc lpfn, IntPtr h_mod, uint dw_thread_id);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int n_code, IntPtr w_param, IntPtr l_param);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lp_module_name);

    private delegate IntPtr wnd_proc_delegate(IntPtr hwnd, uint msg, IntPtr w_param, IntPtr l_param);

    private const int gwlp_wndproc = -4;
    private const int gwl_exstyle = -20;
    private const int ws_ex_toolwindow = 0x00000080;
    private const int ws_ex_appwindow = 0x00040000;
    private const int wm_nchittest = 0x0084;
    private const int httransparent = -1;
    private const int rgn_or = 2;

    [DllImport("gdi32.dll")] private static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);
    [DllImport("gdi32.dll")] private static extern int CombineRgn(IntPtr dest, IntPtr src1, IntPtr src2, int mode);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("user32.dll")] private static extern int SetWindowRgn(IntPtr hwnd, IntPtr hrgn, bool redraw);

    private wnd_proc_delegate? custom_wnd_proc_delegate;
    private IntPtr old_wnd_proc = IntPtr.Zero;

    private IntPtr hook_id = IntPtr.Zero;
    private low_level_keyboard_proc? proc;
    private DateTime last_key_press_time = DateTime.MinValue;
    private DateTime typing_session_start_time = DateTime.MinValue;
    private DispatcherTimer? typing_check_timer;
    private int typing_duration_threshold_ms = 2000;
    private bool is_typing_animation_active = false;
    private DispatcherTimer? idle_delay_timer;
    private Image hamster_img;
    private IntPtr x11_display = IntPtr.Zero;
    private byte[] x11_prev_keys = new byte[32];
    private DispatcherTimer? x11_key_timer;

    public MainWindow()
    {
        InitializeComponent();
        hamster_img = this.FindControl<Image>("hamster_image")!;
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, on_drag_enter);
        AddHandler(DragDrop.DropEvent, on_drop);
        AddHandler(ContextRequestedEvent, on_context_requested, handledEventsToo: false);
        load_settings();
        populate_uninterruptible_animations();
        preload_animations();
        load_initial_animation();
        load_menu_icons();

        animation_timer = new DispatcherTimer();
        animation_timer.Tick += animation_timer_tick;
        if (current_animation_frames.Count > 0 && current_animation_frames[0].image != null)
        {
            hamster_img.Source = current_animation_frames[0].image;
            update_window_region(current_animation_frames[0].image);
            animation_timer.Interval = TimeSpan.FromMilliseconds(current_animation_frames[0].duration > 0 ? current_animation_frames[0].duration : 100);
            animation_timer.Start();
        }

        music_check_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(3000) };
        music_check_timer.Tick += music_check_timer_tick;
        if (is_music_listening_enabled)
        {
            music_check_timer.Start();
            _ = check_music_state_async();
        }

        typing_check_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        typing_check_timer.Tick += typing_check_timer_tick;
        typing_check_timer.Start();

        idle_delay_timer = new DispatcherTimer();
        idle_delay_timer.Tick += idle_delay_timer_tick;

        afk_check_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10000) };
        afk_check_timer.Tick += afk_check_timer_tick;
        afk_check_timer.Start();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            proc = hook_callback;
            hook_id = set_hook(proc);
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        var hwnd = this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero) return;

        var ex = GetWindowLongPtr(hwnd, gwl_exstyle).ToInt64();
        ex = (ex | ws_ex_toolwindow) & ~ws_ex_appwindow;
        SetWindowLongPtr(hwnd, gwl_exstyle, new IntPtr(ex));

        custom_wnd_proc_delegate = wnd_proc;
        old_wnd_proc = SetWindowLongPtr(hwnd, gwlp_wndproc, Marshal.GetFunctionPointerForDelegate(custom_wnd_proc_delegate));
    }

    private IntPtr wnd_proc(IntPtr hwnd, uint msg, IntPtr w_param, IntPtr l_param)
    {
        if (msg == wm_nchittest)
        {
            try
            {
                int sx = (short)(l_param.ToInt64() & 0xFFFF);
                int sy = (short)((l_param.ToInt64() >> 16) & 0xFFFF);
                var pt = new win32_point { x = sx, y = sy };
                ScreenToClient(hwnd, ref pt);
                var frame = current_animation_frames.Count > 0
                    ? current_animation_frames[current_frame_index < current_animation_frames.Count ? current_frame_index : 0]
                    : null;
                if (frame?.image == null || !is_pixel_opaque(frame.image, pt.x, pt.y))
                    return new IntPtr(httransparent);
            }
            catch
            {
                return new IntPtr(httransparent);
            }
        }
        return CallWindowProc(old_wnd_proc, hwnd, msg, w_param, l_param);
    }

    private void load_menu_icons()
    {
        string base_dir = AppDomain.CurrentDomain.BaseDirectory;
        var icon_map = new Dictionary<string, string>
        {
            { "icon_exit", "icon1.ico" },
            { "icon_donate", "icon_2.ico" },
            { "icon_settings", "icon3.ico" },
            { "icon_screenshot", "icon4.ico" }
        };
        foreach (var kv in icon_map)
        {
            try
            {
                string path = Path.Combine(base_dir, "files", kv.Value);
                if (!File.Exists(path)) path = Path.Combine(base_dir, kv.Value);
                if (!File.Exists(path)) continue;
                var img = this.FindControl<Image>(kv.Key);
                if (img == null) continue;
                using var stream = File.OpenRead(path);
                img.Source = new Bitmap(stream);
            }
            catch { }
        }
    }

    private void load_settings()
    {
        string settings_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.txt");
        if (!File.Exists(settings_path)) return;
        foreach (var line in File.ReadAllLines(settings_path))
        {
            var parts = line.Split('=');
            if (parts.Length != 2) continue;
            if (parts[0] == "is_music_listening_enabled" && bool.TryParse(parts[1], out bool m)) is_music_listening_enabled = m;
            if (parts[0] == "is_music_listening_enabled" && bool.TryParse(parts[1], out bool b)) is_music_listening_enabled = b;
            if (parts[0] == "real_eat_files" && bool.TryParse(parts[1], out bool r)) real_eat_files = r;
            if (parts[0] == "permanent_delete" && bool.TryParse(parts[1], out bool pd)) permanent_delete = pd;
            if (parts[0] == "music_whitelist")
            {
                music_whitelist.Clear();
                if (!string.IsNullOrWhiteSpace(parts[1])) music_whitelist.AddRange(parts[1].Split(';'));
            }
        }
    }

    private void save_settings()
    {
        string settings_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.txt");
        File.WriteAllLines(settings_path, new[]
        {
            $"is_music_listening_enabled={is_music_listening_enabled}",
            $"real_eat_files={real_eat_files}",
            $"permanent_delete={permanent_delete}",
            $"music_whitelist={string.Join(";", music_whitelist)}"
        });
    }

    private IntPtr set_hook(low_level_keyboard_proc p)
    {
        using var cur_process = Process.GetCurrentProcess();
        using var cur_module = cur_process.MainModule;
        if (cur_module?.ModuleName != null) return SetWindowsHookEx(wh_keyboard_ll, p, GetModuleHandle(cur_module.ModuleName), 0);
        return IntPtr.Zero;
    }

    private IntPtr hook_callback(int n_code, IntPtr w_param, IntPtr l_param)
    {
        if (n_code >= 0 && (w_param == (IntPtr)wm_keydown || w_param == (IntPtr)wm_syskeydown))
        {
            if (l_param != IntPtr.Zero)
            {
                var structure = Marshal.PtrToStructure(l_param, typeof(Kbdllhookstruct));
                if (structure is Kbdllhookstruct kb_struct)
                {
                    uint vk = kb_struct.vk_code;
                    if (vk >= 0x20 && vk <= 0xFE)
                    {
                        last_key_press_time = DateTime.Now;
                        update_user_activity();
                    }
                }
            }
        }
        return CallNextHookEx(hook_id, n_code, w_param, l_param);
    }

    private void update_user_activity()
    {
        last_user_activity_time = DateTime.Now;
        if (is_in_afk_mode) end_afk_animation();
    }

    private void afk_check_timer_tick(object? sender, EventArgs e)
    {
        if (is_in_afk_mode || is_character_dragging_animation || is_dragging_file || is_typing_animation_active || (is_spotify_music_playing && is_music_listening_enabled) || is_screenshot_animation_active || is_write_mode_active) return;
        if ((DateTime.Now - last_user_activity_time).TotalMinutes >= afk_timeout_minutes) start_afk_animation();
    }

    private void start_afk_animation()
    {
        if (is_in_afk_mode || !loaded_animations.ContainsKey(afk_start_anim)) return;
        is_in_afk_mode = true;
        idle_delay_timer?.Stop();
        animation_timer?.Stop();
        load_animation(afk_start_anim);
        current_animation_name = afk_start_anim;
    }

    private void end_afk_animation()
    {
        if (!is_in_afk_mode) return;
        is_in_afk_mode = false;
        if ((current_animation_name == afk_start_anim || current_animation_name == afk_loop_anim) && loaded_animations.ContainsKey(afk_finish_anim))
        {
            animation_timer?.Stop();
            load_animation(afk_finish_anim);
            current_animation_name = afk_finish_anim;
        }
        else handle_animation_finish();
    }

    private void typing_check_timer_tick(object? sender, EventArgs e)
    {
        if (is_in_afk_mode || is_character_dragging_animation || is_dragging_file || (is_spotify_music_playing && is_music_listening_enabled) || is_screenshot_animation_active || is_write_mode_active) return;
        bool is_user_typing = (DateTime.Now - last_key_press_time).TotalMilliseconds < typing_duration_threshold_ms;
        if (is_user_typing)
        {
            if (!is_typing_animation_active)
            {
                if (typing_session_start_time == DateTime.MinValue) typing_session_start_time = DateTime.Now;
                if ((DateTime.Now - typing_session_start_time).TotalMilliseconds >= typing_duration_threshold_ms)
                {
                    idle_delay_timer?.Stop();
                    animation_timer?.Stop();
                    if (loaded_animations.ContainsKey("AnimTypingStart"))
                    {
                        load_animation("AnimTypingStart");
                        current_animation_name = "AnimTypingStart";
                    }
                    else if (loaded_animations.ContainsKey("AnimTyping"))
                    {
                        load_animation("AnimTyping");
                        current_animation_name = "AnimTyping";
                    }
                    is_typing_animation_active = true;
                }
            }
            else
            {
                if (current_animation_name == "AnimTypingStart" && current_frame_index >= current_animation_frames.Count - 1 && loaded_animations.ContainsKey("AnimTyping"))
                {
                    animation_timer?.Stop();
                    load_animation("AnimTyping");
                    current_animation_name = "AnimTyping";
                }
            }
        }
        else
        {
            if (is_typing_animation_active && current_animation_name != "AnimTypingStop")
            {
                if (loaded_animations.ContainsKey("AnimTypingStop"))
                {
                    animation_timer?.Stop();
                    load_animation("AnimTypingStop");
                    current_animation_name = "AnimTypingStop";
                }
                else
                {
                    is_typing_animation_active = false;
                    handle_animation_finish();
                }
            }
            typing_session_start_time = DateTime.MinValue;
        }
    }

    private void populate_uninterruptible_animations()
    {
        uninterruptible_animations.Clear();
        uninterruptible_animations.Add("AnimIdleStart1");
        uninterruptible_animations.Add("AnimIdleStart2");
        uninterruptible_animations.Add("AnimIdleFinish1");
        uninterruptible_animations.Add("AnimIdleFinish2");
        foreach (var anim in one_off_random_idle_animations) uninterruptible_animations.Add(anim);
        uninterruptible_animations.Add("AnimTypingStart");
        uninterruptible_animations.Add("AnimTypingStop");
        uninterruptible_animations.Add(current_music_start);
        uninterruptible_animations.Add(current_music_finish);
        uninterruptible_animations.Add("AnimDragFileStart");
        uninterruptible_animations.Add("AnimDragFileFinish");
        uninterruptible_animations.Add("AnimCharacterMoveStart");
        uninterruptible_animations.Add("AnimCharacterMoveFinish");
        uninterruptible_animations.Add(afk_start_anim);
        uninterruptible_animations.Add(afk_finish_anim);
        uninterruptible_animations.Add("AnimScreenshotFinish");
    }

    private void on_exit_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Environment.Exit(0);

    private async void on_write_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var dlg = new WriteDialog();
        string? text = await dlg.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(text)) return;

        write_bubble_text = text;
        is_write_mode_active = true;
        idle_delay_timer?.Stop();
        animation_timer?.Stop();

        if (loaded_animations.ContainsKey("AnimTypingStart")) { load_animation("AnimTypingStart"); current_animation_name = "AnimTypingStart"; }
        else if (loaded_animations.ContainsKey("AnimTyping")) { load_animation("AnimTyping"); current_animation_name = "AnimTyping"; }
        else if (loaded_animations.ContainsKey("AnimTypingStop")) { load_animation("AnimTypingStop"); current_animation_name = "AnimTypingStop"; }
        else { is_write_mode_active = false; show_bubble(); }
    }

    private void show_bubble()
    {
        bubble_follow_timer?.Stop();
        bubble_window?.Close();
        var anchor = get_bubble_anchor();
        bubble_target = anchor;
        bubble_window = new BubbleWindow(write_bubble_text, anchor);
        bubble_window.Closed += (_, _) => { bubble_follow_timer?.Stop(); bubble_follow_timer = null; };
        bubble_window.Show(this);
        bubble_follow_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        bubble_follow_timer.Tick += bubble_follow_tick;
        bubble_follow_timer.Start();
    }

    private PixelPoint get_bubble_anchor()
    {
        var (cx, top_y) = get_hamster_visual_bounds();
        return new PixelPoint(this.Position.X + cx, this.Position.Y + top_y);
    }

    private void bubble_follow_tick(object? sender, EventArgs e)
    {
        if (bubble_window == null || !bubble_window.IsVisible) { bubble_follow_timer?.Stop(); return; }
        bubble_target = get_bubble_anchor();
        int bw = (int)bubble_window.Bounds.Width;
        int bh = (int)bubble_window.Bounds.Height;
        var target_pos = new PixelPoint(bubble_target.X - bw / 2, bubble_target.Y - bh - 6);
        var cur = bubble_window.Position;
        int dx = target_pos.X - cur.X;
        int dy = target_pos.Y - cur.Y;
        if (dx == 0 && dy == 0) return;
        int nx = cur.X + (int)(dx * 0.07);
        int ny = cur.Y + (int)(dy * 0.07);
        if (nx == cur.X && dx != 0) nx = cur.X + Math.Sign(dx);
        if (ny == cur.Y && dy != 0) ny = cur.Y + Math.Sign(dy);
        bubble_window.Position = new PixelPoint(nx, ny);
    }

    private (int cx, int top_y) get_hamster_visual_bounds()
    {
        var frame = current_animation_frames.Count > 0
            ? current_animation_frames[current_frame_index < current_animation_frames.Count ? current_frame_index : 0]
            : null;
        if (frame?.image == null) return ((int)hamster_img.Bounds.Width / 2, 0);

        int w = frame.image.PixelSize.Width;
        int h = frame.image.PixelSize.Height;
        var alpha = get_alpha_data(frame.image);

        int min_x = w, max_x = 0, min_y = h;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (alpha[y * w + x] > 10)
                {
                    if (x < min_x) min_x = x;
                    if (x > max_x) max_x = x;
                    if (y < min_y) min_y = y;
                }

        if (min_x > max_x) return (w / 2, 0);
        return ((min_x + max_x) / 2, min_y);
    }
    private void on_donate_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Process.Start(new ProcessStartInfo { FileName = "https://donatepay.ru/don/1493944", UseShellExecute = true });

    private async void on_settings_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        try
        {
            var settings_form = new Settings(is_music_listening_enabled, music_whitelist, real_eat_files, permanent_delete);
            var result = await settings_form.ShowDialog<bool>(this);
            if (result)
            {
                is_music_listening_enabled = settings_form.is_music_listening_enabled;
                real_eat_files = settings_form.real_eat_files;
                permanent_delete = settings_form.permanent_delete;
                music_whitelist.Clear();
                music_whitelist.AddRange(settings_form.music_whitelist);
                save_settings();

                if (is_music_listening_enabled && music_check_timer != null && !music_check_timer.IsEnabled)
                {
                    music_check_timer.Start();
                    _ = check_music_state_async();
                }
                else if (!is_music_listening_enabled && music_check_timer != null && music_check_timer.IsEnabled)
                {
                    music_check_timer.Stop();
                    if (is_spotify_music_playing)
                    {
                        is_spotify_music_playing = false;
                        if (current_animation_name == current_music_start || current_animation_name == current_music_loop) handle_animation_finish();
                    }
                }
            }
        }
        catch { }
    }

    private void on_screenshot_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (is_in_afk_mode || is_character_dragging_animation || is_dragging_file) return;
        is_screenshot_animation_active = true;
        idle_delay_timer?.Stop();
        animation_timer?.Stop();
        if (loaded_animations.ContainsKey("AnimScreenshotFinish"))
        {
            load_animation("AnimScreenshotFinish");
            current_animation_name = "AnimScreenshotFinish";
        }
    }

    private async void on_about_click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        List<animation_frame>? frames = loaded_animations.ContainsKey("AnimMusicLoop") ? loaded_animations["AnimMusicLoop"] : null;
        var box = new MessageBox("created with love❤\nauthor: blaing", frames);
        await box.ShowDialog(this);
    }

    private async Task take_screenshot()
    {
        try
        {
            this.Hide();
            await Task.Delay(200);

            string out_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "screenshot.png");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var tools = new[] { ("scrot", $"\"{out_path}\""), ("import", $"-window root \"{out_path}\""), ("gnome-screenshot", $"-f \"{out_path}\"") };
                foreach (var (tool, args) in tools)
                {
                    try
                    {
                        using var p = Process.Start(new ProcessStartInfo(tool, args) { UseShellExecute = false, CreateNoWindow = true });
                        p?.WaitForExit();
                        if (File.Exists(out_path)) break;
                    }
                    catch { }
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                using var p = Process.Start(new ProcessStartInfo("screencapture", $"\"{out_path}\"") { UseShellExecute = false, CreateNoWindow = true });
                p?.WaitForExit();
            }
            else
            {
                var screen = Screens.Primary;
                if (screen == null) return;
                var rect = screen.Bounds;
                var rtb = new RenderTargetBitmap(new PixelSize(rect.Width, rect.Height), new Vector(96, 96));
                rtb.Render(this);
                rtb.Save(out_path);
            }

            var top_level = TopLevel.GetTopLevel(this);
            if (top_level?.Clipboard != null && File.Exists(out_path))
            {
                var data = new DataObject();
                data.Set(DataFormats.Files, new[] { out_path });
                await top_level.Clipboard.SetDataObjectAsync(data);
            }
        }
        catch { }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => this.Show());
        }
    }

    private static string get_base_dir()
    {
        string exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";
        string exe_dir = string.IsNullOrEmpty(exe) ? "" : Path.GetDirectoryName(exe) ?? "";
        string app_dir = AppDomain.CurrentDomain.BaseDirectory;
        foreach (var dir in new[] { exe_dir, app_dir })
        {
            if (!string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "files", "anims.txt"))) return dir;
            if (!string.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "anims.txt"))) return dir;
        }
        return app_dir;
    }

    private void preload_animations()
    {
        loaded_animations.Clear();
        string log = Path.Combine(Path.GetTempPath(), "chomik_debug.txt");
        string base_dir = "";
        string anims_file = "";
        string ani_folder = "";
        try
        {
            base_dir = get_base_dir();
            anims_file = Path.Combine(base_dir, "files", "anims.txt");
            ani_folder = Path.Combine(base_dir, "files");
            if (!File.Exists(anims_file)) anims_file = Path.Combine(base_dir, "anims.txt");
            if (!Directory.Exists(ani_folder)) ani_folder = base_dir;

            File.WriteAllText(log,
                $"base_dir: {base_dir}\n" +
                $"anims_file: {anims_file}\n" +
                $"anims_exists: {File.Exists(anims_file)}\n" +
                $"ani_folder: {ani_folder}\n" +
                $"folder_exists: {Directory.Exists(ani_folder)}\n");

            if (!File.Exists(anims_file)) return;

            string[] lines = File.ReadAllLines(anims_file);
            File.AppendAllText(log, $"lines_count: {lines.Length}\nfirst_line: [{(lines.Length > 0 ? lines[0] : "")}]\n");

            string? current_section = null;
            List<animation_frame>? current_list = null;

            foreach (string line in lines)
            {
                string t = line.Trim();
                if (string.IsNullOrWhiteSpace(t) || t.StartsWith("//") || t.StartsWith("#")) continue;
                if (t.StartsWith("Anim"))
                {
                    if (current_section != null && current_list != null && current_list.Count > 0) loaded_animations[current_section] = current_list;
                    current_section = t;
                    current_list = new();
                }
                else if (current_section != null && current_list != null)
                {
                    if (int.TryParse(t, out _)) continue;
                    string[] parts = t.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && parts[0].EndsWith(".png", StringComparison.OrdinalIgnoreCase) && int.TryParse(parts[1], out int duration))
                    {
                        string f_path = Path.Combine(ani_folder, parts[0]);
                        if (File.Exists(f_path))
                        {
                            try
                            {
                                using var stream = new FileStream(f_path, FileMode.Open, FileAccess.Read);
                                current_list.Add(new animation_frame(new Bitmap(stream), duration));
                            }
                            catch (Exception ex)
                            {
                                File.AppendAllText(log, $"bitmap_error [{parts[0]}]: {ex.Message}\n");
                            }
                        }
                        else
                        {
                            File.AppendAllText(log, $"file_not_found: {f_path}\n");
                        }
                    }
                }
            }
            if (current_section != null && current_list != null && current_list.Count > 0) loaded_animations[current_section] = current_list;
            File.AppendAllText(log, $"loaded_animations_count: {loaded_animations.Count}\n");
        }
        catch (Exception ex)
        {
            try { File.AppendAllText(log, $"EXCEPTION: {ex}\n"); } catch { }
        }
    }

    private async void music_check_timer_tick(object? sender, EventArgs e) => await check_music_state_async();

    private async Task check_music_state_async()
    {
        if (!is_music_listening_enabled)
        {
            if (is_spotify_music_playing)
            {
                is_spotify_music_playing = false;
                if (current_animation_name == current_music_start || current_animation_name == current_music_loop) handle_animation_finish();
            }
            return;
        }

        bool prev_state = is_spotify_music_playing;
        bool curr_state = await detect_music_playing_async();
        is_spotify_music_playing = curr_state;

        if (is_spotify_music_playing != prev_state && !is_in_afk_mode && !is_character_dragging_animation && !is_dragging_file && !is_typing_animation_active && !is_screenshot_animation_active)
        {
            idle_delay_timer?.Stop();
            animation_timer?.Stop();
            if (is_spotify_music_playing)
            {
                if (loaded_animations.ContainsKey(current_music_start)) { load_animation(current_music_start); current_animation_name = current_music_start; }
                else if (loaded_animations.ContainsKey(current_music_loop)) { load_animation(current_music_loop); current_animation_name = current_music_loop; }
            }
            else
            {
                if (current_animation_name == current_music_start || current_animation_name == current_music_loop)
                {
                    if (loaded_animations.ContainsKey(current_music_finish)) { load_animation(current_music_finish); current_animation_name = current_music_finish; }
                    else handle_animation_finish();
                }
            }
        }
    }

    private Task<bool> detect_music_playing_async()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return Task.Run(() => detect_music_windows());
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return Task.Run(() => detect_music_linux());
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return Task.Run(() => detect_music_macos());
        return Task.FromResult(false);
    }

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(ref Guid rclsid, IntPtr punk_outer, uint dw_cls_ctx, ref Guid riid, out IntPtr pp_v);

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr reserved, uint dw_co_init);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    private static unsafe void** vtbl(IntPtr punk) => *(void***)punk;

    private static unsafe int com_release(IntPtr punk)
    {
        if (punk == IntPtr.Zero) return 0;
        return ((delegate* unmanaged<IntPtr, int>)vtbl(punk)[2])(punk);
    }

    private static unsafe int com_qi(IntPtr punk, ref Guid iid, out IntPtr result)
    {
        result = IntPtr.Zero;
        if (punk == IntPtr.Zero) return unchecked((int)0x80004003);
        fixed (Guid* p_iid = &iid)
        fixed (IntPtr* p_result = &result)
            return ((delegate* unmanaged<IntPtr, Guid*, IntPtr*, int>)vtbl(punk)[0])(punk, p_iid, p_result);
    }

    private static unsafe int imm_get_default_endpoint(IntPtr punk, int flow, int role, out IntPtr device)
    {
        device = IntPtr.Zero;
        fixed (IntPtr* p = &device)
            return ((delegate* unmanaged<IntPtr, int, int, IntPtr*, int>)vtbl(punk)[4])(punk, flow, role, p);
    }

    private static unsafe int imm_device_activate(IntPtr punk, ref Guid iid, uint ctx, out IntPtr result)
    {
        result = IntPtr.Zero;
        fixed (Guid* p_iid = &iid)
        fixed (IntPtr* p_result = &result)
            return ((delegate* unmanaged<IntPtr, Guid*, uint, IntPtr, IntPtr*, int>)vtbl(punk)[3])(punk, p_iid, ctx, IntPtr.Zero, p_result);
    }

    private static unsafe int asm2_get_enumerator(IntPtr punk, out IntPtr sessions)
    {
        sessions = IntPtr.Zero;
        fixed (IntPtr* p = &sessions)
            return ((delegate* unmanaged<IntPtr, IntPtr*, int>)vtbl(punk)[5])(punk, p);
    }

    private static unsafe int ase_get_count(IntPtr punk, out int count)
    {
        count = 0;
        fixed (int* p = &count)
            return ((delegate* unmanaged<IntPtr, int*, int>)vtbl(punk)[3])(punk, p);
    }

    private static unsafe int ase_get_session(IntPtr punk, int index, out IntPtr session)
    {
        session = IntPtr.Zero;
        fixed (IntPtr* p = &session)
            return ((delegate* unmanaged<IntPtr, int, IntPtr*, int>)vtbl(punk)[4])(punk, index, p);
    }

    private static unsafe int asc_get_state(IntPtr punk, out int state)
    {
        state = 0;
        fixed (int* p = &state)
            return ((delegate* unmanaged<IntPtr, int*, int>)vtbl(punk)[3])(punk, p);
    }

    private static unsafe int asc2_get_pid(IntPtr punk, out uint pid)
    {
        pid = 0;
        fixed (uint* p = &pid)
            return ((delegate* unmanaged<IntPtr, uint*, int>)vtbl(punk)[14])(punk, p);
    }

    private bool detect_music_windows()
    {
        int co_hr = CoInitializeEx(IntPtr.Zero, 0x0);
        bool co_inited = co_hr == 0 || co_hr == 1;

        var clsid_mm = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
        var iid_mm = new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6");
        var iid_asm2 = new Guid("BFA971F1-4D5E-40BB-935E-967039BFBEE4");
        var iid_asc2 = new Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d");
        IntPtr enumerator = IntPtr.Zero, device = IntPtr.Zero, mgr = IntPtr.Zero, sessions = IntPtr.Zero;
        try
        {
            if (CoCreateInstance(ref clsid_mm, IntPtr.Zero, 1, ref iid_mm, out enumerator) != 0 || enumerator == IntPtr.Zero)
                return false;

            if (imm_get_default_endpoint(enumerator, 0, 1, out device) != 0 || device == IntPtr.Zero)
                return false;

            if (imm_device_activate(device, ref iid_asm2, 23, out mgr) != 0 || mgr == IntPtr.Zero)
                return false;

            if (asm2_get_enumerator(mgr, out sessions) != 0 || sessions == IntPtr.Zero)
                return false;

            ase_get_count(sessions, out int count);

            for (int i = 0; i < count; i++)
            {
                IntPtr sess = IntPtr.Zero, sess2 = IntPtr.Zero;
                try
                {
                    if (ase_get_session(sessions, i, out sess) != 0 || sess == IntPtr.Zero) continue;
                    asc_get_state(sess, out int state);
                    if (state != 1) continue;

                    if (music_whitelist.Count > 0)
                    {
                        if (com_qi(sess, ref iid_asc2, out sess2) != 0 || sess2 == IntPtr.Zero) continue;
                        if (asc2_get_pid(sess2, out uint pid) != 0 || pid == 0) continue;
                        try
                        {
                            var p = Process.GetProcessById((int)pid);
                            if (music_whitelist.Any(w => p.ProcessName.Contains(w, StringComparison.OrdinalIgnoreCase)))
                                return true;
                        }
                        catch { }
                    }
                    else
                    {
                        if (com_qi(sess, ref iid_asc2, out sess2) != 0 || sess2 == IntPtr.Zero) continue;
                        if (asc2_get_pid(sess2, out uint pid) != 0) continue;
                        try
                        {
                            var p = Process.GetProcessById((int)pid);
                            if (p.Id != Process.GetCurrentProcess().Id) return true;
                        }
                        catch { }
                    }
                }
                catch { }
                finally { com_release(sess2); com_release(sess); }
            }
        }
        catch { }
        finally
        {
            com_release(sessions);
            com_release(mgr);
            com_release(device);
            com_release(enumerator);
            if (co_inited) CoUninitialize();
        }
        return false;
    }

    private bool detect_music_linux()
    {
        bool found = false;

        try
        {
            var psi = new ProcessStartInfo("playerctl", "status")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p != null)
            {
                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();
                if (output == "Playing")
                {
                    if (music_whitelist.Count == 0) return true;
                    var player_psi = new ProcessStartInfo("playerctl", "metadata --format '{{playerName}}'")
                    {
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var pp = Process.Start(player_psi);
                    if (pp != null)
                    {
                        string player = pp.StandardOutput.ReadToEnd().Trim();
                        pp.WaitForExit();
                        if (music_whitelist.Any(app => player.Contains(app, StringComparison.OrdinalIgnoreCase)))
                            return true;
                    }
                    found = true;
                }
            }
        }
        catch { }

        try
        {
            var psi = new ProcessStartInfo("pactl", "list sink-inputs")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return found;
            string raw = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            var blocks = raw.Split("Sink Input #", StringSplitOptions.RemoveEmptyEntries);
            foreach (var block in blocks)
            {
                if (!block.Contains("Corked: no")) continue;
                if (music_whitelist.Count == 0) return true;
                if (music_whitelist.Any(app => block.Contains(app, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
        }
        catch { }

        return found;
    }

    private bool detect_music_macos()
    {
        try
        {
            string script = music_whitelist.Count == 0
                ? "tell application \"Spotify\" to if player state is playing then return \"yes\""
                : string.Join(" ", music_whitelist.Select(app => $"try\ntell application \"{app}\" to if player state is playing then return \"yes\"\nend try"));

            var psi = new ProcessStartInfo("osascript", $"-e '{script}'")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return false;
            string output = p.StandardOutput.ReadToEnd().Trim();
            p.WaitForExit();
            return output.Contains("yes", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    private void load_initial_animation()
    {
        is_random_idle_sequence = false;
        idle_loop_counter = 0;
        current_idle_start = ""; current_idle_loop = ""; current_idle_finish = "";
        is_typing_animation_active = false;
        last_key_press_time = DateTime.MinValue;
        typing_session_start_time = DateTime.MinValue;
        is_dragging_file = false;
        is_character_dragging_animation = false;
        is_in_afk_mode = false;
        is_screenshot_animation_active = false;
        idle_delay_timer?.Stop();

        _ = check_music_state_async().ContinueWith(t => Dispatcher.UIThread.Invoke(() =>
        {
            if (is_spotify_music_playing && is_music_listening_enabled)
            {
                if (loaded_animations.ContainsKey(current_music_loop)) { load_animation(current_music_loop); current_animation_name = current_music_loop; }
                else if (loaded_animations.ContainsKey(current_music_start)) { load_animation(current_music_start); current_animation_name = current_music_start; }
                else { load_animation("AnimMainIdle"); current_animation_name = "AnimMainIdle"; start_idle_delay(); }
            }
            else { load_animation("AnimMainIdle"); current_animation_name = "AnimMainIdle"; start_idle_delay(); }
        }));
    }

    private void load_animation(string anim_name)
    {
        if (string.IsNullOrEmpty(anim_name)) anim_name = "AnimMainIdle";
        is_character_dragging_animation = anim_name == "AnimCharacterMoveStart" || anim_name == "AnimCharacterMoving" || anim_name == "AnimCharacterMoveFinish";
        is_screenshot_animation_active = anim_name == "AnimScreenshotFinish";

        if (loaded_animations.TryGetValue(anim_name, out var frames) && frames.Count > 0)
        {
            current_animation_frames = frames;
            current_frame_index = 0;
            hamster_img.Source = current_animation_frames[0].image;
            update_window_region(current_animation_frames[0].image);
            if (animation_timer != null)
            {
                animation_timer.Stop();
                animation_timer.Interval = TimeSpan.FromMilliseconds(current_animation_frames[0].duration > 0 ? current_animation_frames[0].duration : 100);
                animation_timer.Start();
            }
        }
        else
        {
            if (anim_name != "AnimMainIdle") { current_animation_name = "AnimMainIdle"; load_animation("AnimMainIdle"); start_idle_delay(); }
            else
            {
                var box = new MessageBox("папка files/ не найдена\nили anims.txt пустой");
                _ = box.ShowDialog(this);
            }
        }
    }

    private void animation_timer_tick(object? sender, EventArgs e)
    {
        if (current_animation_frames.Count == 0 || animation_timer == null)
        {
            animation_timer?.Stop();
            if (current_animation_name != "AnimMainIdle") { load_animation("AnimMainIdle"); current_animation_name = "AnimMainIdle"; start_idle_delay(); }
            return;
        }

        current_frame_index++;

        if (current_animation_name == "AnimCharacterMoving" && is_mouse_down)
        {
            if (current_frame_index >= current_animation_frames.Count) current_frame_index = 0;
        }
        else if (current_animation_name == current_music_loop && is_spotify_music_playing && is_music_listening_enabled && !is_in_afk_mode)
        {
            if (current_frame_index >= current_animation_frames.Count) current_frame_index = 0;
        }
        else if (current_animation_name == "AnimTyping" && is_typing_animation_active && !is_in_afk_mode && !is_write_mode_active)
        {
            if (current_frame_index >= current_animation_frames.Count) current_frame_index = 0;
        }
        else if (current_animation_name == "AnimDragFileProcessing" && is_dragging_file && !is_in_afk_mode)
        {
            if (current_frame_index >= current_animation_frames.Count) current_frame_index = 0;
        }
        else if (current_animation_name == afk_loop_anim && is_in_afk_mode)
        {
            if (current_frame_index >= current_animation_frames.Count) current_frame_index = 0;
        }
        else if (is_random_idle_sequence && !is_in_afk_mode && current_animation_name == current_idle_loop && idle_loop_counter < max_idle_loops)
        {
            if (current_frame_index >= current_animation_frames.Count) { current_frame_index = 0; idle_loop_counter++; }
        }

        if (current_frame_index >= current_animation_frames.Count)
        {
            handle_animation_finish();
            return;
        }

        var frame = current_animation_frames[current_frame_index];
        hamster_img.Source = frame.image;
        update_window_region(frame.image);
        var new_interval = TimeSpan.FromMilliseconds(frame.duration > 0 ? frame.duration : 100);
        if (animation_timer.Interval != new_interval)
        {
            animation_timer.Stop();
            animation_timer.Interval = new_interval;
            animation_timer.Start();
        }
    }

    private void handle_animation_finish()
    {
        string prev_anim = current_animation_name;
        string next_anim = "AnimMainIdle";
        bool start_delay = false;
        animation_timer?.Stop();

        if (is_write_mode_active && (prev_anim == "AnimTypingStart" || prev_anim == "AnimTyping" || prev_anim == "AnimTypingStop"))
        {
            if (prev_anim == "AnimTypingStart") next_anim = "AnimTyping";
            else if (prev_anim == "AnimTyping") next_anim = "AnimTypingStop";
            else { is_write_mode_active = false; show_bubble(); start_delay = true; }
        }
        else if (prev_anim == "AnimScreenshotFinish")
        {
            if (is_screenshot_animation_active)
            {
                _ = take_screenshot();
                is_screenshot_animation_active = false;
                if (is_spotify_music_playing && is_music_listening_enabled && loaded_animations.ContainsKey(current_music_loop)) next_anim = current_music_loop;
                else if (is_typing_animation_active && loaded_animations.ContainsKey("AnimTyping")) next_anim = "AnimTyping";
                else start_delay = true;
            }
        }
        else if (is_in_afk_mode) { next_anim = afk_loop_anim; }
        else if (prev_anim == afk_finish_anim) { start_delay = true; }
        else if (prev_anim == "AnimCharacterMoveStart") { if (is_mouse_down && loaded_animations.ContainsKey("AnimCharacterMoving")) next_anim = "AnimCharacterMoving"; else if (!is_mouse_down && loaded_animations.ContainsKey("AnimCharacterMoveFinish")) next_anim = "AnimCharacterMoveFinish"; else start_delay = true; }
        else if (prev_anim == "AnimCharacterMoving") { if (!is_mouse_down && loaded_animations.ContainsKey("AnimCharacterMoveFinish")) next_anim = "AnimCharacterMoveFinish"; else if (is_mouse_down) next_anim = "AnimCharacterMoving"; else start_delay = true; }
        else if (prev_anim == "AnimCharacterMoveFinish") { start_delay = true; }
        else if (prev_anim == "AnimTypingStop") { is_typing_animation_active = false; typing_session_start_time = DateTime.MinValue; if (is_spotify_music_playing && is_music_listening_enabled && loaded_animations.ContainsKey(current_music_loop)) next_anim = current_music_loop; else if (is_dragging_file && loaded_animations.ContainsKey("AnimDragFileProcessing")) next_anim = "AnimDragFileProcessing"; else start_delay = true; }
        else if (prev_anim == "AnimTypingStart") { if (is_typing_animation_active && loaded_animations.ContainsKey("AnimTyping")) next_anim = "AnimTyping"; else if (loaded_animations.ContainsKey("AnimTypingStop")) next_anim = "AnimTypingStop"; else { is_typing_animation_active = false; start_delay = true; } }
        else if (prev_anim == "AnimDragFileStart") { if (is_dragging_file && loaded_animations.ContainsKey("AnimDragFileProcessing")) next_anim = "AnimDragFileProcessing"; else if (!is_dragging_file && loaded_animations.ContainsKey("AnimDragFileFinish")) next_anim = "AnimDragFileFinish"; else { is_dragging_file = false; start_delay = true; } }
        else if (prev_anim == "AnimDragFileProcessing" || prev_anim == "AnimDragFileFinish") { is_dragging_file = false; if (is_spotify_music_playing && is_music_listening_enabled && loaded_animations.ContainsKey(current_music_loop)) next_anim = current_music_loop; else if (is_typing_animation_active && loaded_animations.ContainsKey("AnimTyping")) next_anim = "AnimTyping"; else start_delay = true; }
        else if (prev_anim == current_music_start) { if (is_spotify_music_playing && is_music_listening_enabled && loaded_animations.ContainsKey(current_music_loop)) next_anim = current_music_loop; else if (loaded_animations.ContainsKey(current_music_finish)) next_anim = current_music_finish; else { is_spotify_music_playing = false; start_delay = true; } }
        else if (prev_anim == current_music_finish) { is_spotify_music_playing = false; if (is_dragging_file && loaded_animations.ContainsKey("AnimDragFileProcessing")) next_anim = "AnimDragFileProcessing"; else if (is_typing_animation_active && loaded_animations.ContainsKey("AnimTyping")) next_anim = "AnimTyping"; else start_delay = true; }
        else if (is_random_idle_sequence && prev_anim == current_idle_finish) { is_random_idle_sequence = false; current_idle_start = ""; current_idle_loop = ""; current_idle_finish = ""; idle_loop_counter = 0; if (is_spotify_music_playing && is_music_listening_enabled && loaded_animations.ContainsKey(current_music_loop)) next_anim = current_music_loop; else if (is_dragging_file && loaded_animations.ContainsKey("AnimDragFileProcessing")) next_anim = "AnimDragFileProcessing"; else if (is_typing_animation_active && loaded_animations.ContainsKey("AnimTyping")) next_anim = "AnimTyping"; else start_delay = true; }
        else if (is_random_idle_sequence && prev_anim == current_idle_start) { if (loaded_animations.ContainsKey(current_idle_loop)) { next_anim = current_idle_loop; idle_loop_counter = 0; } else if (loaded_animations.ContainsKey(current_idle_finish)) { next_anim = current_idle_finish; idle_loop_counter = 0; } else { is_random_idle_sequence = false; start_delay = true; } }
        else if (is_random_idle_sequence && prev_anim == current_idle_loop && idle_loop_counter >= max_idle_loops) { if (loaded_animations.ContainsKey(current_idle_finish)) next_anim = current_idle_finish; else { is_random_idle_sequence = false; start_delay = true; } }
        else if (one_off_random_idle_animations.Contains(prev_anim)) { if (is_spotify_music_playing && is_music_listening_enabled && loaded_animations.ContainsKey(current_music_loop)) next_anim = current_music_loop; else if (is_dragging_file && loaded_animations.ContainsKey("AnimDragFileProcessing")) next_anim = "AnimDragFileProcessing"; else if (is_typing_animation_active && loaded_animations.ContainsKey("AnimTyping")) next_anim = "AnimTyping"; else start_delay = true; }
        else { if (is_spotify_music_playing && is_music_listening_enabled && loaded_animations.ContainsKey(current_music_loop)) next_anim = current_music_loop; else if (is_dragging_file && loaded_animations.ContainsKey("AnimDragFileProcessing")) next_anim = "AnimDragFileProcessing"; else if (is_typing_animation_active && loaded_animations.ContainsKey("AnimTyping")) next_anim = "AnimTyping"; else if (is_random_idle_sequence) { is_random_idle_sequence = false; start_delay = true; } else start_delay = true; }

        if (start_delay && !is_in_afk_mode && !is_screenshot_animation_active)
        {
            animation_timer?.Stop();
            current_animation_name = "AnimMainIdle";
            if (loaded_animations.TryGetValue("AnimMainIdle", out var idle_frames) && idle_frames.Count > 0)
            {
                current_animation_frames = idle_frames;
                current_frame_index = 0;
                hamster_img.Source = idle_frames[0].image;
                update_window_region(idle_frames[0].image);
            }
            start_idle_delay();
        }
        else { load_animation(next_anim); current_animation_name = next_anim; }

        is_character_dragging_animation = current_animation_name == "AnimCharacterMoveStart" || current_animation_name == "AnimCharacterMoving" || current_animation_name == "AnimCharacterMoveFinish";
        is_random_idle_sequence = !is_in_afk_mode && (current_animation_name.StartsWith("AnimIdleStart") || current_animation_name.StartsWith("AnimIdleLoop") || current_animation_name.StartsWith("AnimIdleFinish"));
        is_typing_animation_active = current_animation_name == "AnimTypingStart" || current_animation_name == "AnimTyping" || current_animation_name == "AnimTypingStop";
        is_screenshot_animation_active = current_animation_name == "AnimScreenshotFinish";
    }

    private void start_idle_delay()
    {
        if (is_in_afk_mode || is_character_dragging_animation || is_dragging_file || is_typing_animation_active || is_spotify_music_playing || is_screenshot_animation_active || is_write_mode_active) { idle_delay_timer?.Stop(); return; }
        if (idle_delay_timer != null) { idle_delay_timer.Stop(); double max_ms = Math.Max(1500, idle_delay_seconds * 1000); idle_delay_timer.Interval = TimeSpan.FromMilliseconds(1000 + rnd.NextDouble() * (max_ms - 1000)); idle_delay_timer.Start(); }
    }

    private void idle_delay_timer_tick(object? sender, EventArgs e)
    {
        idle_delay_timer?.Stop();
        if (is_in_afk_mode || is_character_dragging_animation || is_dragging_file || is_typing_animation_active || is_spotify_music_playing || is_screenshot_animation_active) return;
        string next_anim = "AnimMainIdle";

        if (rnd.Next(100) < 20 && one_off_random_idle_animations.Any(a => loaded_animations.ContainsKey(a)))
        {
            var avail = one_off_random_idle_animations.Where(a => loaded_animations.ContainsKey(a)).ToList();
            if (avail.Count > 0) next_anim = avail[rnd.Next(avail.Count)];
        }
        else if (rnd.Next(100) < 10)
        {
            var avail = loaded_animations.Keys.Where(k => k.StartsWith("AnimIdleStart") && k != afk_start_anim).ToList();
            if (avail.Count > 0)
            {
                current_idle_start = avail[rnd.Next(avail.Count)];
                if (int.TryParse(current_idle_start.Replace("AnimIdleStart", ""), out int num))
                {
                    current_idle_loop = $"AnimIdleLoop{num}";
                    current_idle_finish = $"AnimIdleFinish{num}";
                    if (loaded_animations.ContainsKey(current_idle_start)) { next_anim = current_idle_start; is_random_idle_sequence = true; idle_loop_counter = 0; }
                    else if (loaded_animations.ContainsKey(current_idle_loop)) { next_anim = current_idle_loop; is_random_idle_sequence = true; idle_loop_counter = 0; }
                    else if (loaded_animations.ContainsKey(current_idle_finish)) { next_anim = current_idle_finish; is_random_idle_sequence = true; idle_loop_counter = 0; }
                }
            }
        }
        animation_timer?.Stop();
        load_animation(next_anim);
        current_animation_name = next_anim;
        is_random_idle_sequence = !is_in_afk_mode && (current_animation_name.StartsWith("AnimIdleStart") || current_animation_name.StartsWith("AnimIdleLoop") || current_animation_name.StartsWith("AnimIdleFinish"));
        if (next_anim == "AnimMainIdle") start_idle_delay();
    }

    private unsafe byte[] get_alpha_data(Bitmap bmp)
    {
        if (alpha_cache.TryGetValue(bmp, out var cached)) return cached;
        int w = bmp.PixelSize.Width, h = bmp.PixelSize.Height;
        var buf = new byte[w * h];
        var pixels = new byte[w * h * 4];
        fixed (byte* p_pixels = pixels)
        {
            bmp.CopyPixels(new PixelRect(0, 0, w, h), (IntPtr)p_pixels, pixels.Length, w * 4);
        }
        for (int i = 0; i < w * h; i++) buf[i] = pixels[i * 4 + 3];
        alpha_cache[bmp] = buf;
        return buf;
    }

    private bool is_pixel_opaque(Bitmap bmp, int x, int y)
    {
        if (x < 0 || y < 0 || x >= bmp.PixelSize.Width || y >= bmp.PixelSize.Height) return false;
        return get_alpha_data(bmp)[y * bmp.PixelSize.Width + x] > 10;
    }

    private void update_window_region(Bitmap? bmp)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero) return;
        if (bmp == null) { SetWindowRgn(hwnd, IntPtr.Zero, false); return; }

        try
        {
            var alpha = get_alpha_data(bmp);
            int w = bmp.PixelSize.Width;
            int h = bmp.PixelSize.Height;
            var region = CreateRectRgn(0, 0, 0, 0);
            for (int y = 0; y < h; y++)
            {
                int x = 0;
                while (x < w)
                {
                    while (x < w && alpha[y * w + x] <= 10) x++;
                    if (x >= w) break;
                    int start = x;
                    while (x < w && alpha[y * w + x] > 10) x++;
                    var row = CreateRectRgn(start, y, x, y + 1);
                    CombineRgn(region, region, row, rgn_or);
                    DeleteObject(row);
                }
            }
            SetWindowRgn(hwnd, region, false);
        }
        catch { }
    }

    private void on_context_requested(object? sender, ContextRequestedEventArgs e)
    {
        if (e.TryGetPosition(this, out var pos))
        {
            var frame = current_animation_frames.Count > 0
                ? current_animation_frames[current_frame_index < current_animation_frames.Count ? current_frame_index : 0]
                : null;
            if (frame?.image == null || !is_pixel_opaque(frame.image, (int)pos.X, (int)pos.Y))
                e.Handled = true;
        }
    }

    private void on_pointer_pressed(object? sender, PointerPressedEventArgs e)
    {
        var local_pos = e.GetPosition(this);
        var frame = current_animation_frames.Count > 0 ? current_animation_frames[current_frame_index < current_animation_frames.Count ? current_frame_index : 0] : null;
        bool on_transparent = frame?.image == null || !is_pixel_opaque(frame.image, (int)local_pos.X, (int)local_pos.Y);

        if (on_transparent)
        {
            e.Handled = true;
            return;
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var screen_click = this.PointToScreen(local_pos);
            mouse_offset = new Point(this.Position.X - screen_click.X, this.Position.Y - screen_click.Y);
            is_mouse_down = true;
            update_user_activity();
            idle_delay_timer?.Stop();
            if (!is_in_afk_mode && !is_spotify_music_playing && !is_typing_animation_active && !is_dragging_file && !is_screenshot_animation_active && !is_write_mode_active && !uninterruptible_animations.Contains(current_animation_name) && !current_animation_name.StartsWith("AnimCharacterMove"))
            {
                animation_timer?.Stop();
                if (loaded_animations.ContainsKey("AnimCharacterMoveStart")) { load_animation("AnimCharacterMoveStart"); current_animation_name = "AnimCharacterMoveStart"; }
                else if (loaded_animations.ContainsKey("AnimCharacterMoving")) { load_animation("AnimCharacterMoving"); current_animation_name = "AnimCharacterMoving"; }
            }
        }
    }

    private void on_pointer_moved(object? sender, PointerEventArgs e)
    {
        if (is_mouse_down)
        {
            if (this.ContextMenu?.IsOpen == true)
                this.ContextMenu.Close();

            var screen_pos = this.PointToScreen(e.GetPosition(this));
            this.Position = new PixelPoint((int)(screen_pos.X + mouse_offset.X), (int)(screen_pos.Y + mouse_offset.Y));
            if (!is_in_afk_mode && !is_dragging_file && !is_spotify_music_playing && !is_typing_animation_active && !is_screenshot_animation_active && !is_write_mode_active && current_animation_name != "AnimCharacterMoveStart" && current_animation_name != "AnimCharacterMoving" && current_animation_name != "AnimCharacterMoveFinish" && loaded_animations.ContainsKey("AnimCharacterMoving"))
            {
                idle_delay_timer?.Stop();
                animation_timer?.Stop();
                load_animation("AnimCharacterMoving");
                current_animation_name = "AnimCharacterMoving";
            }
        }
    }

    private void on_pointer_released(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            is_mouse_down = false;
            update_user_activity();
            if (!is_in_afk_mode && is_character_dragging_animation)
            {
                if (loaded_animations.ContainsKey("AnimCharacterMoveFinish")) { animation_timer?.Stop(); load_animation("AnimCharacterMoveFinish"); current_animation_name = "AnimCharacterMoveFinish"; }
                else handle_animation_finish();
            }
        }
    }

    private void on_drag_enter(object? sender, DragEventArgs e)
    {
        if (is_in_afk_mode || is_dragging_file || is_screenshot_animation_active || is_write_mode_active) { e.DragEffects = DragDropEffects.None; return; }
        e.DragEffects = DragDropEffects.Copy;
        if (is_character_dragging_animation) is_mouse_down = false;
        if (!is_spotify_music_playing && !current_animation_name.StartsWith("AnimDragFile"))
        {
            is_dragging_file = true;
            idle_delay_timer?.Stop();
            animation_timer?.Stop();
            if (loaded_animations.ContainsKey("AnimDragFileStart")) { load_animation("AnimDragFileStart"); current_animation_name = "AnimDragFileStart"; }
            else if (loaded_animations.ContainsKey("AnimDragFileProcessing")) { load_animation("AnimDragFileProcessing"); current_animation_name = "AnimDragFileProcessing"; }
        }
    }

    private void on_drop(object? sender, DragEventArgs e)
    {
        if (is_dragging_file)
        {
            if (real_eat_files)
            {
                var files = e.Data.GetFiles();
                if (files != null)
                {
                    foreach (var f in files)
                    {
                        try
                        {
                            string path = f.Path.LocalPath;
                            if (permanent_delete)
                            {
                                if (File.Exists(path)) File.Delete(path);
                                else if (Directory.Exists(path)) Directory.Delete(path, true);
                            }
                            else
                            {
                                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                                    move_to_recycle_bin_windows(path);
                                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                                    move_to_trash_linux(path);
                                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                                    move_to_trash_macos(path);
                            }
                        }
                        catch { }
                    }
                }
            }

            if (loaded_animations.ContainsKey("AnimDragFileFinish")) { animation_timer?.Stop(); load_animation("AnimDragFileFinish"); current_animation_name = "AnimDragFileFinish"; }
            else handle_animation_finish();
        }
    }

    private void move_to_recycle_bin_windows(string path)
    {
        try
        {
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                path,
                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            else if (Directory.Exists(path)) Directory.Delete(path, true);
        }
    }

    private void move_to_trash_linux(string path)
    {
        try
        {
            var psi = new ProcessStartInfo("gio", $"trash \"{path}\"") { UseShellExecute = false, CreateNoWindow = true };
            var p = Process.Start(psi);
            p?.WaitForExit();
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            else if (Directory.Exists(path)) Directory.Delete(path, true);
        }
    }

    private void move_to_trash_macos(string path)
    {
        try
        {
            var psi = new ProcessStartInfo("osascript", $"-e 'tell application \"Finder\" to delete POSIX file \"{path}\"'") { UseShellExecute = false, CreateNoWindow = true };
            var p = Process.Start(psi);
            p?.WaitForExit();
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            else if (Directory.Exists(path)) Directory.Delete(path, true);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && hook_id != IntPtr.Zero)
            UnhookWindowsHookEx(hook_id);
        base.OnClosed(e);
    }
}