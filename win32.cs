using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace chomik;

public static class win
{
    public const int wh_keyboard_ll = 13;
    public const int wm_keydown = 0x0100;
    public const int wm_syskeydown = 0x0104;
    public const int gwlp_wndproc = -4;
    public const int gwl_exstyle = -20;
    public const int ws_ex_toolwindow = 0x00000080;
    public const int ws_ex_appwindow = 0x00040000;
    public const int wm_nchittest = 0x0084;
    public const int httransparent = -1;
    private const int rgn_copy = 5;
    private const uint srccopy = 0x00CC0020;
    private const uint captureblt = 0x40000000;
    private const uint cf_dib = 8;
    private const uint gmem_moveable = 0x0002;
    private const uint swp_nosize = 0x0001;
    private const uint swp_nomove = 0x0002;
    private const uint swp_noactivate = 0x0010;
    private static readonly IntPtr hwnd_topmost = new(-1);

    public delegate IntPtr key_proc(int n_code, IntPtr w_param, IntPtr l_param);
    public delegate IntPtr wnd_proc_delegate(IntPtr hwnd, uint msg, IntPtr w_param, IntPtr l_param);

    [StructLayout(LayoutKind.Sequential)]
    public struct kbd_hook_struct
    {
        public uint vk_code;
        public uint scan_code;
        public uint flags;
        public uint time;
        public IntPtr dw_extra_info;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct win32_point { public int x, y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct bitmap_info
    {
        public uint size;
        public int width;
        public int height;
        public ushort planes;
        public ushort bit_count;
        public uint compression;
        public uint size_image;
        public int x_ppm;
        public int y_ppm;
        public uint clr_used;
        public uint clr_important;
        public uint color;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr get_window_long_64(IntPtr hwnd, int n_index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr set_window_long_64(IntPtr hwnd, int n_index, IntPtr new_long);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern int get_window_long_32(IntPtr hwnd, int n_index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int set_window_long_32(IntPtr hwnd, int n_index, int new_long);

    [DllImport("user32.dll")]
    public static extern IntPtr CallWindowProc(IntPtr prev_wnd_func, IntPtr hwnd, uint msg, IntPtr w_param, IntPtr l_param);

    [DllImport("user32.dll")]
    public static extern bool ScreenToClient(IntPtr hwnd, ref win32_point pt);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int id_hook, key_proc lpfn, IntPtr h_mod, uint dw_thread_id);

    [DllImport("user32.dll")]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int n_code, IntPtr w_param, IntPtr l_param);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string lp_module_name);

    [DllImport("user32.dll")]
    private static extern int SetWindowRgn(IntPtr hwnd, IntPtr hrgn, bool redraw);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insert_after, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

    [DllImport("gdi32.dll")]
    private static extern int CombineRgn(IntPtr dest, IntPtr src1, IntPtr src2, int mode);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern IntPtr ExtCreateRegion(IntPtr xform, uint count, byte[] data);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdc_src, int x1, int y1, uint rop);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbm, uint start, uint lines, byte[] bits, ref bitmap_info info, uint usage);

    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll")]
    private static extern IntPtr SetClipboardData(uint format, IntPtr mem);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr mem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr mem);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalFree(IntPtr mem);

    public static IntPtr GetWindowLongPtr(IntPtr hwnd, int n_index) =>
        IntPtr.Size == 8 ? get_window_long_64(hwnd, n_index) : new IntPtr(get_window_long_32(hwnd, n_index));

    public static IntPtr SetWindowLongPtr(IntPtr hwnd, int n_index, IntPtr new_long) =>
        IntPtr.Size == 8 ? set_window_long_64(hwnd, n_index, new_long) : new IntPtr(set_window_long_32(hwnd, n_index, new_long.ToInt32()));
        
    public static IntPtr set_key_hook(key_proc proc)
    {
        using var cur_process = Process.GetCurrentProcess();
        using var cur_module = cur_process.MainModule;
        if (cur_module?.ModuleName == null) return IntPtr.Zero;
        return SetWindowsHookEx(wh_keyboard_ll, proc, GetModuleHandle(cur_module.ModuleName), 0);
    }

    public static void free_region(IntPtr region) => DeleteObject(region);

    public static void keep_on_top(IntPtr hwnd) => SetWindowPos(hwnd, hwnd_topmost, 0, 0, 0, 0, swp_nosize | swp_nomove | swp_noactivate);

    public static void set_region(IntPtr hwnd, sprite_sheet sheet, int index, double scale)
    {
        if (sheet.regions_scale != scale) sheet.clear_regions(scale);
        if (index < 0 || index >= sheet.count) return;
        if (sheet.regions[index] == IntPtr.Zero) sheet.regions[index] = build_region(sheet, index, scale);
        var copy = CreateRectRgn(0, 0, 0, 0);
        CombineRgn(copy, sheet.regions[index], IntPtr.Zero, rgn_copy);
        SetWindowRgn(hwnd, copy, false);
    }

    private static IntPtr build_region(sprite_sheet sheet, int index, double scale)
    {
        var rects = new List<int>();
        for (int y = 0; y < sheet.frame_h; y++)
        {
            int top = (int)Math.Round(y * scale);
            int bottom = (int)Math.Round((y + 1) * scale);
            if (bottom <= top) continue;
            int x = 0;
            while (x < sheet.frame_w)
            {
                while (x < sheet.frame_w && !sheet.opaque(index, x, y)) x++;
                if (x >= sheet.frame_w) break;
                int start = x;
                while (x < sheet.frame_w && sheet.opaque(index, x, y)) x++;
                rects.Add((int)Math.Round(start * scale));
                rects.Add(top);
                rects.Add((int)Math.Round(x * scale));
                rects.Add(bottom);
            }
        }

        int full_w = (int)Math.Ceiling(sheet.frame_w * scale);
        int full_h = (int)Math.Ceiling(sheet.frame_h * scale);
        int n = rects.Count / 4;
        if (n == 0) return CreateRectRgn(0, 0, 0, 0);

        int min_x = int.MaxValue, min_y = int.MaxValue, max_x = 0, max_y = 0;
        for (int i = 0; i < rects.Count; i += 4)
        {
            min_x = Math.Min(min_x, rects[i]);
            min_y = Math.Min(min_y, rects[i + 1]);
            max_x = Math.Max(max_x, rects[i + 2]);
            max_y = Math.Max(max_y, rects[i + 3]);
        }

        var ints = new int[8 + rects.Count];
        ints[0] = 32;
        ints[1] = 1;
        ints[2] = n;
        ints[3] = n * 16;
        ints[4] = min_x;
        ints[5] = min_y;
        ints[6] = max_x;
        ints[7] = max_y;
        rects.CopyTo(ints, 8);
        var bytes = new byte[ints.Length * 4];
        Buffer.BlockCopy(ints, 0, bytes, 0, bytes.Length);
        var region = ExtCreateRegion(IntPtr.Zero, (uint)bytes.Length, bytes);
        return region != IntPtr.Zero ? region : CreateRectRgn(0, 0, full_w, full_h);
    }

    public static Bitmap? capture(PixelRect area)
    {
        int w = area.Width, h = area.Height;
        var screen_dc = GetDC(IntPtr.Zero);
        var mem_dc = CreateCompatibleDC(screen_dc);
        var hbmp = CreateCompatibleBitmap(screen_dc, w, h);
        var old = SelectObject(mem_dc, hbmp);
        try
        {
            if (!BitBlt(mem_dc, 0, 0, w, h, screen_dc, area.X, area.Y, srccopy | captureblt)) return null;
            var info = new bitmap_info { size = 40, width = w, height = -h, planes = 1, bit_count = 32 };
            var buf = new byte[w * h * 4];
            if (GetDIBits(mem_dc, hbmp, 0, (uint)h, buf, ref info, 0) == 0) return null;
            for (int i = 3; i < buf.Length; i += 4) buf[i] = 255;
            var result = new WriteableBitmap(new PixelSize(w, h), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Opaque);
            using var fb = result.Lock();
            for (int y = 0; y < h; y++)
                Marshal.Copy(buf, y * w * 4, fb.Address + y * fb.RowBytes, w * 4);
            return result;
        }
        finally
        {
            SelectObject(mem_dc, old);
            DeleteObject(hbmp);
            DeleteDC(mem_dc);
            ReleaseDC(IntPtr.Zero, screen_dc);
        }
    }

    public static bool copy_image(Bitmap bmp, IntPtr hwnd)
    {
        int w = bmp.PixelSize.Width, h = bmp.PixelSize.Height;
        int stride = w * 4;
        var pixels = new byte[stride * h];
        var pin = GCHandle.Alloc(pixels, GCHandleType.Pinned);
        try { bmp.CopyPixels(new PixelRect(0, 0, w, h), pin.AddrOfPinnedObject(), pixels.Length, stride); }
        finally { pin.Free(); }

        var header = new byte[40];
        BitConverter.TryWriteBytes(header.AsSpan(0), 40);
        BitConverter.TryWriteBytes(header.AsSpan(4), w);
        BitConverter.TryWriteBytes(header.AsSpan(8), h);
        BitConverter.TryWriteBytes(header.AsSpan(12), (short)1);
        BitConverter.TryWriteBytes(header.AsSpan(14), (short)32);
        BitConverter.TryWriteBytes(header.AsSpan(20), pixels.Length);

        var mem = GlobalAlloc(gmem_moveable, (UIntPtr)(40 + pixels.Length));
        if (mem == IntPtr.Zero) return false;
        var ptr = GlobalLock(mem);
        if (ptr == IntPtr.Zero)
        {
            GlobalFree(mem);
            return false;
        }
        Marshal.Copy(header, 0, ptr, 40);
        for (int y = 0; y < h; y++)
            Marshal.Copy(pixels, (h - 1 - y) * stride, ptr + 40 + y * stride, stride);
        GlobalUnlock(mem);

        bool opened = false;
        for (int i = 0; i < 10 && !opened; i++)
        {
            opened = OpenClipboard(hwnd);
            if (!opened) Thread.Sleep(30);
        }
        if (!opened)
        {
            GlobalFree(mem);
            return false;
        }

        bool ok = false;
        try
        {
            EmptyClipboard();
            ok = SetClipboardData(cf_dib, mem) != IntPtr.Zero;
        }
        finally
        {
            CloseClipboard();
        }
        if (!ok) GlobalFree(mem);
        return ok;
    }
}
