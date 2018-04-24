using ConvolutedDenoiser.Image.Loading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace ConvolutedDenoiser.Image.Tests.Loading
{
    [TestClass]
    public class ImageLoaderTest
    {
        private const string Directory =
            @"C:\Users\dillo\code\ConvolutedDenoiser\ConvolutedDenoiser\bin\Debug\images\test";
        [TestMethod]
        public async Task LoadImages_ShouldGetAll()
        {
            var images = await ImageLoader.LoadImagesFromDir(Directory);
            Assert.AreEqual(12, images.Count);
        }
    }
}
