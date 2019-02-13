namespace com.microsoft.ExpertConnect.Models
{
    public class AppSettings
    {
        public string Url { get; set; }
        public string AssetsPath { get; set; }
        public string AgentChannelName { get; set; }
        public string BotName { get; set; }

        public string GetImageUrlFromLocation(string location)
        {
            return Url + AssetsPath + location;
        }
    }
}
