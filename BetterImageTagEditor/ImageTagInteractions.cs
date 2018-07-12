using System.Collections.Generic;
using System.Linq;

namespace BetterImageTagEditor
{
    public class ImageTagInteractions
    {
        public ImageTag InteractionTag { get; private set; }
        public HashSet<ImageTag> AffectedTags { get; set; } = new HashSet<ImageTag>();

        public ImageTagInteractions(ImageTag interactionTag)
        {
            InteractionTag = interactionTag;
        }
    }
}
