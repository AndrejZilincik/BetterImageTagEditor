using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace BetterImageTagEditor
{
    public partial class SourcesPanel : DockPanel, INotifyPropertyChanged
    {
        private ObservableCollection<ImageSource> _currentSources;
        public ObservableCollection<ImageSource> CurrentSources
        {
            get
            {
                return _currentSources;
            }
            set
            {
                _currentSources = value;
                OnPropertyChanged();
            }
        }


        // Implementation of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SourcesPanel()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void SetSources(List<ImageSource> currentSources)
        {
            CurrentSources = new ObservableCollection<ImageSource>(currentSources);
        }

        private void Active_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            ImageSource source = (ImageSource)checkBox.Tag;
            source.IsActive = true;
        }

        private void Active_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            ImageSource source = (ImageSource)checkBox.Tag;
            source.IsActive = false;
        }

        private void Subfolders_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            ImageSource source = (ImageSource)checkBox.Tag;
            source.IncludeSubfolders = true;
        }

        private void Subfolders_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            ImageSource source = (ImageSource)checkBox.Tag;
            source.IncludeSubfolders = false;
        }

        private void NewLocalSource_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Select folder not file
            // Setup and display an OpenFileDialog
            OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Select a folder to open",
                Filter = "All files (*.*)|*.*",
                CheckFileExists = false,
                CheckPathExists = false
            };

            if (ofd.ShowDialog() == true)
            {
                // Add folder to sources
                string dirName = Path.GetDirectoryName(ofd.FileName);
                CurrentSources.Add(new ImageSource(dirName));
            }
        }

        private void NewWebSource_Click(object sender, RoutedEventArgs e)
        {
            // Toggle input panel visibility
            WebSourceInput.Visibility = (WebSourceInput.Visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;

            // Reset help text
            WebSourceKeywords.Text = "Enter search keywords...";
        }

        private void RemoveSource_Click(object sender, RoutedEventArgs e)
        {
            // Remove selected sources
            while (SourceDisplay.SelectedItems.Count > 0)
            {
                ImageSource source = (ImageSource)SourceDisplay.SelectedItems[0];
                CurrentSources.Remove(source);
            }
        }

        private void WebSourceKeywords_GotFocus(object sender, RoutedEventArgs e)
        {
            // Auto-clear help text
            if (WebSourceKeywords.Text == "Enter search keywords...")
            {
                WebSourceKeywords.Text = string.Empty;
            }
        }

        private void WebSourceKeywords_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddWebSource();
            }
        }

        private void WebSourceConfirm_Click(object sender, RoutedEventArgs e)
        {
            AddWebSource();
        }

        private void AddWebSource()
        {
            // Build "path" string
            string path = string.Empty;
            switch (WebSourceSite.SelectedIndex)
            {
                case 0:
                    path = $"[e621]:\"{WebSourceKeywords.Text}\"";
                    break;
            }

            // Add source
            CurrentSources.Add(new ImageSource(path, true, false, false));

            // Hide input panel
            WebSourceInput.Visibility = Visibility.Collapsed;
        }
    }
}
