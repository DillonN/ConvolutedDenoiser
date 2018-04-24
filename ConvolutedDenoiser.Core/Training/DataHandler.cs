using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConvNetSharp.Volume.GPU;
using ConvNetSharp.Volume.GPU.Single;

namespace ConvolutedDenoiser.Training
{
    public static class DataHandler
    {
        private const string Dir = @"..\..\..\images\";

        public static readonly DataLoader Test = new DataLoader();
        public static readonly DataLoader Train = new DataLoader();

        static DataHandler()
        {
            //BuilderInstance.Volume = new VolumeBuilder();
            //{
            //    Context = new GpuContext()
            //};
        }

        public static Task LoadTestData(double noise)
        {
            return Task.WhenAll(LoadAll(noise));
        }

        private static IEnumerable<Task> LoadAll(double noise)
        {
            var dir = Path.Combine(Environment.CurrentDirectory, Dir);
            yield return Test.Load(noise, Path.Combine(dir, "test"));
            yield return Train.Load(noise, Path.Combine(dir, "train"));
        }
    }
}
