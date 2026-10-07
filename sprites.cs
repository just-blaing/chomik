using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace chomik;

public record anim_def(string file, int fw, int fh, int cols, int gap, int[] durations);

public unsafe class sprite_sheet : IDisposable
{
    private readonly byte[] alpha;
    private readonly int sheet_w;
    private readonly PixelRect[] bounds_cache;
    private readonly bool[] bounds_ready;

    public Bitmap image { get; }
    public int frame_w { get; }
    public int frame_h { get; }
    public int cols { get; }
    public int gap { get; }
    public int[] durations { get; }
    public int count => durations.Length;
    public IntPtr[] regions { get; }
    public double regions_scale { get; private set; }

    public sprite_sheet(string path, anim_def def)
    {
        using (var stream = File.OpenRead(path)) image = new Bitmap(stream);
        frame_w = def.fw;
        frame_h = def.fh;
        cols = def.cols;
        gap = def.gap;
        durations = def.durations;
        regions = new IntPtr[count];
        bounds_cache = new PixelRect[count];
        bounds_ready = new bool[count];
        sheet_w = image.PixelSize.Width;
        int sheet_h = image.PixelSize.Height;
        var pixels = new byte[sheet_w * sheet_h * 4];
        fixed (byte* p = pixels)
            image.CopyPixels(new PixelRect(0, 0, sheet_w, sheet_h), (IntPtr)p, pixels.Length, sheet_w * 4);
        alpha = new byte[sheet_w * sheet_h];
        for (int i = 0; i < alpha.Length; i++) alpha[i] = pixels[i * 4 + 3];
    }

    public Rect frame_rect(int i) => new(i % cols * (frame_w + gap), i / cols * (frame_h + gap), frame_w, frame_h);

    public bool opaque(int i, int x, int y)
    {
        if (i < 0 || i >= count || x < 0 || y < 0 || x >= frame_w || y >= frame_h) return false;
        int index = (i / cols * (frame_h + gap) + y) * sheet_w + i % cols * (frame_w + gap) + x;
        return index < alpha.Length && alpha[index] > 10;
    }

    public PixelRect bounds(int i)
    {
        if (i < 0 || i >= count) return new PixelRect(frame_w / 2, 0, 0, 0);
        if (bounds_ready[i]) return bounds_cache[i];
        int min_x = frame_w, min_y = frame_h, max_x = -1, max_y = -1;
        for (int y = 0; y < frame_h; y++)
            for (int x = 0; x < frame_w; x++)
                if (opaque(i, x, y))
                {
                    if (x < min_x) min_x = x;
                    if (x > max_x) max_x = x;
                    if (y < min_y) min_y = y;
                    if (y > max_y) max_y = y;
                }
        var result = max_x < 0
            ? new PixelRect(frame_w / 2, 0, 0, 0)
            : new PixelRect(min_x, min_y, max_x - min_x + 1, max_y - min_y + 1);
        bounds_cache[i] = result;
        bounds_ready[i] = true;
        return result;
    }

    public PixelRect union_bounds()
    {
        PixelRect? total = null;
        for (int i = 0; i < count; i++)
        {
            var b = bounds(i);
            if (b.Width == 0) continue;
            total = total == null ? b : total.Value.Union(b);
        }
        return total ?? new PixelRect(0, 0, frame_w, frame_h);
    }

    public void clear_regions(double scale)
    {
        for (int i = 0; i < regions.Length; i++)
        {
            if (regions[i] == IntPtr.Zero) continue;
            win.free_region(regions[i]);
            regions[i] = IntPtr.Zero;
        }
        regions_scale = scale;
    }

    public void Dispose()
    {
        clear_regions(0);
        image.Dispose();
    }
}

public class sprite_view : Control
{
    private sprite_sheet? sheet;
    private int index;
    private PixelRect? crop;
    private double zoom = 1;
    private double stretch = 1;

    public void configure(PixelRect? new_crop, double new_zoom, double new_stretch = 1)
    {
        crop = new_crop;
        zoom = new_zoom;
        stretch = new_stretch;
        InvalidateMeasure();
        InvalidateVisual();
    }

    public void show(sprite_sheet? new_sheet, int new_index)
    {
        bool resized = new_sheet?.frame_w != sheet?.frame_w || new_sheet?.frame_h != sheet?.frame_h;
        sheet = new_sheet;
        index = new_index;
        if (resized) InvalidateMeasure();
        InvalidateVisual();
    }

    private Rect source()
    {
        var r = sheet!.frame_rect(index);
        if (crop is not { } c) return r;
        return new Rect(r.X + c.X, r.Y + c.Y, c.Width, c.Height);
    }

    protected override Size MeasureOverride(Size available)
    {
        if (sheet == null) return default;
        var s = source();
        return new Size(s.Width * zoom * stretch, s.Height * zoom);
    }

    public override void Render(DrawingContext context)
    {
        if (sheet == null || index >= sheet.count) return;
        var s = source();
        context.DrawImage(sheet.image, s, new Rect(0, 0, s.Width * zoom * stretch, s.Height * zoom));
    }
}

public class anim_lib
{
    private const int max_cached = 8;
    private readonly string dir;
    private readonly Dictionary<string, anim_def> defs = new();
    private readonly Dictionary<string, sprite_sheet> cache = new();
    private readonly List<string> order = new();

    public IEnumerable<string> Keys => defs.Keys;

    public anim_lib(string anims_path)
    {
        dir = Path.Combine(Path.GetDirectoryName(anims_path) ?? string.Empty, "sheets");
        if (!File.Exists(anims_path)) return;
        var lines = File.ReadAllLines(anims_path);
        for (int i = 0; i + 1 < lines.Length; i++)
        {
            var head = lines[i].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (head.Length != 6 || !head[0].StartsWith("Anim")) continue;
            if (!int.TryParse(head[2], out int fw) || !int.TryParse(head[3], out int fh)) continue;
            if (!int.TryParse(head[4], out int cols) || !int.TryParse(head[5], out int gap)) continue;
            var durations = lines[i + 1]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out int ms) ? ms : 100)
                .ToArray();
            defs[head[0]] = new anim_def(head[1], fw, fh, cols, gap, durations);
            i++;
        }
    }

    public bool ContainsKey(string name) => defs.ContainsKey(name);

    public sprite_sheet? get(string name)
    {
        if (cache.TryGetValue(name, out var cached))
        {
            order.Remove(name);
            order.Add(name);
            return cached;
        }
        var sheet = open_copy(name);
        if (sheet == null) return null;
        cache[name] = sheet;
        order.Add(name);
        while (order.Count > max_cached)
        {
            cache[order[0]].Dispose();
            cache.Remove(order[0]);
            order.RemoveAt(0);
        }
        return sheet;
    }

    public sprite_sheet? open_copy(string name)
    {
        if (!defs.TryGetValue(name, out var def)) return null;
        try { return new sprite_sheet(Path.Combine(dir, def.file), def); }
        catch { return null; }
    }
}
