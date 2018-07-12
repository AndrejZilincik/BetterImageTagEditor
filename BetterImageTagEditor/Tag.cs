using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterImageTagEditor
{
    public enum TagTypes { None, Category, Regular, Special }

    public class Tag
    {
        public string Name { get; private set; }
        public TagTypes Type { get; private set; }
        public string Parent { get; private set; }
        public List<string> TaggedImages { get; private set; } = new List<string>();

        public Tag(TagTypes type, string name, string parent)
        {
            this.Type = type;
            this.Name = name;
            this.Parent = parent;
        }

        public void AddTaggedImage(string image)
        {
            TaggedImages.Add(image);
        }

        public void RemoveTaggedImage(string image)
        {
            TaggedImages.Remove(image);
        }
    }
}
