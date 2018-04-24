using ConvolutedDenoiser.Image.Loading;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ConvolutedDenoiser.Image.Manipulation
{
    public class Noiser
    {
        private double _noise;
        public double Noise
        {
            get => _noise;
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException();

                _noise = value;
            }
        }

        private static readonly Random Global = new Random();
        private readonly ThreadLocal<Random> _r;

        public Noiser(double noise = 0.3)
        {
            _r = new ThreadLocal<Random>(() =>
            {
                int seed;
                lock (Global)
                {
                    seed = Global.Next();
                }

                return new Random(seed);
            });

            Noise = noise;
        }

        public Bitmap AddNoise(Bitmap originalImage)
        {
            var bmap = new Bitmap(originalImage.Width, originalImage.Height);
            var amount = (int) Noise * 255;

            for (var x = 0; x < originalImage.Width; x++)
            {
                for (var y = 0; y < originalImage.Height; y++)
                {
                    var pixel = originalImage.GetPixel(x, y);
                    var r = pixel.R + _r.Value.Next(amount + 1);
                    //var g = pixel.G + _r.Next(-amount, amount + 1);
                    //var b = pixel.B + _r.Next(-amount, amount + 1);

                    r = Math.Max(0, Math.Min(r, 255));
                    //g = Math.Max(0, Math.Min(g, 255));
                    //b = Math.Max(0, Math.Min(b, 255));

                    var newCol = Color.FromArgb(r, r, r);
                    bmap.SetPixel(x, y, newCol);
                }
            }

            return bmap;
        }

        public ImageFile AddNoise(ImageFile originalImage)
        {
            var noised = originalImage.CopyBlack();
            var amount = (int) (Noise * noised.MaxGrayVal);
            for (var x = 0; x < noised.Width; x++)
            {
                for (var y = 0; y < noised.Height; y++)
                {
                    var pixel = (int) originalImage.Pixels[x, y];
                    //var noise = _r.Value.Next(-amount, amount + 1);
                    //pixel += noise;
                    var u1 = 1 - _r.Value.NextDouble();
                    var u2 = 1 - _r.Value.NextDouble();
                    var randStdNormal = Math.Sqrt(-2 * Math.Log(u1)) * Math.Sin(2 * Math.PI * u2);
                    pixel += randStdNormal > 0 ? (int) (noised.MaxGrayVal * 0.1 * randStdNormal) : 0;
                    pixel = Math.Max(0, Math.Min(pixel, noised.MaxGrayVal));
                    noised.Pixels[x, y] = (ushort) pixel;
                }
            }

            return noised;
        }

        public async Task<IEnumerable<ImageFile>> NoiseImagesAsync(IEnumerable<ImageFile> images)
        {
            var tasks = images.Select(image => Task.Run(() => AddNoise(image)));

            return await Task.WhenAll(tasks);
        }
    }
}
