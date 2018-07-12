using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Windows.Controls.Primitives;

namespace BetterImageTagEditor
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public Database DB;

        // Directory where program data is stored
        private string AppDataDir;

        private int ItemsPerPage = 24;
        
        private string _currentFile;
        public string CurrentFile
        {
            get
            {
                return _currentFile;
            }
            set
            {
                _currentFile = value;
                OnPropertyChanged();
            }
        }

        private string _currentHash;
        public string CurrentHash
        {
            get
            {
                return _currentHash;
            }
            set
            {
                _currentHash = value;
                OnPropertyChanged();
            }
        }

        public List<ImageSource> CurrentSources { get; private set; } = new List<ImageSource>();
        public List<Image> CurrentImages { get; private set; } = new List<Image>();

        
        // Implementation of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Load data on opening, save data on closing
            LoadData();
            this.Closing += (s, e) => SaveData();
        }

        // Load program data from file
        private void LoadData()
        {
            // Set app data folder
            AppDataDir = Path.Combine(Environment.CurrentDirectory, "portable");
            bool firstStart = !Directory.Exists(AppDataDir);
            if (firstStart)
            {
                AppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BITE");
                firstStart = !Directory.Exists(AppDataDir);
            }

            // Set log file path
            Log.SetLogPath(Path.Combine(AppDataDir, "BITELog.txt"));

            // Initialise database
            DB = new Database(AppDataDir);

            // Make sure app data folders exist
            Directory.CreateDirectory(AppDataDir);
            Directory.CreateDirectory(Path.Combine(AppDataDir, "images"));
            Directory.CreateDirectory(Path.Combine(AppDataDir, "thumbs"));

            if (!firstStart)
            {
                // Load database data from file
                DB.LoadAllData();

                // Load sources from file
                LoadSources();
                OnSourcesModified();
                
                // TODO: Also load preferences and other data
            }
        }

        // Load sources from file
        private void LoadSources()
        {
            // Clear sources list
            CurrentSources.Clear();

            // Check if source file exists
            string sourcesFile = Path.Combine(AppDataDir, "sources.txt");
            if (!File.Exists(sourcesFile))
            {
                return;
            }

            // Read tag data from file
            using (StreamReader sr = new StreamReader(File.OpenRead(sourcesFile)))
            {
                string line = sr.ReadLine();
                while (!string.IsNullOrWhiteSpace(line))
                {
                    string[] split = line.Split(' ');
                    CurrentSources.Add(new ImageSource(split[3], bool.Parse(split[0]), bool.Parse(split[1]), bool.Parse(split[2])));

                    line = sr.ReadLine();
                }
            }
        }

        // Save program data to file
        private void SaveData()
        {
            // Save database data to file
            DB.SaveAllData();

            // Save sources to file
            SaveSources();

            // TODO: Also save preferences and other data
        }

        // Save sources to file
        private void SaveSources()
        {
            string sourcesFile = Path.Combine(AppDataDir, "sources.txt");
            using (StreamWriter sw = new StreamWriter(File.Create(sourcesFile)))
            {
                foreach (ImageSource source in CurrentSources)
                {
                    sw.WriteLine($"{source.IsActive} {source.IncludeSubfolders} {source.IsLocal} {source.Path}");
                }
            }
        }

        // Actions to perform when the source list is modified
        private void OnSourcesModified()
        {
            CurrentImages.Clear();
            foreach (ImageSource source in CurrentSources)
            {
                if (!source.IsActive)
                {
                    continue;
                }

                if (source.IsLocal)
                {
                    AddFilesFromDir(source.Path, source.IncludeSubfolders);
                }
                else
                {
                    string[] parts = source.Path.Split('"');
                    string sourceName = parts[0];
                    string searchQuery = parts[1];
                    if (sourceName == "[e621]:")
                    {
                        CurrentImages.AddRange(WebAPIs.E6GetImages(DB, searchQuery, ItemsPerPage));
                    }
                    //else if (sourceName == "...")
                    // TODO: Handle other types of web sources as well
                }
            }

            // TODO: Sort image list

            RedrawGallery();
        }

        private void AddFilesFromDir(string path, bool includeSubfolders)
        {
            foreach (string file in Directory.EnumerateFileSystemEntries(path))
            {
                string fileName = Path.GetFullPath(file);
                // TODO: Support additional file types (+folders in gallery view?)

                if (includeSubfolders && Directory.Exists(fileName))
                {
                    // Path is a subfolder, add files from folder if setting is enabled
                    AddFilesFromDir(fileName, true);
                }
                else if (Path.GetExtension(fileName) == ".jpg" || Path.GetExtension(fileName) == ".jpeg" || Path.GetExtension(fileName) == ".png")
                {
                    // Add image file
                    Image image = DB.AddImageFromLocation(fileName);
                    CurrentImages.Add(image);
                }
            }
        }

        // Refresh the gallery panel
        private void RedrawGallery()
        {
            // Clear all images
            GalleryDisplay.Items.Clear();

            // Add individual images
            foreach (Image image in CurrentImages)
            {
                string imageFile = image.Locations.First();

                ListBoxItem item = new ListBoxItem();
                item.Tag = imageFile;
                if (Layout.Value == 0)
                {
                    // Text only view
                    item.Content = imageFile;
                }
                else
                {
                    // Thumbnail view
                    item.Height = 50 + 50 * Layout.Value;
                    item.Width = 50 + 50 * Layout.Value;
                    var thumbnail = new System.Windows.Controls.Image();
                    string thumbFile = image.ThumbLocation ?? imageFile;
                    thumbnail.Source = new BitmapImage(new Uri(thumbFile));
                    item.Content = thumbnail;
                }
                GalleryDisplay.Items.Add(item);
            }
        }

        // Actions to perform when the user opens a file
        private void OnOpenFile()
        {
            string path = ((ListBoxItem)GalleryDisplay.SelectedItem).Tag.ToString();
            CurrentFile = path;
            CurrentHash = Image.ComputeFileMD5(CurrentFile);
            MainTagPanel.Initialise(DB, CurrentHash);
            MainTabControl.SelectedIndex = 1;
        }

        private void GalleryDisplay_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OnOpenFile();
        }

        // Actions to perform when the manage sources button is clicked
        private void ManageSources_Click(object sender, RoutedEventArgs e)
        {
            if (((ToggleButton)sender).IsChecked == true)
            {
                // Display source selection panel
                MainSourcesPanel.Visibility = Visibility.Visible;
                MainSourcesPanel.SetSources(CurrentSources);

                // Hide gallery panel
                GalleryDisplay.Visibility = Visibility.Collapsed;
                GalleryTabRightPanel.Visibility = Visibility.Collapsed;

                // Change button text
                ManageSources.Content = "🡸";
            }
            else
            {
                // Update source list
                CurrentSources = MainSourcesPanel.CurrentSources.ToList();

                // Hide source selection panel
                MainSourcesPanel.Visibility = Visibility.Collapsed;

                // Display gallery panel
                GalleryDisplay.Visibility = Visibility.Visible;
                GalleryTabRightPanel.Visibility = Visibility.Visible;

                // Change button text
                ManageSources.Content = "Manage sources...";

                // Load file list
                OnSourcesModified();
            }
        }
        
        private void Layout_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RedrawGallery();
        }

        private void Rating_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            DB.SetRating(CurrentHash, (int)Rating.Value);
        }

        private void ManageSubstitutions_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Children.Clear();
            SettingsPanel.Children.Add(new SubstitutionPanel(DB));
        }

        private void ManageImplications_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Children.Clear();
            SettingsPanel.Children.Add(new ImplicationPanel(DB));
        }
    }
}
