using System.Collections.Generic;

namespace BetterImageTagEditor
{
    public enum ImageTagTypes { None, Root, Regular, Category, Modifier, Interaction }

    public class ImageTag
    {
        public string Name { get; private set; }
        public string Path { get; private set; }
        public ImageTagTypes Type { get; set; }
        public ImageTag Parent { get; private set; }
        public List<ImageTag> Children { get; set; } = new List<ImageTag>();
        public List<Image> TaggedImages { get; set; } = new List<Image>();
        
        // Create a blank, unconnected root tag
        public ImageTag()
        {
            InitialiseTag("", null, ImageTagTypes.Root);
        }

        // Create a child tag underneath the specified parent tag
        public ImageTag(string name, ImageTag parent, ImageTagTypes type = ImageTagTypes.Regular)
        {
            InitialiseTag(name, parent, type);
        }

        public void InitialiseTag(string name, ImageTag parent, ImageTagTypes type = ImageTagTypes.Regular)
        {
            this.Name = name;
            this.Type = type;
            this.Parent = parent;
            if (Parent != null)
            {
                Parent.Children.Add(this);
            }

            // Set tag path
            if (parent == null || parent.Type == ImageTagTypes.Root)
            {
                this.Path = Name;
            }
            else
            {
                this.Path = $"{parent.Path}:{Name}";
            }
        }
    }
}
