using System;
using System.Collections.Generic;
using System.IO;

namespace BetterImageTagEditor
{
    public class Image
    {
        public string Hash { get; private set; }
        private int _rating = 0;
        public int Rating
        {
            get
            {
                return _rating;
            }
            set
            {
                _rating = (value < 0) ? 0 : (value > 5) ? 5 : value;
            }
        }
        public List<ImageTag> Tags = new List<ImageTag>();
        public Dictionary<string, ImageTagInteractions> TagInteractions = new Dictionary<string, ImageTagInteractions>();

        // Image metadata
        public HashSet<string> Locations { get; set; } = new HashSet<string>();
        public string ThumbLocation { get; set; } = null;
        public string Format { get; set; }
        public string Width { get; set; }
        public string Height { get; set; }

        public Image(string hash)
        {
            this.Hash = hash;
        }

        // Create an Image object from an image's location
        public static Image FromFile(string filePath)
        {
            string imageHash = ComputeFileMD5(filePath);
            Image image = new Image(imageHash);
            image.Locations.Add(filePath);

            // TODO: Get format, dimensions, etc.
            return image;
        }

        // Compute the MD5 hash of the specified file
        public static string ComputeFileMD5(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}
