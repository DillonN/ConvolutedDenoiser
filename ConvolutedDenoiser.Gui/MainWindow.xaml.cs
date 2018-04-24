using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ConvolutedDenoiser.Training;

namespace ConvolutedDenoiser.Gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Trainer _trainer;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = DataHandler.Train;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;

            if (!double.TryParse(NoiseAmountTextBox.Text, out var noise)
                || noise < 0 || noise > 1)
            {
                noise = 0.3;
            }
            await DataHandler.LoadTestData(noise);

            _trainer = new Trainer();

            DataContext = _trainer;

            StopButton.IsEnabled = true;

            await _trainer.TrainLoop();
            StartButton.IsEnabled = true;
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _trainer.Running = false;
            //StopButton.IsEnabled = false;
        }
    }
}
