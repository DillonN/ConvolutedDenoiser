using ConvNetSharp.Volume;
using ConvNetSharp.Volume.GPU.Single;
using ConvolutedDenoiser.Image.Loading;
using ConvolutedDenoiser.Image.Manipulation;
using ConvolutedDenoiser.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ConvolutedDenoiser.Training
{
    public class DataLoader : INotifyPropertyChanged
    {
        public ObservableCollection<ImageFile> Images { get; private set; } = new ObservableCollection<ImageFile>();
        public ObservableCollection<ImageFile> NoisedImages { get; private set; } = new ObservableCollection<ImageFile>();

        public ObservableCollection<ImageFile> ResultImages { get; private set; } = new ObservableCollection<ImageFile>();

        public int Width;
        public int Height;
        public ushort MaxGrayVal;

        private int _start;
        private int _epoch;

        public async Task Load(double noise, string directory)
        {
            if (Images.Count <= 0)
            {
                Images = new ObservableCollection<ImageFile>(await ImageLoader.LoadImagesFromDir(directory));
            }

            if (Images.Count > 0)
            {
                Width = Images[0].Width;
                Height = Images[0].Height;
                MaxGrayVal = Images[0].MaxGrayVal;
            }

            // Shuffle images
            if (_start == 0 && _epoch == 0)
            {
                var rand = new Random();

                for (var i = Images.Count - 1; i >= 0; i--)
                {
                    var j = rand.Next(i);
                    var temp = Images[j];
                    Images[j] = Images[i];
                    Images[i] = temp;
                }
            }

            var noiser = new Noiser(noise);
            NoisedImages = new ObservableCollection<ImageFile>(await noiser.NoiseImagesAsync(Images));

            ResultImages = new ObservableCollection<ImageFile>();

            OnPropertyChanged(nameof(Images));
            OnPropertyChanged(nameof(NoisedImages));
            OnPropertyChanged(nameof(ResultImages));
        }

        internal Batch NextBatch(int batchSize)
        {
            var dataShape = new Shape(Width, Height, 1, batchSize);
            var labelShape = new Shape(dataShape);

            var data = new float[dataShape.TotalLength];
            var label = new float[labelShape.TotalLength];
            
            var dataVolume = BuilderInstance.Volume.From(data, dataShape);
            var labelVolume = BuilderInstance.Volume.From(label, labelShape);

            var yf = new List<ImageFile>();
            var xf = new List<ImageFile>();
            var vf = new List<ImageFile>();

            for (var i = 0; i < batchSize; i++)
            {
                var image = Images[_start];
                var nImage = NoisedImages[_start];
                var vImage = NoisedImages[_start] - Images[_start];

                xf.Add(nImage);
                yf.Add(image);
                vf.Add(vImage);

                for (var w = 0; w < Width; w++)
                {
                    for (var h = 0; h < Height; h++)
                    {
                        dataVolume.Set(w, h, 0, i, nImage.Pixels[h, w] / (float) nImage.MaxGrayVal);
                        var justNoise = (nImage.Pixels[h, w] - image.Pixels[h, w]) / (float) image.MaxGrayVal;
                        labelVolume.Set(w, h, 0, i, justNoise);
                    }
                }
                
                if (++_start == Images.Count)
                {
                    _start = 0;
                    _epoch++;
                    Console.WriteLine($"Epoch #{_epoch}");
                }
            }

            return new Batch(dataVolume, labelVolume, xf, yf, vf);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
