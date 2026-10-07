using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace chomik;

public partial class MainWindow
{
    private async void music_check_timer_tick(object? sender, EventArgs e) => await check_music_state_async();

    private async Task check_music_state_async()
    {
        if (!is_listening_enabled)
        {
            if (is_music_playing)
            {
                is_music_playing = false;
                if (cur_animation_name == cur_music_start || cur_animation_name == cur_music_loop) handle_animation_finish();
            }
            return;
        }

        bool was_playing = is_music_playing;
        bool now_playing = await detect_music_playing_async();
        is_music_playing = now_playing;

        if (is_music_playing != was_playing && !is_in_afk_mode && !is_chomik_dragging_animation && !is_dragging_file && !typing_animation_active && !is_screenshot_anim_active && !write_mode_active)
        {
            idle_delay_timer?.Stop();
            animation_timer?.Stop();
            if (is_music_playing)
            {
                if (loaded_anim.ContainsKey(cur_music_start)) { load_animation(cur_music_start); cur_animation_name = cur_music_start; }
                else if (loaded_anim.ContainsKey(cur_music_loop)) { load_animation(cur_music_loop); cur_animation_name = cur_music_loop; }
            }
            else
            {
                if (cur_animation_name == cur_music_start || cur_animation_name == cur_music_loop)
                {
                    if (loaded_anim.ContainsKey(cur_music_finish)) { load_animation(cur_music_finish); cur_animation_name = cur_music_finish; }
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
        int hr = CoInitializeEx(IntPtr.Zero, 0x0);
        bool com_ready = hr == 0 || hr == 1;

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
            if (com_ready) CoUninitialize();
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
}
