using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace BetterImageTagEditor
{
    public static class WebAPIs
    {
        private static readonly string UserAgent = "BITE";

        private static HttpClient Client = new HttpClient();
        private static Stopwatch SW = new Stopwatch();

        private static string SearchQuery;
        private static int ImageBatchSize;
        private static string LastID;

        static WebAPIs()
        {
            Client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            SW.Start();
        }

        // Wait between repeatedly accessing API to ensure rate limit is not hit
        private static void E6WaitForAccess()
        {
            // Wait this many ms between repeat accesses
            int waitTime = 1000;

            // Busy wait until enough time has elapsed
            while (SW.ElapsedMilliseconds < waitTime)
            {
                Thread.Sleep(100);
            }

            SW.Restart();
        }
        
        public static List<string> E6GetTags(string imageHash)
        {
            E6WaitForAccess();

            // Retrieve tag list
            string url = $"https://e621.net/post/tags.json?md5={imageHash}";
            string responseString = Client.GetStringAsync(url).Result;

            // Parse tag list
            List<string> tagList = new List<string>();
            string name = null;
            string type = null;
            string[] chunks = responseString.Split(',');
            foreach (string chunk in chunks)
            {
                // Read individual properties
                if (chunk.StartsWith("\"name\""))
                {
                    name = chunk.Substring(8, chunk.Length - 9);
                }
                else if (chunk.StartsWith("\"type\""))
                {
                    type = chunk.Substring(7, 1);
                }

                // Save tag
                if (name != null && type != null)
                {
                    // TODO: Possibly do something with remaining types as well
                    string prefix = (type == "1") ? "artist:" : (type == "3") ? "universe:" : (type == "5") ? "character:" : "";
                    tagList.Add(prefix + name);
                    name = null;
                    type = null;
                }
            }

            return tagList;
        }

        public static List<Image> E6GetImages(Database DB, string searchQuery, int count = 1, string lastID = null)
        {
            E6WaitForAccess();

            // Save search parameters
            SearchQuery = searchQuery;
            ImageBatchSize = count;

            // Retrieve image list
            string url = $"https://e621.net/post/index.json?tags={SearchQuery}&limit={ImageBatchSize}";
            if (lastID != null)
            {
                url += $"&before_id={lastID}";
            }
            string responseString = Client.GetStringAsync(url).Result;

            // Parse image list
            List<Image> imageList = new List<Image>();
            string md5 = null;
            string file_url = null;
            string preview_url = null;
            string[] chunks = responseString.Split(',');
            foreach (string chunk in chunks)
            {
                // Read individual properties
                if (chunk.StartsWith("\"md5\""))
                {
                    md5 = chunk.Substring(7, chunk.Length - 8);
                }
                else if (chunk.StartsWith("\"file_url\""))
                {
                    file_url = chunk.Substring(12, chunk.Length - 13);
                }
                else if (chunk.StartsWith("\"preview_url\""))
                {
                    preview_url = chunk.Substring(15, chunk.Length - 16);
                }
                else if (chunk.StartsWith("\"id\""))
                {
                    LastID = chunk.Substring(6, chunk.Length - 8);
                }

                // Save tag
                if (md5 != null && file_url != null && preview_url != null)
                {
                    // Get image object
                    if (!DB.ContainsImage(md5))
                    {
                        DB.AddImage(md5);
                    }
                    Image image = DB.GetImage(md5);

                    // Add file location
                    image.Locations.Add(file_url);

                    // Download thumbnail
                    string fileName = preview_url.Substring(preview_url.LastIndexOf('/') + 1);
                    string thumbFile = Path.Combine(DB.DataDir, "thumbs", fileName);
                    if (!File.Exists(thumbFile))
                    {
                        using (var client = new WebClient())
                        {
                            client.Headers.Add(HttpRequestHeader.UserAgent, UserAgent);
                            client.DownloadFile(preview_url, thumbFile);
                        }
                        image.ThumbLocation = thumbFile;
                    }

                    imageList.Add(image);

                    md5 = null;
                    file_url = null;
                    preview_url = null;
                }
            }

            return imageList;
        }

        public static List<Image> E6GetImagesNextPage(Database DB)
        {
            return E6GetImages(DB, SearchQuery, ImageBatchSize, LastID);
        }
    }
}
