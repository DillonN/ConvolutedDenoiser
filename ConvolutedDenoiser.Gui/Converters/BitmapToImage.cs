using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ConvolutedDenoiser.Gui.Converters
{
    internal class BitmapToImage : IValueConverter
    {
        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Bitmap bmap)
            {
                BitmapSource source = null;
                var ptr = IntPtr.Zero;
                try
                {
                    ptr = bmap.GetHbitmap();
                    source = Imaging.CreateBitmapSourceFromHBitmap(
                        ptr,
                        IntPtr.Zero,
                        Int32Rect.Empty,
                        BitmapSizeOptions.FromEmptyOptions());
                }
                catch (Exception e)
                { }
                finally
                {
                    if (ptr != IntPtr.Zero)
                    {
                        DeleteObject(ptr);
                    }
                }

                return source;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
