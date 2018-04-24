using ConvolutedDenoiser.Image.Enums;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace ConvolutedDenoiser.Image.Loading 
{
    public class ImageFile
    {
        public readonly ImageFileType Type;
        public readonly int Width;
        public readonly int Height;
        public readonly ushort MaxGrayVal;
        public string Name { get; }

        public readonly ushort[,] Pixels;

        public int SizeOfPixel => MaxGrayVal <= 255 ? 1 : 2;

        public ImageFile(Stream sr, string name, int downScale = 2)
        {
            Name = "";
            Type = (ImageFileType) ((sr.ReadByte() << 8) + sr.ReadByte());
            sr.ReadByte();
            var width = ReadIntToWhitespace(sr);
            var height = ReadIntToWhitespace(sr);
            Width = width / downScale;
            Height = height / downScale;
            MaxGrayVal = (ushort) ReadIntToWhitespace(sr);

            var pixels = new ushort[height, width];

            for (var h = 0; h < height; h++)
            {
                for (var w = 0; w < width; w++)
                {
                    pixels[h, w] = ReadShort(sr, SizeOfPixel);
                }
            }

            Pixels = new ushort[Height, Width];
            for (var h = 0; h < height - 1; h += downScale)
            {
                for (var w = 0; w < width - 1; w += downScale)
                {
                    var pixel = 0d;
                    for (var i = 0; i < downScale; i++)
                    {
                        for (var j = 0; j < downScale; j++)
                        {
                            pixel += pixels[h + i, w + j];
                        }
                    }

                    Pixels[h / downScale, w / downScale] = (ushort) Math.Round(pixel / downScale / downScale);
                }
            }
        }

        public ImageFile(int width, int height, ushort maxGrayVal, string name, ushort[,] pixels,
            ImageFileType type = ImageFileType.Pgm)
        {
            Width = width;
            Height = height;
            MaxGrayVal = maxGrayVal;
            Name = name;
            Pixels = pixels;
            Type = type;
        }

        public ImageFile CopyBlack()
        {
            var pixels = new ushort[Width, Height];
            return new ImageFile(Width, Height, MaxGrayVal, Name, pixels, Type);
        }

        private static byte[] ReadToWhitespace(Stream sr, int max = 0)
        {
            var byteList = new List<byte>();
            var comments = 0;
            var lines = 0;
            while (true)
            {
                var x = sr.ReadByte();
                if (x < 0) break;

                var b = (byte) x;
                if (IsComment(b)) comments++;
                else if (IsNewLine(b) && lines < comments) lines++;
                else if (lines >= comments)
                {
                    if (IsWhitespace(b)) break;
                    byteList.Add(b);
                    if (max > 0 && byteList.Count >= max) break;
                }
            }

            Debug.Assert(max <= 0 || byteList.Count <= max);
            return byteList.ToArray();
        }

        private static string ReadStringToWhitespace(Stream sr, int max = 0)
        {
            var read = ReadToWhitespace(sr, max);
            return Encoding.ASCII.GetString(read);
        }

        private static int ReadIntToWhitespace(Stream sr, int max = 0)
        {
            var read = ReadStringToWhitespace(sr, max);
            return int.Parse(read);
        }

        private static ushort ReadShort(Stream sr, int max = 2)
        {
            if (max < 1 || max > 2)
                throw new ArgumentException();

            return (ushort) (max == 2 ? (sr.ReadByte() << 8) + sr.ReadByte() : sr.ReadByte());
        }

        private static bool IsWhitespace(byte b)
        {
            return char.IsWhiteSpace((char) b);
        }

        private static bool IsNewLine(byte b)
        {
            return b == 10 || b == 13;
        }

        private static bool IsComment(byte b)
        {
            return (char) b == '#';
        }

        public Bitmap Bitmap
        {
            get
            {
                var bmap = new Bitmap(Width, Height);
                //var gr = Graphics.FromImage(bmap);
                for (var h = 0; h < Height; h++)
                {
                    for (var w = 0; w < Width; w++)
                    {
                        int pixelColor = Pixels[h, w];
                        var c = Color.FromArgb(pixelColor, pixelColor, pixelColor);
                        //var sb = new SolidBrush(c);
                        //gr.FillRectangle(sb, w, h, 1, 1);
                        bmap.SetPixel(w, h, c);
                    }
                }

                return bmap;
            }
        }

        public static ImageFile ReadFromFile(FileInfo file)
        {
            using (var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read))
            {
                return new ImageFile(stream, file.Name);
            }
        }

        public async Task WriteToFile(string file)
        {
            using (var stream = new FileStream(file, FileMode.OpenOrCreate, FileAccess.Write))
            {
                // TODO
                //await stream.WriteAsync(Bytes, 0, Bytes.Length);
            }
        }

        public static ImageFile operator -(ImageFile left, ImageFile right)
        {
            var ret = left.CopyBlack();
            for (var h = 0; h < ret.Height; h++)
            {
                for (var w = 0; w < ret.Width; w++)
                {
                    ret.Pixels[h, w] = (ushort) Math.Max(0, left.Pixels[h, w] - right.Pixels[h, w]);
                }
            }

            return ret;
        }
    }
}
