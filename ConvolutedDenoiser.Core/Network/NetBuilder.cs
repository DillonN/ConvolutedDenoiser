using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers.Single;

namespace ConvolutedDenoiser.Network 
{
    internal static class NetBuilder 
    {
        public static Net<float> Build(int width, int height, int batchSize)
        {
            var net = new Net<float>();

            net.AddLayer(new InputLayer(width, height, 1));
            // 0.5*((in - 1) * stride + width - in) = pad
            // pad = 0.5*(width + in * stride - stride)
            // With stride = width = 3, pad = in
            net.AddLayer(ConvFromInput(3, width, 1, 64));
            net.AddLayer(new ReluLayer());
            net.AddLayer(ConvFromInput(3, width, 1, 64));
            net.AddLayer(new ReluLayer());
            //net.AddLayer(new DropoutLayer(0.3f));
            //net.AddLayer(ConvFromInput(3, width, 1, 64));
            //net.AddLayer(new ReluLayer());
            //net.AddLayer(new DropoutLayer(0.3f));
            //net.AddLayer(new ConvLayer(2, 2, 16) { Stride = 1 });
            net.AddLayer(ConvFromInput(3, width, 1, 64));
            net.AddLayer(new ReluLayer());
            //net.AddLayer(new DropoutLayer(0.3f));
            //net.AddLayer(new FullyConnLayer(width * height));
            //net.AddLayer(new ReluLayer());
            net.AddLayer(ConvFromInput(3, width, 1, 1));
      
            //net.AddLayer(new ReluLayer());
            net.AddLayer(new RegressionLayer());

            return net;
        }

        private static ConvLayer ConvFromInput(int size, int inSize, int stride, int filter)
        {
            return new ConvLayer(size, size, filter) { Stride = stride, Pad = Pad(size, inSize, stride) };
        }

        private static PoolLayer PoolFromInput(int size, int inSize, int stride)
        {
            return new PoolLayer(size, size) { Stride = stride, Pad = Pad(size, stride, inSize) };
        }

        private static int Pad(int size, int inSize, int stride)
        {
            return (size + stride * (inSize - 1) - inSize) / 2;
        }
    }
}
