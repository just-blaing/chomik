using Avalonia.Media.Imaging;

namespace chomik;

public class animation_frame
{
    public Bitmap? image { get; set; }
    public int duration { get; set; }

    public animation_frame(Bitmap? img, int dur)
    {
        image = img;
        duration = dur;
    }
}