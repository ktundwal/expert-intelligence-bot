using System;
using System.Configuration;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json;

namespace Microsoft.Office.EIBot.Service.utility
{
    public class DateTimeUtility
    {
        public static async System.Threading.Tasks.Task<DateTime?> ParseForDate(string textToAnalyze)
        {
            //Query LUIS and get the response
            LuisOutput luisOutput = await GetIntentAndEntitiesFromLuis(textToAnalyze);

            if (luisOutput?.entities == null) return null;

            foreach (var entity in luisOutput.entities)
            {
                if (entity.Resolution?.Values == null) continue;
                foreach (var value in entity.Resolution?.Values)
                {
                    if (value.Type == "datetime")
                    {
                        return DateTime.Parse(value.Value);
                    }
                }
            }

            return null;
        }

        private static async System.Threading.Tasks.Task<LuisOutput> GetIntentAndEntitiesFromLuis(string textToAnalyze)
        {
            const string luisEndpoint = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps";

            var url = $"{luisEndpoint}/{ConfigurationManager.AppSettings["LuisAppId"]}" +
                      $"?subscription-key={ConfigurationManager.AppSettings["LuisSubscriptionId"]}" +
                      "&verbose=true&timezoneOffset=0" +
                      $"&q={HttpUtility.UrlEncode(textToAnalyze)}";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage msg = await client.GetAsync(url);
                    if (msg.IsSuccessStatusCode)
                    {
                        var jsonDataResponse = await msg.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<LuisOutput>(jsonDataResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error extracting entity from text {textToAnalyze}", ex);
            }
            return null;
        }

        private class LuisOutput
        {
            public string query { get; set; }
            public LuisIntent[] intents { get; set; }
            public LuisEntity[] entities { get; set; }
        }
        private class LuisEntity
        {
            public string Entity { get; set; }
            public string Type { get; set; }
            public string StartIndex { get; set; }
            public string EndIndex { get; set; }
            public float Score { get; set; }
            public Resolution Resolution { get; set; }
        }
        private class LuisIntent
        {
            public string Intent { get; set; }
            public float Score { get; set; }
        }

        private class Resolution
        {
            public ResolutionValue[] Values { get; set; }
        }

        private class ResolutionValue
        {
            public string Timex { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
        }
    }
}