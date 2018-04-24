using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConvolutedDenoiser.Image.Loading
{
    public static class ImageLoader
    {
        public const string ImageFormat = ".pgm";

        public static async Task<List<ImageFile>> LoadImagesFromDir(string directory)
        {
            var info = new DirectoryInfo(directory);

            var tasks = info.EnumerateFiles($"*{ImageFormat}")
                .Select(file => Task.Run(() =>
                {
                    try
                    {
                        return ImageFile.ReadFromFile(file);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }

                    return null;
                }));

            var images = (await Task.WhenAll(tasks)).Where(i => i != null);

            return images.ToList();
        }
    }
}
