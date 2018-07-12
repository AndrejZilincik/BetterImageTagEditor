using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BetterImageTagEditor
{
    public class ImageSource : INotifyPropertyChanged
    {
        private string _path;
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                _path = value;
                OnPropertyChanged();
            }
        }
        private bool _isActive;
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                _isActive = value;
                OnPropertyChanged();
            }
        }
        private bool _includeSubfolders;
        public bool IncludeSubfolders
        {
            get
            {
                return _includeSubfolders;
            }
            set
            {
                _includeSubfolders = value;
                OnPropertyChanged();
            }
        }
        private bool _isLocal;
        public bool IsLocal
        {
            get
            {
                return _isLocal;
            }
            set
            {
                _isLocal = value;
                OnPropertyChanged();
            }
        }

        
        // Implementation of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ImageSource(string path, bool isActive = true, bool includeSubfolders = false, bool isLocal = true)
        {
            this.Path = path;
            this.IsActive = isActive;
            this.IncludeSubfolders = includeSubfolders;
            this.IsLocal = isLocal;
        }
    }
}
