using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace chomik;

public static unsafe class plat
{
    private const int shape_input = 2;

    private static IntPtr x_display = IntPtr.Zero;
    private static bool x_failed;
    private static sprite_sheet? last_sheet;
    private static int last_index = -1;
    private static double last_scale;
    private static readonly byte[] modifier_codes = { 37, 50, 62, 64, 66, 105, 108, 133, 134 };

    [DllImport("libX11.so.6", EntryPoint = "XOpenDisplay")]
    private static extern IntPtr x_open_display(IntPtr name);

    [DllImport("libX11.so.6", EntryPoint = "XQueryKeymap")]
    private static extern int x_query_keymap(IntPtr display, byte* keys);

    [DllImport("libX11.so.6", EntryPoint = "XDefaultRootWindow")]
    private static extern IntPtr x_root_window(IntPtr display);

    [DllImport("libX11.so.6", EntryPoint = "XCreateBitmapFromData")]
    private static extern IntPtr x_create_bitmap(IntPtr display, IntPtr drawable, byte* data, uint width, uint height);

    [DllImport("libX11.so.6", EntryPoint = "XFreePixmap")]
    private static extern int x_free_pixmap(IntPtr display, IntPtr pixmap);

    [DllImport("libX11.so.6", EntryPoint = "XFlush")]
    private static extern int x_flush(IntPtr display);

    [DllImport("libXext.so.6", EntryPoint = "XShapeCombineMask")]
    private static extern void x_shape_combine_mask(IntPtr display, IntPtr window, int kind, int x_off, int y_off, IntPtr pixmap, int op);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics", EntryPoint = "CGEventSourceSecondsSinceLastEventType")]
    private static extern double mac_seconds_since(int state, uint event_type);

    private static IntPtr display()
    {
        if (x_failed) return IntPtr.Zero;
        if (x_display != IntPtr.Zero) return x_display;
        try { x_display = x_open_display(IntPtr.Zero); }
        catch { }
        if (x_display == IntPtr.Zero) x_failed = true;
        return x_display;
    }

    private static double mac_idle()
    {
        try { return mac_seconds_since(0, 10); }
        catch { return double.MaxValue; }
    }

    public static bool key_down()
    {
        if (OperatingSystem.IsMacOS()) return mac_idle() < 0.1;
        if (!OperatingSystem.IsLinux()) return false;
        var d = display();
        if (d == IntPtr.Zero) return false;
        byte* keys = stackalloc byte[32];
        try { x_query_keymap(d, keys); }
        catch
        {
            x_failed = true;
            return false;
        }
        for (int code = 8; code < 256; code++)
        {
            if ((keys[code / 8] & (1 << (code % 8))) == 0) continue;
            if (Array.IndexOf(modifier_codes, (byte)code) < 0) return true;
        }
        return false;
    }

    public static void set_shape(IntPtr window, sprite_sheet sheet, int index, double scale)
    {
        var d = display();
        if (d == IntPtr.Zero || index < 0 || index >= sheet.count) return;
        if (ReferenceEquals(sheet, last_sheet) && index == last_index && scale == last_scale) return;
        int w = Math.Max(1, (int)Math.Round(sheet.frame_w * scale));
        int h = Math.Max(1, (int)Math.Round(sheet.frame_h * scale));
        int stride = (w + 7) / 8;
        var bits = new byte[stride * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (sheet.opaque(index, (int)(x / scale), (int)(y / scale)))
                    bits[y * stride + x / 8] |= (byte)(1 << (x % 8));
        try
        {
            fixed (byte* p = bits)
            {
                var mask = x_create_bitmap(d, x_root_window(d), p, (uint)w, (uint)h);
                if (mask == IntPtr.Zero) return;
                x_shape_combine_mask(d, window, shape_input, 0, 0, mask, 0);
                x_free_pixmap(d, mask);
            }
            x_flush(d);
            last_sheet = sheet;
            last_index = index;
            last_scale = scale;
        }
        catch { x_failed = true; }
    }

    public static string lang()
    {
        if (OperatingSystem.IsMacOS())
        {
            foreach (var line in run("defaults", "read -g AppleLanguages").Split('\n'))
            {
                var item = line.Trim().Trim(',', '"');
                if (item.Length < 2) continue;
                return item.StartsWith("ru") ? "ru" : "en";
            }
            return "en";
        }
        foreach (var line in run("locale", "").Split('\n'))
        {
            if (!line.StartsWith("LC_MESSAGES=") && !line.StartsWith("LANG=")) continue;
            var value = line[(line.IndexOf('=') + 1)..].Trim('"');
            if (value.StartsWith("ru")) return "ru";
        }
        return "en";
    }

    private static string run(string file, string args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo(file, args) { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
            if (p == null) return "";
            string text = p.StandardOutput.ReadToEnd();
            p.WaitForExit(2000);
            return text;
        }
        catch { return ""; }
    }
}
