using MapInfo.RasterEngine.IO;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TileGen.Generators;

namespace TileGen
{



    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ViewModel viewModel = new ViewModel();
        

        public MainWindow()
        {
            InitializeComponent();
            viewModel.Methods = new BindingList<GeneratorProfile>();
           
            viewModel.Methods.Add(new GeneratorProfile("Random Iterator", new RandomIteratorGenerator()));
            viewModel.Methods.Add(new GeneratorProfile("Block Reader", new BlockReaderGenerator()));
            this.DataContext = viewModel;
            viewModel.Path = Properties.Settings.Default.Path;
            viewModel.OutPath = Properties.Settings.Default.OutPath;
            viewModel.TileSet = Properties.Settings.Default.TileSet;
            viewModel.MaxZoom = Properties.Settings.Default.Zoom;
            
        }

        private async void btnGo_Click(object sender, RoutedEventArgs e)
        {

            var outDir = Path.Combine(viewModel.OutPath , viewModel.TileSet);
            bool writeTiles = true;
            if (Directory.Exists(outDir))
            {
                if (MessageBox.Show(Properties.Resources.DeleteFolder, Properties.Resources.FolderExists, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    Directory.Delete(outDir, true);
                    Directory.CreateDirectory(outDir);

                }
                else
                {
                    MessageBox.Show(Properties.Resources.JobCancelled, Properties.Resources.Cancelled);
                    writeTiles = false;
                }

            }
            if(writeTiles){
                await WriteTiles(outDir);
            }
           

        }

        private async Task WriteTiles(string outDir)
        {
           
            var srcMRR = viewModel.Path;
            this.IsEnabled = false;
            var tileGenerator = viewModel.SelectedGenerator.Generator;
            await Task.Run(() =>
            {
                Stopwatch s = new Stopwatch();
                s.Start();
                int maxZoom, totalTiles;
                IRasterDataset dataSet = RasterDatasetFactory.Open(srcMRR);
                tileGenerator.GetJobSize(dataSet, viewModel.MaxZoom > 0 ? viewModel.MaxZoom : 100, out maxZoom, out totalTiles);
                viewModel.MaxProgress = totalTiles;
                viewModel.ProgressVisible = Visibility.Visible;
                viewModel.ResetProgress();
                System.Progress<int> progress = new Progress<int>((i) => viewModel.AddToProgress(1));
                var output = new CompressedFileOutput(outDir);
                tileGenerator.CreateAllTiles(dataSet, maxZoom,output , progress);
                CreateMissingTopLevelTile(outDir, output);
                s.Stop();
                Console.WriteLine(s.Elapsed);
            });
            this.IsEnabled = true;
            viewModel.ProgressVisible = Visibility.Collapsed;
        }

        private static void CreateMissingTopLevelTile(string outDir, IOutput output)
        {
            if (!Directory.Exists(Path.Combine(outDir, @"0\0")))
            {
                output.WriteTile(0, 0, 0, new byte[(65 * 65 * 2) + 2]);
            }
            if (!Directory.Exists(Path.Combine(outDir, @"0\1")))
            {
                output.WriteTile(0, 1, 0, new byte[(65 * 65 * 2) + 2]);
            }
        }

      

  
        private void btnChooseMRRInput_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                viewModel.Path = (openFileDialog.FileName);
        }

        private void output_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                viewModel.OutPath = dialog.SelectedPath;
            }
        }

        private void generateSelected(object sender, SelectionChangedEventArgs e)
        {
            btnGo.IsEnabled = true;
        }
    }
}
