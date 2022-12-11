using System;
using System.IO;

namespace SI.AOC.Leaderboard
{
    public class Icons 
    {
        public string Gold { get; init; }
        public string Silver { get; init; }
        public string Bronze { get; init; }
        public string SameDay { get; init; }
        public string Empty { get; init; }
        public string Star { get; init; }
        public string StartDir { get; init; }
        private string m_attr;
        public Icons()
        {
            string attrPath = Path.Combine("data", "icons", "attributeNormal.txt");
            if (File.Exists(attrPath))
            {
                StartDir = ".";
            }
            else
            {
                StartDir = Path.Combine("/", "home", "site", "wwwroot");
            }
            m_attr = File.ReadAllText(Path.Combine(StartDir, attrPath));
            Gold = GetIconHtml("gold.png");
            Silver = GetIconHtml("silver.png");
            Bronze = GetIconHtml("bronze.png");
            SameDay = GetIconHtml("sameday.png");
            Empty = GetIconHtml("empty.png");
            Star = GetIconHtml("star.png");
        }

        private string GetIconHtml(string fileName)
        {
            var bytes = File.ReadAllBytes(Path.Combine(StartDir, "data", "icons", fileName));
            var encodedStr = Convert.ToBase64String(bytes);
            return $"<img src='data:image/png;base64,{encodedStr}' {m_attr}>";
        }
     }
}