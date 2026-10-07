using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace chomik;

public partial class MainWindow : Window
{
    private Point mouse_offset;
    private bool is_mouse_down = false;
    private anim_lib loaded_anim = new(string.Empty);
    private sprite_sheet? sheet;
    private int cur_frame_index = 0;
    private bool is_opened = false;
    private bool is_error_shown = false;
    private DispatcherTimer? animation_timer;
    private Random rnd = new();
    private string cur_animation_name = "AnimMainIdle";
    private int idle_loop_counter = 0;
    private int max_idle_loops = 1;
    private bool is_random_idle = false;
    private string cur_idle_start = "";
    private string cur_idle_loop = "";
    private string cur_idle_finish = "";
    private bool is_music_playing = false;
    private string cur_music_start = "AnimMusicStart";
    private string cur_music_loop = "AnimMusicLoop";
    private string cur_music_finish = "AnimMusicFinish";
    private DispatcherTimer? music_check_timer;
    private bool is_dragging_file = false;
    private bool is_chomik_dragging_animation = false;
    private List<string> one_off_random_idle_animations = new() { "AnimIdle1", "AnimIdle3", "AnimIdle4", "AnimIdle5", "AnimIdle6" };
    private HashSet<string> inf_animations = new();
    private bool is_screenshot_anim_active = false;
    private double idle_delay = 3.0;
    private bool is_listening_enabled = true;
    private List<string> music_whitelist = new();
    private DateTime last_user_activity_time = DateTime.Now;
    private DispatcherTimer? afk_check_timer;
    private bool is_in_afk_mode = false;
    private int afk_timeout = 3;
    private string afk_start = "AnimIdleStart3";
    private string afk_loop = "AnimIdleLoop3";
    private string afk_finish = "AnimIdleFinish3";
    private bool real_eat_files = false;
    private bool perm_delete = false;
    private bool write_mode_active = false;
    private string write_bubble_text = "";
    private BubbleWindow? bubble_window;
    private win.wnd_proc_delegate? custom_wnd_proc_delegate;
    private IntPtr old_wnd_proc = IntPtr.Zero;
    private IntPtr hook_id = IntPtr.Zero;
    private win.key_proc? proc;
    private DateTime last_key_press_time = DateTime.MinValue;
    private DateTime typing_session_start_time = DateTime.MinValue;
    private DispatcherTimer? typing_check_timer;
    private int typing_duration = 2000;
    private bool typing_animation_active = false;
    private DispatcherTimer? idle_delay_timer;
    private DispatcherTimer? top_timer;
    private DispatcherTimer? key_poll_timer;
    private static string data_dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "chomik");

    private static string settings_path => Path.Combine(data_dir, "settings.txt");

    public MainWindow()
    {
        InitializeComponent();
        Opacity = 0;
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, on_drag_enter);
        AddHandler(DragDrop.DropEvent, on_drop);
        AddHandler(ContextRequestedEvent, on_context_requested, handledEventsToo: false);
        PositionChanged += (_, _) => move_bubble();
        ScalingChanged += (_, _) => apply_region();

        load_settings();
        apply_language();
        populate_uninterruptible_animations();
        load_menu_icons();

        animation_timer = new DispatcherTimer();
        animation_timer.Tick += animation_timer_tick;

        idle_delay_timer = new DispatcherTimer();
        idle_delay_timer.Tick += idle_delay_timer_tick;

        music_check_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(3000) };
        music_check_timer.Tick += music_check_timer_tick;

        typing_check_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        typing_check_timer.Tick += typing_check_timer_tick;

        afk_check_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10000) };
        afk_check_timer.Tick += afk_check_timer_tick;

        top_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        top_timer.Tick += top_timer_tick;

        loaded_anim = new anim_lib(find_anims_path());
        load_initial_animation();

        typing_check_timer.Start();
        afk_check_timer.Start();
        if (is_listening_enabled)
        {
            music_check_timer.Start();
            _ = check_music_state_async();
        }

        if (OperatingSystem.IsWindows())
        {
            proc = hook_callback;
            hook_id = win.set_key_hook(proc);
        }
        else
        {
            key_poll_timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
            key_poll_timer.Tick += (_, _) =>
            {
                if (!plat.key_down()) return;
                last_key_press_time = DateTime.Now;
                update_user_activity();
            };
            key_poll_timer.Start();
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        is_opened = true;
        hook_window();
        apply_region();
        top_timer?.Start();
        DispatcherTimer.RunOnce(() => Opacity = 1, TimeSpan.FromMilliseconds(150));
    }

    private void top_timer_tick(object? sender, EventArgs e)
    {
        if (!is_opened || !OperatingSystem.IsWindows()) return;
        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd != IntPtr.Zero) win.keep_on_top(hwnd);
    }

    private void hook_window()
    {
        if (!OperatingSystem.IsWindows()) return;
        var hwnd = this.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero) return;

        var ex = win.GetWindowLongPtr(hwnd, win.gwl_exstyle).ToInt64();
        ex = (ex | win.ws_ex_toolwindow) & ~win.ws_ex_appwindow;
        win.SetWindowLongPtr(hwnd, win.gwl_exstyle, new IntPtr(ex));

        custom_wnd_proc_delegate = wnd_proc;
        old_wnd_proc = win.SetWindowLongPtr(hwnd, win.gwlp_wndproc, Marshal.GetFunctionPointerForDelegate(custom_wnd_proc_delegate));
    }

    private IntPtr wnd_proc(IntPtr hwnd, uint msg, IntPtr w_param, IntPtr l_param)
    {
        if (msg == win.wm_nchittest)
        {
            try
            {
                int sx = (short)(l_param.ToInt64() & 0xFFFF);
                int sy = (short)((l_param.ToInt64() >> 16) & 0xFFFF);
                var pt = new win.win32_point { x = sx, y = sy };
                win.ScreenToClient(hwnd, ref pt);
                double scale = RenderScaling;
                if (!is_opaque(pt.x / scale, pt.y / scale)) return new IntPtr(win.httransparent);
            }
            catch
            {
                return new IntPtr(win.httransparent);
            }
        }
        return win.CallWindowProc(old_wnd_proc, hwnd, msg, w_param, l_param);
    }

    private static string find_anims_path()
    {
        string base_dir = AppContext.BaseDirectory;
        string in_files = Path.Combine(base_dir, "files", "anims.txt");
        return File.Exists(in_files) ? in_files : Path.Combine(base_dir, "anims.txt");
    }

    private bool is_opaque(double x, double y)
    {
        if (sheet == null) return false;
        int i = cur_frame_index < sheet.count ? cur_frame_index : 0;
        return sheet.opaque(i, (int)Math.Floor(x), (int)Math.Floor(y));
    }

    private int frame_ms(int i)
    {
        if (sheet == null || i >= sheet.count) return 100;
        return sheet.durations[i] > 0 ? sheet.durations[i] : 100;
    }

    private void fit_window()
    {
        if (sheet == null) return;
        if (Width != sheet.frame_w) Width = sheet.frame_w;
        if (Height != sheet.frame_h) Height = sheet.frame_h;
    }

    private void show_frame(int i)
    {
        if (sheet == null) return;
        cur_frame_index = i;
        chomik_view.show(sheet, i);
        apply_region();
        move_bubble();
    }

    private void apply_region()
    {
        if (!is_opened || sheet == null) return;
        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd == IntPtr.Zero) return;
        try
        {
            if (OperatingSystem.IsWindows()) win.set_region(hwnd, sheet, cur_frame_index, RenderScaling);
            else if (OperatingSystem.IsLinux()) plat.set_shape(hwnd, sheet, cur_frame_index, RenderScaling);
        }
        catch { }
    }

    private void load_menu_icons()
    {
        string base_dir = AppDomain.CurrentDomain.BaseDirectory;
        var icon_map = new Dictionary<string, string>
        {
            { "icon_exit", "icon1.ico" },
            { "icon_donate", "icon_2.ico" },
            { "icon_settings", "icon3.ico" },
            { "icon_screenshot", "icon4.ico" },
            { "icon_write", "icon5.ico" }
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

    private void apply_language()
    {
        set_header("mi_write", "write");
        set_header("mi_screenshot", "screenshot");
        set_header("mi_exit", "exit");
        set_header("mi_donate", "donate");
        set_header("mi_settings", "settings");
    }

    private void set_header(string name, string key)
    {
        var item = this.FindControl<MenuItem>(name);
        if (item != null) item.Header = localization.t(key);
    }

    private void load_settings()
    {
        string path = settings_path;
        if (!File.Exists(path)) path = Path.Combine(AppContext.BaseDirectory, "settings.txt");
        if (!File.Exists(path)) return;
        foreach (var line in File.ReadAllLines(path))
        {
            int eq = line.IndexOf('=');
            if (eq < 0) continue;
            string key = line[..eq];
            string value = line[(eq + 1)..];
            if (key == "is_music_listening_enabled" && bool.TryParse(value, out bool listening)) is_listening_enabled = listening;
            else if (key == "real_eat_files" && bool.TryParse(value, out bool eat)) real_eat_files = eat;
            else if (key == "permanent_delete" && bool.TryParse(value, out bool perm)) perm_delete = perm;
            else if (key == "language") localization.use(value);
            else if (key == "music_whitelist")
            {
                music_whitelist.Clear();
                music_whitelist.AddRange(value.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }
        }
    }

    private void save_settings()
    {
        try
        {
            Directory.CreateDirectory(data_dir);
            File.WriteAllLines(settings_path, new[]
            {
                $"is_music_listening_enabled={is_listening_enabled}",
                $"real_eat_files={real_eat_files}",
                $"permanent_delete={perm_delete}",
                $"language={localization.lang}",
                $"music_whitelist={string.Join(";", music_whitelist)}"
            });
        }
        catch { }
    }

    private IntPtr hook_callback(int n_code, IntPtr w_param, IntPtr l_param)
    {
        if (n_code >= 0 && (w_param == (IntPtr)win.wm_keydown || w_param == (IntPtr)win.wm_syskeydown) && l_param != IntPtr.Zero)
        {
            var kb = Marshal.PtrToStructure<win.kbd_hook_struct>(l_param);
            if (kb.vk_code >= 0x20 && kb.vk_code <= 0xFE)
            {
                last_key_press_time = DateTime.Now;
                update_user_activity();
            }
        }
        return win.CallNextHookEx(hook_id, n_code, w_param, l_param);
    }

    private void update_user_activity()
    {
        last_user_activity_time = DateTime.Now;
        if (is_in_afk_mode) end_afk_animation();
    }

    private void afk_check_timer_tick(object? sender, EventArgs e)
    {
        if (is_in_afk_mode || is_chomik_dragging_animation || is_dragging_file || typing_animation_active || (is_music_playing && is_listening_enabled) || is_screenshot_anim_active || write_mode_active) return;
        if ((DateTime.Now - last_user_activity_time).TotalMinutes >= afk_timeout) start_afk_animation();
    }

    private void start_afk_animation()
    {
        if (is_in_afk_mode || !loaded_anim.ContainsKey(afk_start)) return;
        is_in_afk_mode = true;
        idle_delay_timer?.Stop();
        animation_timer?.Stop();
        load_animation(afk_start);
        cur_animation_name = afk_start;
    }

    private void end_afk_animation()
    {
        if (!is_in_afk_mode) return;
        is_in_afk_mode = false;
        if ((cur_animation_name == afk_start || cur_animation_name == afk_loop) && loaded_anim.ContainsKey(afk_finish))
        {
            animation_timer?.Stop();
            load_animation(afk_finish);
            cur_animation_name = afk_finish;
        }
        else handle_animation_finish();
    }

    private void typing_check_timer_tick(object? sender, EventArgs e)
    {
        if (is_in_afk_mode || is_chomik_dragging_animation || is_dragging_file || (is_music_playing && is_listening_enabled) || is_screenshot_anim_active || write_mode_active) return;
        bool is_user_typing = (DateTime.Now - last_key_press_time).TotalMilliseconds < typing_duration;
        if (is_user_typing)
        {
            if (!typing_animation_active)
            {
                if (typing_session_start_time == DateTime.MinValue) typing_session_start_time = DateTime.Now;
                if ((DateTime.Now - typing_session_start_time).TotalMilliseconds >= typing_duration)
                {
                    idle_delay_timer?.Stop();
                    animation_timer?.Stop();
                    if (loaded_anim.ContainsKey("AnimTypingStart"))
                    {
                        load_animation("AnimTypingStart");
                        cur_animation_name = "AnimTypingStart";
                    }
                    else if (loaded_anim.ContainsKey("AnimTyping"))
                    {
                        load_animation("AnimTyping");
                        cur_animation_name = "AnimTyping";
                    }
                    typing_animation_active = true;
                }
            }
            else
            {
                if (cur_animation_name == "AnimTypingStart" && sheet != null && cur_frame_index >= sheet.count - 1 && loaded_anim.ContainsKey("AnimTyping"))
                {
                    animation_timer?.Stop();
                    load_animation("AnimTyping");
                    cur_animation_name = "AnimTyping";
                }
            }
        }
        else
        {
            if (typing_animation_active && cur_animation_name != "AnimTypingStop")
            {
                if (loaded_anim.ContainsKey("AnimTypingStop"))
                {
                    animation_timer?.Stop();
                    load_animation("AnimTypingStop");
                    cur_animation_name = "AnimTypingStop";
                }
                else
                {
                    typing_animation_active = false;
                    handle_animation_finish();
                }
            }
            typing_session_start_time = DateTime.MinValue;
        }
    }

    private void populate_uninterruptible_animations()
    {
        inf_animations.Clear();
        inf_animations.Add("AnimIdleStart1");
        inf_animations.Add("AnimIdleStart2");
        inf_animations.Add("AnimIdleFinish1");
        inf_animations.Add("AnimIdleFinish2");
        foreach (var anim in one_off_random_idle_animations) inf_animations.Add(anim);
        inf_animations.Add("AnimTypingStart");
        inf_animations.Add("AnimTypingStop");
        inf_animations.Add(cur_music_start);
        inf_animations.Add(cur_music_finish);
        inf_animations.Add("AnimDragFileStart");
        inf_animations.Add("AnimDragFileFinish");
        inf_animations.Add("AnimCharacterMoveStart");
        inf_animations.Add("AnimCharacterMoveFinish");
        inf_animations.Add(afk_start);
        inf_animations.Add(afk_finish);
        inf_animations.Add("AnimScreenshotFinish");
    }

    private void on_exit_click(object? sender, RoutedEventArgs e) => Environment.Exit(0);

    private async void on_write_click(object? sender, RoutedEventArgs e)
    {
        var dlg = new WriteDialog();
        string? text = await dlg.ShowDialog<string?>(this);
        if (string.IsNullOrWhiteSpace(text)) return;

        write_bubble_text = text;
        write_mode_active = true;
        idle_delay_timer?.Stop();
        animation_timer?.Stop();

        if (loaded_anim.ContainsKey("AnimTypingStart")) { load_animation("AnimTypingStart"); cur_animation_name = "AnimTypingStart"; }
        else if (loaded_anim.ContainsKey("AnimTyping")) { load_animation("AnimTyping"); cur_animation_name = "AnimTyping"; }
        else if (loaded_anim.ContainsKey("AnimTypingStop")) { load_animation("AnimTypingStop"); cur_animation_name = "AnimTypingStop"; }
        else { write_mode_active = false; show_bubble(); }
    }

    private void show_bubble()
    {
        bubble_window?.Close();
        var created = new BubbleWindow(write_bubble_text, get_bubble_anchor());
        created.Closed += (_, _) => { if (bubble_window == created) bubble_window = null; };
        bubble_window = created;
        created.Show(this);
    }

    private void move_bubble() => bubble_window?.move_to(get_bubble_anchor());

    private PixelPoint get_bubble_anchor()
    {
        var (cx, top_y) = get_chomik_bounds();
        double scale = RenderScaling;
        return new PixelPoint(Position.X + (int)(cx * scale), Position.Y + (int)(top_y * scale));
    }

    private (int cx, int top_y) get_chomik_bounds()
    {
        if (sheet == null) return (0, 0);
        var b = sheet.bounds(cur_frame_index < sheet.count ? cur_frame_index : 0);
        return (b.X + b.Width / 2, b.Y);
    }

    private void on_donate_click(object? sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo { FileName = "https://www.donationalerts.com/r/not_blaing", UseShellExecute = true });

    private async void on_settings_click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var settings_form = new Settings(is_listening_enabled, music_whitelist, real_eat_files, perm_delete);
            var result = await settings_form.ShowDialog<bool>(this);
            if (result)
            {
                is_listening_enabled = settings_form.is_listening_enabled;
                real_eat_files = settings_form.real_eat_files;
                perm_delete = settings_form.perm_delete;
                music_whitelist.Clear();
                music_whitelist.AddRange(settings_form.music_whitelist);
                localization.use(settings_form.language);
                apply_language();
                save_settings();

                if (is_listening_enabled && music_check_timer != null && !music_check_timer.IsEnabled)
                {
                    music_check_timer.Start();
                    _ = check_music_state_async();
                }
                else if (!is_listening_enabled && music_check_timer != null && music_check_timer.IsEnabled)
                {
                    music_check_timer.Stop();
                    if (is_music_playing)
                    {
                        is_music_playing = false;
                        if (cur_animation_name == cur_music_start || cur_animation_name == cur_music_loop) handle_animation_finish();
                    }
                }
            }
        }
        catch { }
    }

    private void on_screenshot_click(object? sender, RoutedEventArgs e)
    {
        if (is_in_afk_mode || is_chomik_dragging_animation || is_dragging_file) return;
        is_screenshot_anim_active = true;
        idle_delay_timer?.Stop();
        animation_timer?.Stop();
        if (loaded_anim.ContainsKey("AnimScreenshotFinish"))
        {
            load_animation("AnimScreenshotFinish");
            cur_animation_name = "AnimScreenshotFinish";
        }
    }

    private async void on_about_click(object? sender, RoutedEventArgs e)
    {
        var box = new MessageBox("created with love❤\nauthor: blaing", loaded_anim.open_copy("AnimMusicLoop"));
        await box.ShowDialog(this);
    }

    private async Task take_screenshot()
    {
        Opacity = 0;
        try
        {
            await Task.Delay(250);
            Directory.CreateDirectory(data_dir);
            string out_path = Path.Combine(data_dir, "screenshot.png");
            if (File.Exists(out_path)) File.Delete(out_path);

            bool done = OperatingSystem.IsWindows() ? capture_windows(out_path) : await capture_external(out_path);
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (done && !OperatingSystem.IsWindows() && clipboard != null)
            {
                var data = new DataObject();
                data.Set(DataFormats.FileNames, new[] { out_path });
                await clipboard.SetDataObjectAsync(data);
            }
        }
        catch { }
        finally
        {
            Opacity = 1;
        }
    }

    private bool capture_windows(string out_path)
    {
        var screen = Screens.Primary;
        if (screen == null) return false;
        using var bmp = win.capture(screen.Bounds);
        if (bmp == null) return false;
        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        bool copied = win.copy_image(bmp, hwnd);
        bmp.Save(out_path);
        return copied;
    }

    private static async Task<bool> capture_external(string out_path)
    {
        var tools = OperatingSystem.IsMacOS()
            ? new[] { ("screencapture", $"-x \"{out_path}\"") }
            : new[]
            {
                ("scrot", $"\"{out_path}\""),
                ("maim", $"\"{out_path}\""),
                ("spectacle", $"-b -n -f -o \"{out_path}\""),
                ("grim", $"\"{out_path}\""),
                ("gnome-screenshot", $"-f \"{out_path}\""),
                ("import", $"-window root \"{out_path}\"")
            };
        foreach (var (tool, args) in tools)
        {
            try
            {
                using var p = Process.Start(new ProcessStartInfo(tool, args) { UseShellExecute = false, CreateNoWindow = true });
                if (p == null) continue;
                await p.WaitForExitAsync();
                if (File.Exists(out_path)) return true;
            }
            catch { }
        }
        return false;
    }

    private void load_initial_animation()
    {
        is_random_idle = false;
        idle_loop_counter = 0;
        cur_idle_start = ""; cur_idle_loop = ""; cur_idle_finish = "";
        typing_animation_active = false;
        last_key_press_time = DateTime.MinValue;
        typing_session_start_time = DateTime.MinValue;
        is_dragging_file = false;
        is_chomik_dragging_animation = false;
        is_in_afk_mode = false;
        is_screenshot_anim_active = false;
        idle_delay_timer?.Stop();
        load_animation("AnimMainIdle");
        cur_animation_name = "AnimMainIdle";
        start_idle_delay();
    }

    private void load_animation(string anim_name)
    {
        if (string.IsNullOrEmpty(anim_name)) anim_name = "AnimMainIdle";
        is_chomik_dragging_animation = anim_name == "AnimCharacterMoveStart" || anim_name == "AnimCharacterMoving" || anim_name == "AnimCharacterMoveFinish";
        is_screenshot_anim_active = anim_name == "AnimScreenshotFinish";

        var next = loaded_anim.get(anim_name);
        if (next != null && next.count > 0)
        {
            sheet = next;
            fit_window();
            show_frame(0);
            if (animation_timer != null)
            {
                animation_timer.Stop();
                animation_timer.Interval = TimeSpan.FromMilliseconds(frame_ms(0));
                animation_timer.Start();
            }
        }
        else
        {
            if (anim_name != "AnimMainIdle") { cur_animation_name = "AnimMainIdle"; load_animation("AnimMainIdle"); start_idle_delay(); }
            else if (!is_error_shown)
            {
                is_error_shown = true;
                Dispatcher.UIThread.Post(() => new MessageBox(localization.t("no_files")).Show());
            }
        }
    }

    private void animation_timer_tick(object? sender, EventArgs e)
    {
        if (sheet == null || sheet.count == 0 || animation_timer == null)
        {
            animation_timer?.Stop();
            if (cur_animation_name != "AnimMainIdle") { load_animation("AnimMainIdle"); cur_animation_name = "AnimMainIdle"; start_idle_delay(); }
            return;
        }

        cur_frame_index++;

        if (cur_animation_name == "AnimCharacterMoving" && is_mouse_down)
        {
            if (cur_frame_index >= sheet.count) cur_frame_index = 0;
        }
        else if (cur_animation_name == cur_music_loop && is_music_playing && is_listening_enabled && !is_in_afk_mode)
        {
            if (cur_frame_index >= sheet.count) cur_frame_index = 0;
        }
        else if (cur_animation_name == "AnimTyping" && typing_animation_active && !is_in_afk_mode && !write_mode_active)
        {
            if (cur_frame_index >= sheet.count) cur_frame_index = 0;
        }
        else if (cur_animation_name == "AnimDragFileProcessing" && is_dragging_file && !is_in_afk_mode)
        {
            if (cur_frame_index >= sheet.count) cur_frame_index = 0;
        }
        else if (cur_animation_name == afk_loop && is_in_afk_mode)
        {
            if (cur_frame_index >= sheet.count) cur_frame_index = 0;
        }
        else if (is_random_idle && !is_in_afk_mode && cur_animation_name == cur_idle_loop && idle_loop_counter < max_idle_loops)
        {
            if (cur_frame_index >= sheet.count) { cur_frame_index = 0; idle_loop_counter++; }
        }

        if (cur_frame_index >= sheet.count)
        {
            handle_animation_finish();
            return;
        }

        show_frame(cur_frame_index);
        var new_interval = TimeSpan.FromMilliseconds(frame_ms(cur_frame_index));
        if (animation_timer.Interval != new_interval)
        {
            animation_timer.Stop();
            animation_timer.Interval = new_interval;
            animation_timer.Start();
        }
    }

    private void handle_animation_finish()
    {
        string prev_anim = cur_animation_name;
        string next_anim = "AnimMainIdle";
        bool start_delay = false;
        animation_timer?.Stop();

        if (write_mode_active && (prev_anim == "AnimTypingStart" || prev_anim == "AnimTyping" || prev_anim == "AnimTypingStop"))
        {
            string[] chain = { "AnimTypingStart", "AnimTyping", "AnimTypingStop" };
            string following = chain.Skip(Array.IndexOf(chain, prev_anim) + 1).FirstOrDefault(loaded_anim.ContainsKey) ?? "";
            if (following != "") next_anim = following;
            else
            {
                write_mode_active = false;
                typing_animation_active = false;
                typing_session_start_time = DateTime.MinValue;
                show_bubble();
                if (is_music_playing && is_listening_enabled && loaded_anim.ContainsKey(cur_music_loop)) next_anim = cur_music_loop;
                else start_delay = true;
            }
        }
        else if (prev_anim == "AnimScreenshotFinish")
        {
            if (is_screenshot_anim_active)
            {
                _ = take_screenshot();
                is_screenshot_anim_active = false;
                if (is_music_playing && is_listening_enabled && loaded_anim.ContainsKey(cur_music_loop)) next_anim = cur_music_loop;
                else if (typing_animation_active && loaded_anim.ContainsKey("AnimTyping")) next_anim = "AnimTyping";
                else start_delay = true;
            }
        }
        else if (is_in_afk_mode) { next_anim = afk_loop; }
        else if (prev_anim == afk_finish) { start_delay = true; }
        else if (prev_anim == "AnimCharacterMoveStart") { if (is_mouse_down && loaded_anim.ContainsKey("AnimCharacterMoving")) next_anim = "AnimCharacterMoving"; else if (!is_mouse_down && loaded_anim.ContainsKey("AnimCharacterMoveFinish")) next_anim = "AnimCharacterMoveFinish"; else start_delay = true; }
        else if (prev_anim == "AnimCharacterMoving") { if (!is_mouse_down && loaded_anim.ContainsKey("AnimCharacterMoveFinish")) next_anim = "AnimCharacterMoveFinish"; else if (is_mouse_down) next_anim = "AnimCharacterMoving"; else start_delay = true; }
        else if (prev_anim == "AnimCharacterMoveFinish") { start_delay = true; }
        else if (prev_anim == "AnimTypingStop") { typing_animation_active = false; typing_session_start_time = DateTime.MinValue; if (is_music_playing && is_listening_enabled && loaded_anim.ContainsKey(cur_music_loop)) next_anim = cur_music_loop; else if (is_dragging_file && loaded_anim.ContainsKey("AnimDragFileProcessing")) next_anim = "AnimDragFileProcessing"; else start_delay = true; }
        else if (prev_anim == "AnimTypingStart") { if (typing_animation_active && loaded_anim.ContainsKey("AnimTyping")) next_anim = "AnimTyping"; else if (loaded_anim.ContainsKey("AnimTypingStop")) next_anim = "AnimTypingStop"; else { typing_animation_active = false; start_delay = true; } }
        else if (prev_anim == "AnimDragFileStart") { if (is_dragging_file && loaded_anim.ContainsKey("AnimDragFileProcessing")) next_anim = "AnimDragFileProcessing"; else if (!is_dragging_file && loaded_anim.ContainsKey("AnimDragFileFinish")) next_anim = "AnimDragFileFinish"; else { is_dragging_file = false; start_delay = true; } }
        else if (prev_anim == "AnimDragFileProcessing" || prev_anim == "AnimDragFileFinish") { is_dragging_file = false; if (is_music_playing && is_listening_enabled && loaded_anim.ContainsKey(cur_music_loop)) next_anim = cur_music_loop; else if (typing_animation_active && loaded_anim.ContainsKey("AnimTyping")) next_anim = "AnimTyping"; else start_delay = true; }
        else if (prev_anim == cur_music_start) { if (is_music_playing && is_listening_enabled && loaded_anim.ContainsKey(cur_music_loop)) next_anim = cur_music_loop; else if (loaded_anim.ContainsKey(cur_music_finish)) next_anim = cur_music_finish; else { is_music_playing = false; start_delay = true; } }
        else if (prev_anim == cur_music_finish) { is_music_playing = false; if (is_dragging_file && loaded_anim.ContainsKey("AnimDragFileProcessing")) next_anim = "AnimDragFileProcessing"; else if (typing_animation_active && loaded_anim.ContainsKey("AnimTyping")) next_anim = "AnimTyping"; else start_delay = true; }
        else if (is_random_idle && prev_anim == cur_idle_finish) { is_random_idle = false; cur_idle_start = ""; cur_idle_loop = ""; cur_idle_finish = ""; idle_loop_counter = 0; if (is_music_playing && is_listening_enabled && loaded_anim.ContainsKey(cur_music_loop)) next_anim = cur_music_loop; else if (is_dragging_file && loaded_anim.ContainsKey("AnimDragFileProcessing")) next_anim = "AnimDragFileProcessing"; else if (typing_animation_active && loaded_anim.ContainsKey("AnimTyping")) next_anim = "AnimTyping"; else start_delay = true; }
        else if (is_random_idle && prev_anim == cur_idle_start) { if (loaded_anim.ContainsKey(cur_idle_loop)) { next_anim = cur_idle_loop; idle_loop_counter = 0; } else if (loaded_anim.ContainsKey(cur_idle_finish)) { next_anim = cur_idle_finish; idle_loop_counter = 0; } else { is_random_idle = false; start_delay = true; } }
        else if (is_random_idle && prev_anim == cur_idle_loop && idle_loop_counter >= max_idle_loops) { if (loaded_anim.ContainsKey(cur_idle_finish)) next_anim = cur_idle_finish; else { is_random_idle = false; start_delay = true; } }
        else if (one_off_random_idle_animations.Contains(prev_anim)) { if (is_music_playing && is_listening_enabled && loaded_anim.ContainsKey(cur_music_loop)) next_anim = cur_music_loop; else if (is_dragging_file && loaded_anim.ContainsKey("AnimDragFileProcessing")) next_anim = "AnimDragFileProcessing"; else if (typing_animation_active && loaded_anim.ContainsKey("AnimTyping")) next_anim = "AnimTyping"; else start_delay = true; }
        else { if (is_music_playing && is_listening_enabled && loaded_anim.ContainsKey(cur_music_loop)) next_anim = cur_music_loop; else if (is_dragging_file && loaded_anim.ContainsKey("AnimDragFileProcessing")) next_anim = "AnimDragFileProcessing"; else if (typing_animation_active && loaded_anim.ContainsKey("AnimTyping")) next_anim = "AnimTyping"; else if (is_random_idle) { is_random_idle = false; start_delay = true; } else start_delay = true; }

        if (start_delay && !is_in_afk_mode && !is_screenshot_anim_active)
        {
            animation_timer?.Stop();
            cur_animation_name = "AnimMainIdle";
            var idle_sheet = loaded_anim.get("AnimMainIdle");
            if (idle_sheet != null && idle_sheet.count > 0)
            {
                sheet = idle_sheet;
                fit_window();
                show_frame(0);
            }
            start_idle_delay();
        }
        else { load_animation(next_anim); cur_animation_name = next_anim; }

        is_chomik_dragging_animation = cur_animation_name == "AnimCharacterMoveStart" || cur_animation_name == "AnimCharacterMoving" || cur_animation_name == "AnimCharacterMoveFinish";
        is_random_idle = !is_in_afk_mode && (cur_animation_name.StartsWith("AnimIdleStart") || cur_animation_name.StartsWith("AnimIdleLoop") || cur_animation_name.StartsWith("AnimIdleFinish"));
        typing_animation_active = cur_animation_name == "AnimTypingStart" || cur_animation_name == "AnimTyping" || cur_animation_name == "AnimTypingStop";
        is_screenshot_anim_active = cur_animation_name == "AnimScreenshotFinish";
    }

    private void start_idle_delay()
    {
        if (is_in_afk_mode || is_chomik_dragging_animation || is_dragging_file || typing_animation_active || is_music_playing || is_screenshot_anim_active || write_mode_active) { idle_delay_timer?.Stop(); return; }
        if (idle_delay_timer != null) { idle_delay_timer.Stop(); double max_ms = Math.Max(1500, idle_delay * 1000); idle_delay_timer.Interval = TimeSpan.FromMilliseconds(1000 + rnd.NextDouble() * (max_ms - 1000)); idle_delay_timer.Start(); }
    }

    private void idle_delay_timer_tick(object? sender, EventArgs e)
    {
        idle_delay_timer?.Stop();
        if (is_in_afk_mode || is_chomik_dragging_animation || is_dragging_file || typing_animation_active || is_music_playing || is_screenshot_anim_active) return;
        string next_anim = "AnimMainIdle";

        if (rnd.Next(100) < 20 && one_off_random_idle_animations.Any(a => loaded_anim.ContainsKey(a)))
        {
            var avail = one_off_random_idle_animations.Where(a => loaded_anim.ContainsKey(a)).ToList();
            if (avail.Count > 0) next_anim = avail[rnd.Next(avail.Count)];
        }
        else if (rnd.Next(100) < 10)
        {
            var avail = loaded_anim.Keys.Where(k => k.StartsWith("AnimIdleStart") && k != afk_start).ToList();
            if (avail.Count > 0)
            {
                cur_idle_start = avail[rnd.Next(avail.Count)];
                if (int.TryParse(cur_idle_start.Replace("AnimIdleStart", ""), out int num))
                {
                    cur_idle_loop = $"AnimIdleLoop{num}";
                    cur_idle_finish = $"AnimIdleFinish{num}";
                    if (loaded_anim.ContainsKey(cur_idle_start)) { next_anim = cur_idle_start; is_random_idle = true; idle_loop_counter = 0; }
                    else if (loaded_anim.ContainsKey(cur_idle_loop)) { next_anim = cur_idle_loop; is_random_idle = true; idle_loop_counter = 0; }
                    else if (loaded_anim.ContainsKey(cur_idle_finish)) { next_anim = cur_idle_finish; is_random_idle = true; idle_loop_counter = 0; }
                }
            }
        }
        animation_timer?.Stop();
        load_animation(next_anim);
        cur_animation_name = next_anim;
        is_random_idle = !is_in_afk_mode && (cur_animation_name.StartsWith("AnimIdleStart") || cur_animation_name.StartsWith("AnimIdleLoop") || cur_animation_name.StartsWith("AnimIdleFinish"));
        if (next_anim == "AnimMainIdle") start_idle_delay();
    }

    private void on_context_requested(object? sender, ContextRequestedEventArgs e)
    {
        if (e.TryGetPosition(this, out var pos) && !is_opaque(pos.X, pos.Y)) e.Handled = true;
    }

    private void on_pointer_pressed(object? sender, PointerPressedEventArgs e)
    {
        var local_pos = e.GetPosition(this);
        if (!is_opaque(local_pos.X, local_pos.Y))
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
            if (!is_in_afk_mode && !is_music_playing && !typing_animation_active && !is_dragging_file && !is_screenshot_anim_active && !write_mode_active && !inf_animations.Contains(cur_animation_name) && !cur_animation_name.StartsWith("AnimCharacterMove"))
            {
                animation_timer?.Stop();
                if (loaded_anim.ContainsKey("AnimCharacterMoveStart")) { load_animation("AnimCharacterMoveStart"); cur_animation_name = "AnimCharacterMoveStart"; }
                else if (loaded_anim.ContainsKey("AnimCharacterMoving")) { load_animation("AnimCharacterMoving"); cur_animation_name = "AnimCharacterMoving"; }
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
            if (!is_in_afk_mode && !is_dragging_file && !is_music_playing && !typing_animation_active && !is_screenshot_anim_active && !write_mode_active && cur_animation_name != "AnimCharacterMoveStart" && cur_animation_name != "AnimCharacterMoving" && cur_animation_name != "AnimCharacterMoveFinish" && loaded_anim.ContainsKey("AnimCharacterMoving"))
            {
                idle_delay_timer?.Stop();
                animation_timer?.Stop();
                load_animation("AnimCharacterMoving");
                cur_animation_name = "AnimCharacterMoving";
            }
        }
    }

    private void on_pointer_released(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left)
        {
            is_mouse_down = false;
            update_user_activity();
            if (!is_in_afk_mode && is_chomik_dragging_animation)
            {
                if (loaded_anim.ContainsKey("AnimCharacterMoveFinish")) { animation_timer?.Stop(); load_animation("AnimCharacterMoveFinish"); cur_animation_name = "AnimCharacterMoveFinish"; }
                else handle_animation_finish();
            }
        }
    }

    private void on_drag_enter(object? sender, DragEventArgs e)
    {
        if (is_in_afk_mode || is_dragging_file || is_screenshot_anim_active || write_mode_active) { e.DragEffects = DragDropEffects.None; return; }
        e.DragEffects = DragDropEffects.Copy;
        if (is_chomik_dragging_animation) is_mouse_down = false;
        if (!is_music_playing && !cur_animation_name.StartsWith("AnimDragFile"))
        {
            is_dragging_file = true;
            idle_delay_timer?.Stop();
            animation_timer?.Stop();
            if (loaded_anim.ContainsKey("AnimDragFileStart")) { load_animation("AnimDragFileStart"); cur_animation_name = "AnimDragFileStart"; }
            else if (loaded_anim.ContainsKey("AnimDragFileProcessing")) { load_animation("AnimDragFileProcessing"); cur_animation_name = "AnimDragFileProcessing"; }
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
                            if (perm_delete)
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

            if (loaded_anim.ContainsKey("AnimDragFileFinish")) { animation_timer?.Stop(); load_animation("AnimDragFileFinish"); cur_animation_name = "AnimDragFileFinish"; }
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
        if (OperatingSystem.IsWindows() && hook_id != IntPtr.Zero)
            win.UnhookWindowsHookEx(hook_id);
        base.OnClosed(e);
    }
}
