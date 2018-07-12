using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BetterImageTagEditor
{
    public partial class TagPanel : DockPanel, INotifyPropertyChanged
    {
        public Database DB;
        private string _autoCompleteText;
        public string AutoCompleteText
        {
            get
            {
                return _autoCompleteText;
            }
            set
            {
                _autoCompleteText = value;
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
        public List<ImageTag> CurrentTags { get; private set; }
        private ImageTagTypes _currentTagType = ImageTagTypes.Regular;
        public ImageTagTypes CurrentTagType
        {
            get
            {
                return _currentTagType;
            }
            set
            {
                _currentTagType = value;
                OnPropertyChanged("CurrentTagTypeString");
                OnPropertyChanged("CurrentTagTypeColour");
                OnPropertyChanged("CurrentTagTypeTooltip");
            }
        }
        public string CurrentTagTypeString
        {
            get
            {
                switch (CurrentTagType)
                {
                    case ImageTagTypes.Regular:
                        return "R";
                    case ImageTagTypes.Category:
                        return "C";
                    case ImageTagTypes.Modifier:
                        return "M";
                    case ImageTagTypes.Interaction:
                        return "I";
                    default:
                        return "";
                }
            }
        }
        public SolidColorBrush CurrentTagTypeColour
        {
            get
            {
                return new SolidColorBrush(TagTypeColour(CurrentTagType));
            }
        }
        public string CurrentTagTypeTooltip
        {
            get
            {
                return CurrentTagType.ToString();
            }
        }
        private string DragOriginTag;

        // Implementation of INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TagPanel()
        {
            InitializeComponent();
            DataContext = this;
        }

        // Set database location and current image
        // Must be called before using the panel
        public void Initialise(Database db, string imageHash)
        {
            this.DB = db;
            this.CurrentHash = imageHash;
            RedrawTags();
        }

        private void NewTagConfirm_Click(object sender, RoutedEventArgs e)
        {
            AddTag(NewTagName.Text);
        }

        private void NewTagName_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                // Add verbatim tag
                AddTag(NewTagName.Text);
            }
            else if (e.Key == System.Windows.Input.Key.Space)
            {
                // Add autocompleted tag
                AddTag(AutoCompleteText);
                // Ignore regular spacebar action
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Tab)
            {
                // Autocomplete tag name
                NewTagName.Text = AutoCompleteText;
                // Set caret to end of text
                NewTagName.CaretIndex = NewTagName.Text.Length;
                // Ignore regular tab action
                e.Handled = true;
            }
        }
        private void AddTag(string tagPath)
        {
            // Do not accept invalid tags
            if (!DB.IsValidTag(tagPath))
            {
                return;
            }

            // Add tag association to database
            DB.AssignTag(CurrentHash, tagPath, CurrentTagType);

            // Clear textbox
            NewTagName.Text = string.Empty;

            // Redraw tags
            RedrawTags();
        }

        private void DeleteTag_Click(object sender, RoutedEventArgs e)
        {
            // Get listbox item containing the delete button
            DockPanel parent = ((DockPanel)((Button)e.Source).Parent);

            if (TagList.SelectedItems.Contains(parent))
            {
                // Delete all selected tags
                for (int i = 0; i < TagList.SelectedItems.Count; i++)
                {
                    string tag = ((DockPanel)TagList.SelectedItems[i]).Tag.ToString();
                    RemoveTag(tag);
                }
            }
            else
            {
                // Delete just the tag whose delete button was clicked
                string tag = parent.Tag.ToString();
                RemoveTag(tag);
            }

            // Redraw tags
            RedrawTags();
        }
        private void RemoveTag(string tagPath)
        {
            // Remove tag association from database
            DB.UnassignTag(CurrentHash, tagPath);
        }

        private void RedrawTags()
        {
            // Clear tag list and tag input box
            TagList.Items.Clear();
            NewTagName.Text = string.Empty;
            AutoCompleteText = string.Empty;

            // Fetch tag list
            CurrentTags = DB.GetTags(CurrentHash);
            ImageTag RootTag = DB.RootTag;

            // Display tags
            DisplayTagTree(RootTag, 0);
        }

        private void DisplayTagTree(ImageTag tag, int level)
        {
            // TODO: Can likely be optimised
            if (tag.Type != ImageTagTypes.Root && CurrentTags.Contains(tag))
            {
                DisplayTag(tag, level);
            }

            foreach (ImageTag child in tag.Children)
            {
                DisplayTagTree(child, level + 1);
            }
        }

        private void DisplayTag(ImageTag tag, int level)
        {
            string tagName = tag.Name;

            // Create tag container
            DockPanel dockPanel = new DockPanel();
            dockPanel.Tag = tag.Path;
            Color bg = Colors.Transparent;
            if (tag.Type != ImageTagTypes.Regular)
            {
                bg = TagTypeColour(tag.Type);
            }
            else if (level % 2 == 0)
            {
                bg = Color.FromArgb(15, 255, 255, 255);
            }
            else
            {
                bg = Color.FromArgb(10, 0, 0, 0);
            }
            dockPanel.Background = new SolidColorBrush(bg);
            dockPanel.Margin = new Thickness(16 * level - 16, 0, 0, 0);

            // Create delete button
            Button delete = new Button
            {
                Content = "×",
                FontSize = 14,
                Width = 32,
                Margin = new Thickness(4, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Foreground = Brushes.WhiteSmoke,
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
            };
            delete.Click += DeleteTag_Click;
            DockPanel.SetDock(delete, Dock.Right);
            dockPanel.Children.Add(delete);

            // Create label containing the tag name
            Label nameLabel = new Label
            {
                // TODO: There is probably a better workaround than this
                Content = new TextBlock() { Text = tagName },
                Padding = new Thickness(4, 0, 4, 0),
                BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                Tag = tag.Path,
            };
            nameLabel.PreviewMouseDown += Tag_PreviewMouseDown;
            nameLabel.PreviewMouseUp += Tag_PreviewMouseUp;
            dockPanel.Children.Add(nameLabel);

            // Create labels containing added interaction tags
            IEnumerable<ImageTag> interactionList = DB.GetTagInteractions(CurrentHash, tag.Path);
            if (interactionList != null)
            {
                foreach (ImageTag interaction in interactionList)
                {
                    Label interactionLabel = new Label
                    {
                        // TODO: There is probably a better workaround than this
                        Content = new TextBlock() { Text = interaction.Name },
                        Padding = new Thickness(2, 0, 2, 0),
                        Margin = new Thickness(4, 0, 0, 0),
                        BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
                        Tag = interaction.Path
                    };
                    dockPanel.Children.Add(interactionLabel);
                }
            }

            // Create empty slot for an interaction tag
            Label emptyLabel = new Label
            {
                Width = 32,
                Padding = new Thickness(2, 0, 2, 0),
                Margin = new Thickness(4, 0, 0, 0),
                BorderBrush = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                Tag = tag.Path
            };
            emptyLabel.PreviewMouseDown += Tag_PreviewMouseDown;
            emptyLabel.PreviewMouseUp += Tag_PreviewMouseUp;
            dockPanel.Children.Add(emptyLabel);

            // Add tag to listbox
            TagList.Items.Add(dockPanel);
        }

        private void Tag_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DragOriginTag = ((DockPanel)((Label)sender).Parent).Tag.ToString();
            e.Handled = true;
        }
        private void Tag_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string DragTargetTag = ((DockPanel)((Label)sender).Parent).Tag.ToString();
            if (DragOriginTag != DragTargetTag)
            {
                DB.AddTagInteraction(CurrentHash, DragTargetTag, DragOriginTag);
                RedrawTags();
            }
        }

        private Color TagTypeColour(ImageTagTypes type)
        {
            switch (type)
            {
                case ImageTagTypes.Category:
                    return Color.FromArgb(50, 0, 0, 200);
                case ImageTagTypes.Modifier:
                    return Color.FromArgb(50, 100, 0, 100);
                case ImageTagTypes.Interaction:
                    return Color.FromArgb(50, 190, 200, 50);
                default:
                    return Colors.Transparent;
            }
        }

        private void NewTagName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NewTagName.Text))
            {
                AutoCompleteText = string.Empty;
            }
            else
            {
                AutoCompleteText = DB.AutoCompleteTag(NewTagName.Text);
            }
        }

        private void NewTagType_Click(object sender, RoutedEventArgs e)
        {
            if (TagList.SelectedItems.Count > 0)
            {
                // Update type of selected tags
                foreach (string tag in TagList.SelectedItems.Cast<DockPanel>().Select(dp => dp.Tag.ToString()))
                {
                    DB.ChangeTagType(tag, CurrentTagType);
                }
                RedrawTags();
            }
            else
            {
                // Cycle between tag types
                CurrentTagType = (ImageTagTypes)((int)(CurrentTagType - 1) % 4 + 2);
            }
        }

        private void ImportTags_Click(object sender, RoutedEventArgs e)
        {
            DB.ImportTags(CurrentHash);
            RedrawTags();
        }
    }
}
