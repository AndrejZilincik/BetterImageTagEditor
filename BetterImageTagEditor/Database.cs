using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BetterImageTagEditor
{
    public class Database
    {
        public string DataDir { get; private set; }

        // A table of all currently used tags, tag path -> tag
        public Dictionary<string, ImageTag> TagTable { get; private set; }
        // The root tag of the database
        public ImageTag RootTag { get; private set; } = new ImageTag();
        // A table of all known images, MD5 hash -> image
        private Dictionary<string, Image> ImageTable;
        // A table of tag substitutions, string 1 (before) gets replaced by string 2 (after)
        public Dictionary<string, string> SubstitutionTable;
        // A table of tag implications, adding tag 1 (before) also adds tag 2 (after)
        public Dictionary<string, string> ImplicationTable;

        public Database(string dataDir)
        {
            this.DataDir = dataDir;
            ClearAllData();
        }

        public void ClearAllData()
        {
            TagTable = new Dictionary<string, ImageTag>();
            ImageTable = new Dictionary<string, Image>();
            SubstitutionTable = new Dictionary<string, string>();
            ImplicationTable = new Dictionary<string, string>();
            Log.WriteLine("Database cleared");
        }

        public bool ContainsImage(string imageHash)
        {
            return ImageTable.ContainsKey(imageHash);
        }

        private void AssertContainsImage(string imageHash, string errorMessage)
        {
            // Make sure image is in database
            if (!ContainsImage(imageHash))
            {
                Log.WriteLine(errorMessage, MessageType.Error);
                throw new ArgumentException(errorMessage);
            }
        }

        public bool ContainsTag(string tagPath)
        {
            return TagTable.ContainsKey(tagPath);
        }

        private void AssertContainsTag(string tagPath, string errorMessage)
        {
            // Make sure tag is in database
            if (!ContainsTag(tagPath))
            {
                Log.WriteLine(errorMessage, MessageType.Error);
                throw new ArgumentException(errorMessage);
            }
        }

        // Returns the tag at the specified tag path
        public ImageTag GetTag(string tagPath)
        {
            // Check if tag is present in tag table
            // TODO: Optimisation? Use tag table to search for tag along path (from specified tag -> root), then continue traversing down
            if (ContainsTag(tagPath))
            {
                return TagTable[tagPath];
            }

            // Start at root tag
            ImageTag tag = RootTag;

            // Attempt to navigate to specified tag
            String[] tagNames = tagPath.Split(':');
            foreach (String tagName in tagNames)
            {
                ImageTag child = tag.Children.Find(t => t.Name == tagName);
                if (child == null)
                {
                    string errorMessage = $"Tag matching path [{tag.Path}:{tagName}] not found";
                    Log.WriteLine(errorMessage, MessageType.Error);
                    errorMessage = $"Failed to locate tag [{tagPath}]";
                    Log.WriteLine(errorMessage, MessageType.Error);
                    throw new ArgumentException(errorMessage);
                }
                tag = child;
            }

            Log.WriteLine($"Located tag [{tagPath}]");
            return tag;
        }

        // Add a tag at the specified tag path
        public ImageTag AddTagAtPath(string path, ImageTagTypes type = ImageTagTypes.Regular)
        {
            // If tag already exists, only update type
            if (ContainsTag(path))
            {
                ChangeTagType(path, type);
                return TagTable[path];
            }

            // Start at root tag
            ImageTag tag = RootTag;

            // Navigate to parent tag
            String[] tagNames = path.Split(':');
            foreach (String tagName in tagNames.Take(tagNames.Length - 1))
            {
                ImageTag child = tag.Children.Find(t => t.Name == tagName);
                if (child == null)
                {
                    // Tag along path not found, display warning message
                    string errorMessage = $"Tag matching path [" +
                    ((tag.Type == ImageTagTypes.Root) ? "" : $"{tag.Path}:") + 
                    $"{tagName}] not found";
                    Log.WriteLine(errorMessage, MessageType.Warning);

                    // Create the missing tag
                    child = new ImageTag(tagName, tag);
                    Log.WriteLine($"Created tag [{child.Path}]");

                    // Add the tag to the tag table
                    TagTable.Add(child.Path, child);
                    Log.WriteLine($"Created tag table entry for tag [{child.Path}]");
                }
                tag = child;
            }

            // Create leaf tag under parent tag
            ImageTag leaf = new ImageTag(tagNames.Last(), tag, type);
            Log.WriteLine($"Created leaf tag [{leaf.Path}] with parent [{leaf.Parent.Path}]");

            // Add new tag to tag table
            TagTable.Add(path, leaf);
            Log.WriteLine($"Created tag table entry for tag [{leaf.Path}]");

            // Return newly created tag
            return leaf;
        }

        // Remove the tag at the specified tag path
        public void RemoveTagAtPath(string path)
        {
            // Get parent tag and leaf tag name
            ImageTag parent = RootTag;
            string name = path;
            if (path.Contains(':'))
            {
                int lastColon = path.LastIndexOf(':');
                name = path.Substring(lastColon + 1);
                String parentPath = path.Substring(0, lastColon);
                parent = GetTag(parentPath);
            }

            // Get leaf tag
            ImageTag tag = parent.Children.Find(t => t.Name == name);

            // Delete tag
            RemoveTag(tag);
        }

        // Remove the specified tag
        public void RemoveTag(ImageTag tag)
        {
            // Root tag can not be removed
            if (tag.Type == ImageTagTypes.Root)
            {
                return;
            }

            // Remove tag from parent
            tag.Parent.Children.Remove(tag);

            // Check if this makes the parent unused
            if (tag.Parent.TaggedImages.Count == 0 && tag.Parent.Children.Count == 0)
            {
                RemoveTag(tag.Parent);
            }

            // Delete tag
            // TODO: How though?
            string path = tag.Path;
            Log.WriteLine($"Deleted tag [{path}]");

            // Remove tag entry in tag table
            TagTable.Remove(path);
            Log.WriteLine($"Removed database entry for tag [{path}]");
        }

        // Assign a tag to an image
        public void AssignTag(string imageHash, string tagPath, ImageTagTypes tagType = ImageTagTypes.Regular)
        {
            // Get or create database entry for image
            if (!ImageTable.TryGetValue(imageHash, out Image image))
            {
                image = new Image(imageHash);
                Log.WriteLine($"Created image object for [{imageHash}]");
                ImageTable.Add(imageHash, image);
                Log.WriteLine($"Created database entry for image [{imageHash}]");
            }

            // Get or create database entry for tag
            if (!TagTable.TryGetValue(tagPath, out ImageTag tag))
            {
                tag = AddTagAtPath(tagPath, tagType);
            }

            // Check if tag is already assigned
            if (image.Tags.Contains(tag) || tag.TaggedImages.Contains(image))
            {
                Log.WriteLine($"Tag [{tagPath}] is already assigned to image [{imageHash}]", MessageType.Warning);
                return;
            }

            // Link image and tag
            image.Tags.Add(tag);
            tag.TaggedImages.Add(image);
            Log.WriteLine($"Tag [{tagPath}] assigned to image [{imageHash}]");

            // Assign ancestors as well
            if (tag.Parent.Type != ImageTagTypes.Root)
            {
                AssignTag(imageHash, tag.Parent.Path);
            }

            // Assign any implied tags
            if (ImplicationTable.ContainsKey(tagPath))
            {
                AssignTag(imageHash, ImplicationTable[tagPath]);
            }
        }

        public void UnassignTag(string imageHash, string tagPath)
        {
            // Make sure image and tag are in database
            AssertContainsImage(imageHash, $"Image [{imageHash}] not found in database, can not unassign tag [{tagPath}]!");
            AssertContainsTag(tagPath, $"Tag [{tagPath}] not found in database, can not unassign from image [{imageHash}]!");

            // Retrieve image and tag from database
            Image image = ImageTable[imageHash];
            ImageTag tag = TagTable[tagPath];

            // Remove links between image and tag
            image.Tags.Remove(tag);
            tag.TaggedImages.Remove(image);
            Log.WriteLine($"Tag [{tagPath}] removed from image [{imageHash}]");
            
            // Unassign tag's ancestors, if no longer needed
            if (tag.Parent != null && tag.Parent.Type != ImageTagTypes.Root &&
                !tag.Parent.Children.Any(c => image.Tags.Contains(c)) &&
                image.Tags.Contains(tag.Parent))
            {
                UnassignTag(imageHash, tag.Parent.Path);
            }

            // Also unassign all descendant tags
            for (int i = 0; i < tag.Children.Count; i++)
            {
                ImageTag child = tag.Children[i];
                if (image.Tags.Contains(child))
                {
                    UnassignTag(imageHash, child.Path);
                }
            }

            // Delete tag if no longer used
            if (tag.TaggedImages.Count == 0 && tag.Children.Count == 0)
            {
                Log.WriteLine($"Tag [{tagPath}] no longer used, deleting");
                RemoveTag(tag);
            }

        }

        // Get image from database
        public Image GetImage(string imageHash)
        {
            // Make sure image is in database
            AssertContainsImage(imageHash, $"Image [{imageHash}] not found in database!");

            return ImageTable[imageHash];
        }

        // Add image to database
        public void AddImage(string imageHash)
        {
            // If database already contains the image, do nothing
            if (ContainsImage(imageHash))
            {
                Log.WriteLine($"Image [{imageHash}] is already present in database", MessageType.Warning);
                return;
            }
            
            Image image = new Image(imageHash);
            ImageTable.Add(imageHash, image);
            Log.WriteLine($"Added image [{imageHash}] to database");
        }

        public Image AddImageFromLocation(string filePath)
        {
            // Load image from file
            Image image = Image.FromFile(filePath);

            // If database already contains the image, return that image
            if (ContainsImage(image.Hash))
            {
                Log.WriteLine($"Image [{image.Hash}] is already present in database", MessageType.Warning);

                // Add new location
                Image dbImage = ImageTable[image.Hash];
                if (dbImage.Locations.Add(filePath))
                {
                    Log.WriteLine($"Added location [{filePath}] to image [{image.Hash}]");
                }

                return dbImage;
            }

            // Add image to database
            ImageTable.Add(image.Hash, image);
            Log.WriteLine($"Added image [{image.Hash}] to database");
            return image;
        }

        public void DeleteImage(string imageHash)
        {
            // TODO: Do something if not found?
            if (ImageTable.TryGetValue(imageHash, out Image image))
            {
                ImageTable.Remove(imageHash);
                Log.WriteLine($"Removed image [{imageHash}] from database");
            }
        }

        public List<ImageTag> GetTags(string imageHash)
        {
            Image image = GetImage(imageHash);
            Log.WriteLine($"Fetched tags for image [{imageHash}]");
            return image.Tags;
        }

        public bool IsValidTag(string tagPath)
        {
            return (!string.IsNullOrEmpty(tagPath) && !tagPath.Contains(" "));
        }

        public void AddSubstitution(string before, string after)
        {
            if (SubstitutionTable.ContainsKey(before))
            {
                string errorMessage = $"Substitution table already contains an entry for [{before}]!";
                Log.WriteLine(errorMessage, MessageType.Error);
                throw new ArgumentException(errorMessage);
            }

            SubstitutionTable.Add(before, after);
            Log.WriteLine($"Added substitution from [{before}] to [{after}]");
        }

        public void RemoveSubstitution(string before)
        {
            SubstitutionTable.Remove(before);
            Log.WriteLine($"Removed substitution from [{before}]");
        }

        public void AddImplication(string before, string after)
        {
            if (ImplicationTable.ContainsKey(before))
            {
                string errorMessage = $"Implication table already contains an entry for [{before}]!";
                Log.WriteLine(errorMessage, MessageType.Error);
                throw new ArgumentException(errorMessage);
            }

            ImplicationTable.Add(before, after);
            Log.WriteLine($"Added implication from [{before}] to [{after}]");
        }

        public void RemoveImplication(string before)
        {
            ImplicationTable.Remove(before);
            Log.WriteLine($"Removed implication from [{before}]");
        }

        public string AutoCompleteTag(string tagText)
        {
            // Text matches a substitution
            if (SubstitutionTable.ContainsKey(tagText))
            {
                return SubstitutionTable[tagText];
            }

            // Get tags containing tag text
            int startsMinLevel = -1;
            string startsShortest = null;
            int containsMinLevel = -1;
            string containsShortest = null;
            foreach (ImageTag tag in TagTable.Values)
            {
                int tagLevel = tag.Path.Count(ch => ch == ':');
                if (tag.Name == tagText)
                {
                    // Tag name matches full text
                    return tag.Path;
                }
                else if (tag.Name.StartsWith(tagText))
                {
                    // Tag path starts with tag text, prioritise shallower tags
                    if (startsMinLevel == -1 || tagLevel < startsMinLevel)
                    {
                        startsShortest = tag.Path;
                        startsMinLevel = tagLevel;
                    }
                }
                else if (tag.Path.Contains(tagText))
                {
                    // Tag path contains tag text, prioritise shallower tags
                    if (containsMinLevel == -1 || tagLevel < containsMinLevel)
                    {
                        containsShortest = tag.Path;
                        containsMinLevel = tagLevel;
                    }
                }
            }

            if (startsMinLevel != -1)
            {
                return startsShortest;
            }
            else if (containsMinLevel != -1)
            {
                return containsShortest;
            }
            else
            {                
                // No matches found
                return tagText;
            }
        }

        public void SetRating(string imageHash, int rating)
        {
            // Make sure image is in database
            AssertContainsImage(imageHash, $"Unable to set rating for [{imageHash}], image not found in database!");

            // Set image rating
            ImageTable[imageHash].Rating = rating;
            Log.WriteLine($"Rating of image [{imageHash}] set to [{rating}]");
        }

        public void SaveAllData()
        {
            // Save tag data
            Log.WriteLine($"Saving tag data to file");
            SaveTags();

            // Save image data
            Log.WriteLine($"Saving image data to file");
            foreach (Image image in ImageTable.Values)
            {
                Log.WriteLine($"Saving image data for image [{image.Hash}]");
                SaveImage(image);
            }

            // Save substitution data
            Log.WriteLine($"Saving substitution data to file");
            SaveSubstitutions();

            // Save implication data
            Log.WriteLine($"Saving implication data to file");
            SaveImplications();
        }

        public void SaveTags()
        {
            PruneUnusedTags();

            string tagFile = Path.Combine(DataDir, "tags.txt");
            using (StreamWriter sw = new StreamWriter(File.Create(tagFile)))
            {
                foreach (ImageTag tag in TagTable.Values)
                {
                    sw.WriteLine($"{(int)tag.Type} {tag.Path}");
                }
            }
        }

        public void PruneUnusedTags()
        {
            List<ImageTag> unusedTags = new List<ImageTag>();
            foreach (ImageTag tag in TagTable.Values)
            {
                if (tag.TaggedImages.Count == 0 && tag.Children.Count == 0)
                {
                    unusedTags.Add(tag);
                }
            }

            foreach (ImageTag tag in unusedTags)
            {
                Log.WriteLine($"Pruning tag [{tag.Path}]");
                RemoveTag(tag);
            }
        }

        public void SaveImage(Image image)
        {
            // Open file for writing
            string dataFile = Path.Combine(DataDir, "images", image.Hash);
            using (StreamWriter sw = new StreamWriter(File.Create(dataFile)))
            {
                // Save rating
                sw.WriteLine($"rating: {image.Rating}");

                // Save tags
                if (image.Tags.Count > 0)
                {
                    string tags = string.Join(" ", image.Tags.Select(tag => tag.Path));
                    sw.WriteLine($"tags: {tags}");
                }

                // Save locations
                if (image.Locations.Count > 0)
                {
                    string locations = string.Join(" ", image.Locations);
                    sw.WriteLine($"locations: {locations}");
                }
            }
        }

        private void SaveSubstitutions()
        {
            string subFile = Path.Combine(DataDir, "tagsubs.txt");
            using (StreamWriter sw = new StreamWriter(File.Create(subFile)))
            {
                foreach (KeyValuePair<string, string> sub in SubstitutionTable)
                {
                    sw.WriteLine($"{sub.Key} {sub.Value}");
                }
            }
        }

        private void SaveImplications()
        {
            string implFile = Path.Combine(DataDir, "tagimpls.txt");
            using (StreamWriter sw = new StreamWriter(File.Create(implFile)))
            {
                foreach (KeyValuePair<string, string> sub in ImplicationTable)
                {
                    sw.WriteLine($"{sub.Key} {sub.Value}");
                }
            }
        }

        public void LoadAllData()
        {
            // Load tag data
            Log.WriteLine($"Loading tag data from file");
            LoadTags();

            // Load image data
            Log.WriteLine($"Loading image data from file");
            foreach (string dataFile in Directory.EnumerateFiles(Path.Combine(DataDir, "images")))
            {
                LoadImage(dataFile);
            }

            // Load substitution data
            Log.WriteLine($"Loading substitution data from file");
            LoadSubstitutions();

            // Load implication data
            Log.WriteLine($"Loading implication data from file");
            LoadImplications();
        }

        public void LoadTags()
        {
            // Check if tag file exists
            string tagFile = Path.Combine(DataDir, "tags.txt");
            if (!File.Exists(tagFile))
            {
                return;
            }

            // Read tag data from file
            using (StreamReader sr = new StreamReader(File.OpenRead(tagFile)))
            {
                string line = sr.ReadLine();
                while (!string.IsNullOrWhiteSpace(line))
                {
                    string[] split = line.Split(' ');
                    ImageTagTypes type = (ImageTagTypes)int.Parse(split[0]);
                    ImageTag tag = AddTagAtPath(split[1], type);

                    line = sr.ReadLine();
                }
            }
        }

        public void LoadImage(string dataFile)
        {
            // Create image object
            string imageHash = Path.GetFileName(dataFile);
            Log.WriteLine($"Loading image data for image [{imageHash}]");
            Image image = new Image(imageHash);
            ImageTable.Add(imageHash, image);

            // Read image data from file
            using (StreamReader sr = new StreamReader(File.OpenRead(dataFile)))
            {
                string line = sr.ReadLine();
                while (!string.IsNullOrWhiteSpace(line))
                {
                    if (line.StartsWith("rating: "))
                    {
                        // Read rating data
                        image.Rating = int.Parse(line.Substring(8));
                        Log.WriteLine($"Setting rating of image [{imageHash}] to [{image.Rating}]");
                    }
                    else if (line.StartsWith("tags: "))
                    {
                        // Read tag data
                        string tagString = line.Substring(6);
                        string[] tags = tagString.Split(' ');
                        foreach (string tagPath in tags)
                        {
                            AssignTag(imageHash, tagPath);
                        }
                    }
                    else if (line.StartsWith("locations: "))
                    {
                        // Read location data
                        string locString = line.Substring(11);
                        string[] locations = locString.Split(' ');
                        foreach (string location in locations)
                        {
                            image.Locations.Add(location);
                        }
                    }
                    line = sr.ReadLine();
                }
            }
        }

        public void LoadSubstitutions()
        {
            // Check if sub file exists
            string subFile = Path.Combine(DataDir, "tagsubs.txt");
            if (!File.Exists(subFile))
            {
                return;
            }

            // Read sub data from file
            using (StreamReader sr = new StreamReader(File.OpenRead(subFile)))
            {
                string line = sr.ReadLine();
                while (!string.IsNullOrWhiteSpace(line))
                {
                    string[] split = line.Split(' ');
                    AddSubstitution(split[0], split[1]);

                    line = sr.ReadLine();
                }
            }
        }

        public void LoadImplications()
        {
            // Check if implication file exists
            string implFile = Path.Combine(DataDir, "tagimpls.txt");
            if (!File.Exists(implFile))
            {
                return;
            }

            // Read implication data from file
            using (StreamReader sr = new StreamReader(File.OpenRead(implFile)))
            {
                string line = sr.ReadLine();
                while (!string.IsNullOrWhiteSpace(line))
                {
                    string[] split = line.Split(' ');
                    AddImplication(split[0], split[1]);

                    line = sr.ReadLine();
                }
            }
        }

        public void ChangeTagType(string tagPath, ImageTagTypes newType)
        {
            // Make sure tag is in database
            AssertContainsTag(tagPath, $"Unable to change tag type of [{tagPath}], tag not found in database!");

            // Change tag type
            TagTable[tagPath].Type = newType;
        }

        public void AddTagInteraction(string imageHash, string interactionTagPath, string affectedTagPath)
        {
            // Make sure image and tags are in database
            AssertContainsImage(imageHash, $"Unable to add tag interaction, image {imageHash} not found in database!");
            AssertContainsTag(interactionTagPath, $"Unable to add tag interaction, tag {interactionTagPath} not found in database!");
            AssertContainsTag(affectedTagPath, $"Unable to add tag interaction, tag {affectedTagPath} not found in database!");
            
            // Retrieve image and tag from database
            Image image = ImageTable[imageHash];
            ImageTag interactionTag = TagTable[interactionTagPath];
            ImageTag affectedTag = TagTable[affectedTagPath];

            // TODO: Rework interaction tags then modify + reenable this part
            // Make sure interaction tag is of correct type
            //if (interactionTag.Type != ImageTagTypes.Interaction)
            //{
                //Log.WriteLine($"Unable to add non-interaction tag {interactionTagPath} to {affectedTagPath}", MessageType.Warning);
                //return;
            //}

            // Create tag interaction if does not exist
            if (!image.TagInteractions.ContainsKey(interactionTagPath))
            {
                image.TagInteractions.Add(interactionTagPath, new ImageTagInteractions(interactionTag));
            }

            // Add tag interaction
            ImageTagInteractions interactions = image.TagInteractions[interactionTagPath];
            interactions.AffectedTags.Add(affectedTag);
            Log.WriteLine($"Added interaction {interactionTagPath} to {affectedTagPath}");
        }

        public HashSet<ImageTag> GetTagInteractions(string imageHash, string interactionTag)
        {
            // Make sure image and tag is in database
            AssertContainsImage(imageHash, $"Unable to add tag interaction, image {imageHash} not found in database!");
            AssertContainsTag(interactionTag, $"Unable to add tag interaction, tag {interactionTag} not found in database!");

            // Check if there are any interactions
            Image image = ImageTable[imageHash];
            if (!image.TagInteractions.ContainsKey(interactionTag))
            {
                return null;
            }

            // Return list of tag interactions
            ImageTagInteractions interactions = image.TagInteractions[interactionTag];
            return interactions.AffectedTags;
        }

        public void ImportTags(string imageHash)
        {
            try
            {
                List<string> tagList = WebAPIs.E6GetTags(imageHash);
                foreach (string origTag in tagList)
                {
                    string tagPath = SubstituteTag(origTag);
                    if (!string.IsNullOrWhiteSpace(tagPath))
                    {
                        AssignTag(imageHash, tagPath, ImageTagTypes.Regular);
                    }
                }
            }
            catch (Exception)
            {
                // TODO: Display error message
            }
        }

        // Try to replace a string with a different tag string
        private string SubstituteTag(string tag)
        {
            // Check if tag matches an entry in the sub table
            if (SubstitutionTable.ContainsKey(tag))
            {
                return SubstitutionTable[tag];
            }

            // Tag not found, return unmodified tag
            return tag;
        }
    }
}
