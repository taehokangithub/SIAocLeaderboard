using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(SI.AOC.Leaderboard.Configuration))]

namespace SI.AOC.Leaderboard
{
    public class Configuration : FunctionsStartup
    {
        public static string APICookie { get; private set; } = string.Empty;
        public static int RefreshMinutes { get; private set; } = 5;
        public override void Configure(IFunctionsHostBuilder builder)
        {
            try
            {
                Console.WriteLine($"Configuration started");
                string str = Environment.GetEnvironmentVariable("APIsessionCookie");
                if (!String.IsNullOrEmpty(str))
                {
                    APICookie = str;
                    Console.WriteLine($"APICookie {APICookie}");
                }

                str = Environment.GetEnvironmentVariable("RefreshMinutes");
                if (!String.IsNullOrEmpty(str))
                {
                    RefreshMinutes = int.Parse(str);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            
        }
    }

    public static class FunctionMain
    {
        
        private static Dictionary<int, DateTime> m_jsonTimes = new Dictionary<int, DateTime>();
        private static Dictionary<int, string> m_jsonTexts = new Dictionary<int, string>();

        private const int s_defaultYear = 2022;
        private static int m_year = s_defaultYear;

        public static bool LocalTest = false;

        public static IReadOnlyDictionary<int, DateTime> LastUpdateTimesPerYear => m_jsonTimes;

        public static string GetUri(bool isForJsonFile = false)
        {
            return $"https://adventofcode.com/{m_year}/leaderboard/private/view/786608" + (isForJsonFile ? ".json" : "");
        }

        [FunctionName("aoc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger logger)
        {
            logger.LogInformation("Starting aoc");

            string yearStr = req.Query["year"];

            if (!int.TryParse(yearStr, out m_year))
            {
                m_year = s_defaultYear;
            }

            if (LocalTest)
            {
                string fileName = "data/json/data.json";
                m_jsonTexts[m_year] = System.IO.File.ReadAllText(fileName);
                m_jsonTimes[m_year] = DateTime.Now;
            }

            else if (!m_jsonTexts.ContainsKey(m_year)|| !m_jsonTimes.ContainsKey(m_year) || m_jsonTimes[m_year].AddMinutes(Configuration.RefreshMinutes) < DateTime.Now)
            {
                HttpClient client = new HttpClient();
                string uri = GetUri(isForJsonFile: true);

                var message = new HttpRequestMessage(HttpMethod.Get, uri);

                logger.LogInformation($"Requesting {uri}");

                var cookie = Configuration.APICookie;
                Console.WriteLine($"cookie [{cookie}]");
                message.Headers.Add("Cookie", $"session={cookie}");
                HttpResponseMessage response = await client.SendAsync(message);

                string txt = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response : [{response.ToString()}]");
                Console.WriteLine($"json-response txt : [{txt}]");
                m_jsonTexts[m_year] = txt;
                m_jsonTimes[m_year] = DateTime.Now;
            }

            string responseText = BuildReport(m_jsonTexts[m_year]);

            return new ContentResult{ Content = responseText, ContentType = "text/html"};
        }


        static string BuildReport(string jsonText)
        {
            string ret;
            try
            {
                JObject jsonObject = JsonConvert.DeserializeObject<JObject>(jsonText);
                JToken membersObject = jsonObject.GetValue("members");
                
                List<JToken> members = membersObject.Values().ToList();

                Leaderboard board = new Leaderboard();

                foreach(JObject member in members)
                {
                    board.AddUserJObject(member);
                }

                ret = board.BuildReport(m_year);
            }
            catch(Exception e)
            {
                ret = $"Error parsing json : {e.ToString()}\n{jsonText}";
            }

            return ret;
        }
    }
}
