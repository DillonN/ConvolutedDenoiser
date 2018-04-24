using ConvolutedDenoiser.Image.Enums;
using ConvolutedDenoiser.Image.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace ConvolutedDenoiser.Image.Tests.Loading
{
    [TestClass]
    public class ImageFileTest
    {
        private const string ImagePath =
            @"C:\Users\dillo\code\ConvolutedDenoiser\ConvolutedDenoiser\bin\Debug\images\test\1.pgm";

        [TestMethod]
        public void ReadFromFile_ShouldReadCorrectly()
        {
            var info = new FileInfo(ImagePath);
            var image = ImageFile.ReadFromFile(info);
            //Assert.AreEqual(262204, image.Bytes.Length);
            Assert.AreEqual(ImageFileType.Pgm, image.Type);
            Assert.AreEqual(512, image.Width);
            Assert.AreEqual(512, image.Height);
            Assert.AreEqual(255, image.MaxGrayVal);
            Assert.AreEqual(1, image.SizeOfPixel);
        }
    }
}
 