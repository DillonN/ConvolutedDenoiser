using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;
using ConvolutedDenoiser.Image.Loading;

namespace ConvolutedDenoiser.Models
{
    internal class Batch
    {
        public Volume<float> X;
        public Volume<float> Y;
        public List<ImageFile> XF;
        public List<ImageFile> YF;
        public List<ImageFile> VF;
        public int[] Labels;

        public Batch(Volume<float> x, Volume<float> y, List<ImageFile> xf, List<ImageFile> yf, List<ImageFile> vf)
        {
            X = x;
            Y = y;
            XF = xf;
            YF = yf;
            VF = vf;
        }
    }
}
