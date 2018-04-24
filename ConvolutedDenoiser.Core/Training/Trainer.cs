using ConvNetSharp.Core;
using ConvNetSharp.Core.Training;
using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU;
using ConvNetSharp.Volume.GPU.Single;
using ConvolutedDenoiser.Image.Loading;
using ConvolutedDenoiser.Network;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ConvolutedDenoiser.Training
{
    public class Trainer : INotifyPropertyChanged
    {
        private const int BatchSize = 16;

        private static ushort MaxGray => DataHandler.Train?.MaxGrayVal ?? 0;

        public List<ImageFile> Noise { get; private set; } = new List<ImageFile>();
        public List<ImageFile> Results { get; private set; } = new List<ImageFile>();
        public List<ImageFile> Images { get; private set; } = new List<ImageFile>();
        public List<ImageFile> NoisedImages { get; private set; } = new List<ImageFile>();
        public List<ImageFile> NoiseSource { get; private set; } = new List<ImageFile>();

        public float Loss { get; private set; }
        public int Iteration { get; private set; }
        public string Time { get; private set; } = "";
        public string RmseS => $"{Rmse / MaxGray:F4}";
        public string PsnrS => $"{Psnr:F4}dB";

        private double _rmse;

        private double Rmse
        {
            get => _rmse;
            set
            {
                _rmse = value;
                OnPropertyChanged(nameof(RmseS));
                OnPropertyChanged(nameof(PsnrS));
            }
        }

        private double Psnr => PsnrFromRmse(Rmse);
        
        private static string LogFileName => $"log_{DateTime.Now:yyyy-MM-dd-HH-mm}.csv";

        private TrainerBase<float> _trainer;
        private Net<float> _net;
        public bool Running;

        public Trainer()
        {

        }

        public Volume<float> Test(Volume<float> x, bool forward = true)
        {
            var res = new List<ImageFile>();

            if (forward)
            {
                return _net.Forward(x);
                //res = ConvertToImageFiles(result, x.Shape.GetDimension(0), x.Shape.GetDimension(1), "Results");
            }

            //var loss = _trainer.Loss;
            //Console.WriteLine(loss / (DataHandler.Train.Width * DataHandler.Train.Height));

            return null;
        }

        public Volume<float> Train(Volume<float> x, Volume<float> y)
        {
            _trainer.Train(x, y);
            return Test(x, true);
        }

        public Task TrainLoop()
        {
            Running = true;
            //var trainBatch1 = DataHandler.Train.NextBatch(_trainer.BatchSize);
            //Train(trainBatch1.X, trainBatch1.Y);

            return Task.Run(() =>
            {
                //BuilderInstance.Volume = new VolumeBuilder
                //{
                //    Context = new GpuContext(1)
                //};
                var width = DataHandler.Train.Width;
                var height = DataHandler.Train.Height;
                _net = NetBuilder.Build(width, height, BatchSize);

                _trainer = new SgdTrainer<float>(_net)
                {
                    LearningRate = 0.000001f,
                    Momentum = 0.9f,
                    BatchSize = BatchSize,
                    L2Decay = 0.001f
                };

                var logName = LogFileName;
                var testName = "test_" + LogFileName;
                File.AppendAllLines(logName, new[] {"Iteration,Loss,Time (ms),RMSE,PSNR (dB),Total Time (s)"});
                File.AppendAllLines(testName, new []{"Loss, RMSE, PSNR (dB), Total Time (s)"});

                var stopwatch = new Stopwatch();
                stopwatch.Start();

                double maxPsnr = 0;
                double minLoss = 0;

                while (Running)
                {
                    var trainBatch = DataHandler.Train.NextBatch(_trainer.BatchSize);
                    Images = trainBatch.YF;
                    NoisedImages = trainBatch.XF;

                    //Images = ConvertToImageFiles(trainBatch.Y.ReShape(1, 1, -1, Shape.Keep), width, height, "Source");
                    NoiseSource = ConvertToImageFiles(trainBatch.Y.ReShape(1, 1, -1, Shape.Keep), "NoiseSrc", 0.5f);

                    OnPropertyChanged(nameof(Images));
                    OnPropertyChanged(nameof(NoisedImages));
                    OnPropertyChanged(nameof(NoiseSource));

                    var startRmse = RootMeanSquaredError(Images, NoisedImages);
                    Console.WriteLine($"INITIAL\t RMSE: {startRmse}\tPSNR:{PsnrFromRmse(startRmse)}");

                    for (var i = 0; i < 200; i++)
                    {
                        var noise = Train(trainBatch.X, trainBatch.Y);
                        Noise = ConvertToImageFiles(noise, "Noise", 0.5f);
                        Results = ConvertToImageFiles(noise, "Result", 0, NoisedImages);
                        //Results = new List<ImageFile>();
                        //for (var j = 0; j < NoisedImages.Count; j++)
                        //{
                        //    Results.Add(NoisedImages[j] - Noise[j]);
                        //}
                        Loss = _trainer.Loss;
                        if (Loss < minLoss || minLoss == 0) minLoss = Loss;

                        var time = _trainer.UpdateWeightsTimeMs + _trainer.BackwardTimeMs + _trainer.ForwardTimeMs;
                        Time = $"{time:F4}ms";
                        Iteration = i;

                        Rmse = RootMeanSquaredError(Images, Results);
                        if (Psnr > maxPsnr) maxPsnr = Psnr;

                        var r = RootMeanSquaredError(Noise.Take(8).ToList(), Noise.Skip(8).ToList());

                        Console.WriteLine($"{i}: loss {Loss}\ttime: {Time}\trmse: {RmseS}\tpsnr:{PsnrS}\ttotaltime: {stopwatch.Elapsed.TotalSeconds}s\tmpsnr:{maxPsnr}dB\tmloss:{minLoss}");
                        File.AppendAllLines(logName, new []{ $"{i},{Loss},{time},{Rmse},{Psnr},{stopwatch.Elapsed.TotalSeconds}" });

                        OnPropertyChanged(nameof(Results));
                        OnPropertyChanged(nameof(Noise));
                        OnPropertyChanged(nameof(Loss));
                        OnPropertyChanged(nameof(Time));
                        OnPropertyChanged(nameof(Iteration));

                        if (!Running)
                        {
                            Running = true;
                            break;
                        }
                    }

                    var testBatch = DataHandler.Test.NextBatch(_trainer.BatchSize);
                    var testNoise = Test(testBatch.X);
                    Noise = ConvertToImageFiles(testNoise, "Noise", 0.5f);
                    Results = ConvertToImageFiles(testNoise, "Result", 0, trainBatch.XF);

                    Loss = _trainer.Loss;

                    Rmse = RootMeanSquaredError(testBatch.YF, Results);

                    Console.WriteLine($"Test: loss {Loss}\trmse: {RmseS}\tpsnr:{PsnrS}\ttotaltime: {stopwatch.Elapsed.TotalSeconds}s");
                    File.AppendAllLines(testName, new []{ $"{Loss},{Rmse},{Psnr},{stopwatch.Elapsed.TotalSeconds}" });
                }
            });
        }

        private static double RootMeanSquaredError(IReadOnlyList<ImageFile> images, IReadOnlyList<ImageFile> results)
        {
            var mse = 0d;
            var maxGray = (float) images.First().MaxGrayVal;
            var height = images.First().Height;
            var width = images.First().Width;

            for (var i = 0; i < images.Count; i++)
            {
                for (var h = 0; h < height; h++)
                {
                    for (var w = 0; w < width; w++)
                    {
                        mse += Math.Pow(images[i].Pixels[h, w] - results[i].Pixels[h, w], 2);
                    }
                }
            }

            return Math.Sqrt(mse / images.Count / height / width);
        }

        private static double PsnrFromRmse(double rmse)
        {
            return 20 * Math.Log10(MaxGray / rmse);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private List<ImageFile> ConvertToImageFiles(Volume<float> volume, string name, float offset = 0, IReadOnlyList<ImageFile> subtractFrom = null)
        {
            var res = new List<ImageFile>();
            var width = DataHandler.Train.Width;
            var height = DataHandler.Train.Height;
            volume = volume.ReShape(width, height, 1, Shape.Keep);
            //subtract = subtract?.ReShape(width, height, 1, Shape.Keep);

            for (var i = 0; i < volume.Shape.GetDimension(3); i++)
            {
                var pixels = new ushort[height, width];
                const ushort maxGrayVal = 255;

                for (var h = 0; h < height; h++)
                {
                    for (var w = 0; w < width; w++)
                    {
                        var pixel = volume.Get(w, h, 0, i);
                        if (subtractFrom != null)
                        {
                            pixel = subtractFrom[i].Pixels[h, w] / (float) maxGrayVal - pixel;
                        }

                        if (offset > 0) pixel *= 4;
                        pixel = Math.Min(1, Math.Max(0, pixel));
                        pixels[h, w] = (ushort) (maxGrayVal * pixel);
                    }
                }

                res.Add(new ImageFile(width, height, maxGrayVal, name, pixels));
            }

            return res;
        }
    }
}
