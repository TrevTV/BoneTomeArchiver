using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace BTArchiver
{
    internal struct BoneTomeAsset
    {
        public string FileName; //
        public string DownloadUrl;
    }

    internal struct BoneTomeMetadata
    {
        public string ModName; //
        public string Version; //
        public BoneTomeAsset Asset;
        public string ReadMe; //
        public string Uploader; //
        public string Category; //
    }

    internal static class Metadata
    {
        public static BoneTomeMetadata FromUrl(string url)
        {
            HtmlWeb htmlWeb = new HtmlWeb();
            HtmlDocument htmlDoc = htmlWeb.Load(url + '/');

            BoneTomeMetadata btmd = new BoneTomeMetadata();
            BoneTomeAsset bta = new BoneTomeAsset();

            Regex ex = new Regex("[^a-zA-Z0-9_]");
            btmd.ModName = htmlDoc.GetElementbyId("info-assets-top").ChildNodes.Where((n) => n.HasClass("assets-title")).FirstOrDefault()?.InnerText.Trim() ?? "UNKNOWN";

            HtmlNode modData = htmlDoc.GetElementbyId("mod-data");
            foreach (HtmlNode node in modData.ChildNodes)
            {
                string data = node.ChildNodes.Where((n) => n.HasClass("mod-data-header")).FirstOrDefault()?.InnerText ?? "UNKNOWN_ITEXT";
                switch (data.ToLower())
                {
                    case "version":
                        btmd.Version = node.ChildNodes.Where((n) => n.HasClass("mod-data-content")).FirstOrDefault()?.InnerText;
                        break;
                    case "file name":
                        bta.FileName = node.ChildNodes.Where((n) => n.HasClass("mod-data-content")).FirstOrDefault()?.InnerText;
                        break;
                    case "uploader":
                        btmd.Uploader = node.ChildNodes.Where((n) => n.HasClass("mod-data-content")).FirstOrDefault()?.ChildNodes[0].InnerText;
                        break;
                    default:
                        break;
                }
            }

            bta.DownloadUrl = "https://bonetome.com" + htmlDoc.GetElementbyId("download-button").ParentNode.GetAttributeValue("href", "UNKN");
            btmd.Asset = bta;
            btmd.Category = url.Split('/')[6];
            btmd.ReadMe = BuildReadMe(htmlDoc, btmd);

            return btmd;
        }

        private static string BuildReadMe(HtmlDocument doc, BoneTomeMetadata btmd)
        {
            string readMe = $"# {btmd.ModName}\n";
            readMe += $"Version {btmd.Version}, uploaded by {btmd.Uploader} into category {btmd.Category}\n";
            readMe += $"Original File Name: {btmd.Asset.FileName}\n\n";

            var descNodes = doc.GetElementbyId("mod-info-left").ChildNodes.Where((n) => n.Id == "mod-info-desc");
            string description = descNodes.FirstOrDefault()?.InnerHtml;
            ReplaceHeadingsWithMD(ref description);
            ReplaceHtmlTagThing(ref description);
            readMe += description;

            descNodes = descNodes.Reverse();
            readMe += "\n\n## Changelog\n\n";
            string changelog = descNodes.FirstOrDefault()?.InnerHtml;
            ReplaceHeadingsWithMD(ref changelog);
            ReplaceHtmlTagThing(ref changelog);
            readMe += changelog;

            return readMe;
        }

        private static void ReplaceHtmlTagThing(ref string str)
        {
            str = str.Replace("&quot;", "\"");
            str = str.Replace("&#039;", "'");
        }

        private static void ReplaceHeadingsWithMD(ref string str)
        {
            str = str.Replace("<h1>", "# ");
            str = str.Replace("</h1>", "");
            str = str.Replace("<h2>", "## ");
            str = str.Replace("</h2>", "");
            str = str.Replace("<h3>", "### ");
            str = str.Replace("</h3>", "");
            str = str.Replace("<h4>", "#### ");
            str = str.Replace("</h4>", "");
            str = str.Replace("<h5>", "###### ");
            str = str.Replace("</h5>", "");

            str = str.Replace("<b>", "**");
            str = str.Replace("</b>", "**");

            str = str.Replace("<em>", "_");
            str = str.Replace("</em>", "_");
        }
    }
}
