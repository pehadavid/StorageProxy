using ImageMagick;

namespace StorageProxy.Engines;

public static class CdnSettings
{
    private static MagickFormat _magickFormat;

    public static void SetFormat(string format)
    {
        switch (format)
        {
            case "webp":
                _magickFormat = MagickFormat.WebP;
                break;
            default:
                _magickFormat = MagickFormat.Pjpeg;
                break;
        }
    }

    public static MagickFormat GetMagickFormat()
    {
        return _magickFormat;
    }
}