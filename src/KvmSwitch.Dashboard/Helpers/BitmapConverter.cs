using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace KvmSwitch.Dashboard.Helpers;

/// <summary>
/// Converts System.Drawing.Bitmap to WPF BitmapSource.
/// </summary>
public static class BitmapConverter
{
    public static BitmapSource ToBitmapSource(Bitmap bitmap)
    {
        if (bitmap == null)
        {
            return CreateEmptyBitmapSource();
        }

        using var memory = new MemoryStream();
        bitmap.Save(memory, ImageFormat.Bmp);
        memory.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }

    public static BitmapSource ToBitmapSource(IntPtr frameData, int width, int height, int stride)
    {
        if (frameData == IntPtr.Zero || width <= 0 || height <= 0)
        {
            return CreateEmptyBitmapSource();
        }

        try
        {
            var bitmap = new WriteableBitmap(width, height, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);
            bitmap.Lock();

            // Copy frame data to bitmap
            var sourcePtr = frameData;
            var destPtr = bitmap.BackBuffer;
            var bytesToCopy = stride * height;

            unsafe
            {
                var source = (byte*)sourcePtr;
                var dest = (byte*)destPtr;
                
                for (int y = 0; y < height; y++)
                {
                    var sourceLine = source + (y * stride);
                    var destLine = dest + (y * bitmap.BackBufferStride);
                    Buffer.MemoryCopy(sourceLine, destLine, bitmap.BackBufferStride, stride);
                }
            }

            bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
            bitmap.Unlock();
            bitmap.Freeze();

            return bitmap;
        }
        catch
        {
            return CreateEmptyBitmapSource();
        }
    }

    private static BitmapSource CreateEmptyBitmapSource()
    {
        var bitmap = new WriteableBitmap(1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);
        bitmap.Freeze();
        return bitmap;
    }
}

