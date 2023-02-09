using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Serilog;
using Serilog.Core;

namespace BTArchiver
{
    internal static class Program
    {
        public static Logger SeriLogger = new LoggerConfiguration().WriteTo.Console().WriteTo.File("log-.txt", rollingInterval: RollingInterval.Hour).CreateLogger();
        private static System.Net.WebClient WebClient = new System.Net.WebClient();
        private static string allModsPage = "https://bonetome.com/ancientdungeon/all/";

        private static void Main(string[] args)
        {
            string baseFolderPath = @"F:\BoneTomeArchive\ModUploads\Ancient Dungeon VR";
            int pages = 1;

            HtmlWeb web = new HtmlWeb();
            for (int i = 0; i < pages; i++)
            {
                try
                {
                    int currentPage = i + 1;
                    SeriLogger.Information("Loading page " + currentPage);
                    HtmlDocument doc = web.Load(allModsPage + "?page=" + currentPage);
                    foreach (HtmlNode node in doc.GetElementbyId("assets-grid").ChildNodes)
                    {
                        if (!node.HasClass("asset-tile")) continue;
                        string modPage = string.Empty;
                        foreach (HtmlNode cNode in node.ChildNodes)
                        {
                            if (cNode.HasClass("asset-tile-info"))
                            {
                                string page = cNode.ChildNodes.Where((n) => !n.GetAttributeValue("href", "../../").Contains("../../")).FirstOrDefault()?.GetAttributeValue("href", "");
                                if (!string.IsNullOrEmpty(page))
                                    modPage = allModsPage + page;
                                break;
                            }
                        }
                        if (string.IsNullOrEmpty(modPage))
                        {
                            SeriLogger.Error("Failed to find mod page for an item, skipping.");
                            continue;
                        }

                        BoneTomeMetadata btmd = Metadata.FromUrl(modPage);

                        SeriLogger.Information("Located mod " + btmd.ModName + " and found metadata.");

                        string uploaderPath = Path.Combine(baseFolderPath, btmd.Uploader.PathSafe());
                        if (!Directory.Exists(uploaderPath))
                            Directory.CreateDirectory(uploaderPath);

                        string categoryPath = Path.Combine(uploaderPath, btmd.Category);
                        if (!Directory.Exists(categoryPath))
                            Directory.CreateDirectory(categoryPath);

                        string modPath = Path.Combine(categoryPath, btmd.ModName.PathSafe());
                        if (!Directory.Exists(modPath))
                            Directory.CreateDirectory(modPath);

                        if (File.Exists(Path.Combine(modPath, "README.md")) && File.Exists(Path.Combine(modPath, btmd.Asset.FileName)))
                        {
                            SeriLogger.Information("Mod " + btmd.ModName + " exists, skipping.");
                            continue;
                        }

                        SeriLogger.Information("Downloading " + btmd.Asset.FileName + " to disk.");

                        File.WriteAllText(Path.Combine(modPath, "README.md"), btmd.ReadMe);
                        byte[] assetBytes = WebClient.DownloadData(btmd.Asset.DownloadUrl);
                        File.WriteAllBytes(Path.Combine(modPath, btmd.Asset.FileName), assetBytes);

                        SeriLogger.Information("Finished downloading " + btmd.ModName);
                    }
                }
                catch (Exception ex)
                {
                    SeriLogger.Error(ex.ToString());
                    SeriLogger.Error("Exception thrown, we're just gonna continue");
                }
            }

            SeriLogger.Information("Finished downloading all retrievable mods.");
        }

        public static string PathSafe(this string str)
        {
            string newStr = str;
            newStr = newStr.Replace('\'', '_');
            foreach (char c in Path.GetInvalidFileNameChars())
                newStr = newStr.Replace(c, '_');
            Regex ex = new Regex("_+");
            newStr = ex.Replace(newStr, "_");
            newStr.Trim('_');

            return newStr;
        }
    }
}
