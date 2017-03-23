using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TileGen.Generators;

namespace TileGen
{
    public class ViewModel : INotifyPropertyChanged
    {

        String outPath;

        public string OutPath
        {
            get
            {
                return outPath;
            }

            set
            {
                outPath = value;
                Properties.Settings.Default.OutPath = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        String path;

        public string Path
        {
            get
            {
                return path;
            }

            set
            {
                path = value;
                Properties.Settings.Default.Path = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public Visibility ProgressVisible
        {
            get
            {
                return progressVisible;
            }

            set
            {
                progressVisible = value;
                RaisePropertyChanged();
            }
        }
        int maxZoom;

        public string TileSet
        {
            get
            {
                return tileSet;
            }

            set
            {
                tileSet = value;
                Properties.Settings.Default.TileSet = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        public int Progress
        {
            set { }
            get
            {
                return progress;
            }

        }

        private void SetProgressText()
        {
            ProgressText = $"{Progress}/{MaxProgress}";
        }

        public int MaxProgress
        {
            get
            {
                return maxProgress;
            }

            set
            {
                maxProgress = value;
                RaisePropertyChanged();
            }
        }

        public string ProgressText
        {
            get { return progressText; }
            set
            {
                progressText = value;
                RaisePropertyChanged();
            }
        }

        public int MaxZoom
        {
            get
            {
                return maxZoom;
            }

            set
            {
                maxZoom = value;
                Properties.Settings.Default.Zoom = value;
                Properties.Settings.Default.Save();
                RaisePropertyChanged();
            }
        }

        string progressText;
        int maxProgress;

        int progress;

        string tileSet;

        Visibility progressVisible = Visibility.Collapsed;

        void RaisePropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        internal void AddToProgress(int dy)
        {
            System.Threading.Interlocked.Add(ref progress, dy);
            RaisePropertyChanged("Progress");
            SetProgressText();
        }

        internal void ResetProgress()
        {
            progress = 0;
            RaisePropertyChanged("Progress");
        }

       
        public BindingList<GeneratorProfile> Methods { get; set; }

        public GeneratorProfile SelectedGenerator
        {
            get
            {
                return selectedGenerator;
            }

            set
            {
                selectedGenerator = value;
                RaisePropertyChanged();
            }
        }

        private GeneratorProfile selectedGenerator;
     
    }

    public class GeneratorProfile {
       public String Name { get; }
       public IGenerator Generator { get; }

        public GeneratorProfile(string name, IGenerator generator) {
            Name = name;
            Generator = generator;
        }
    }
}
