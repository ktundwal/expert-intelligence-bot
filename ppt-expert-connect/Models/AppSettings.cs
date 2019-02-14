namespace Microsoft.ExpertConnect.Models
{
    public class AppSettings
    {
        public string Url { get; set; }

        public string AssetsPath { get; set; }

        public string AgentChannelName { get; set; }

        public string BotName { get; set; }

        public string VsoOrgUrl { get; set; }

        public string VsoProject { get; set; }

        public string VsoUsername { get; set; }

        public string VsoPassword { get; set; }

        public string ResearchProjectViaTeamsMinHours { get; set; }

        public string AgentToAssignVsoTasksTo { get; set; }

        public string GetImageUrlFromLocation(string location)
        {
            return Url + AssetsPath + location;
        }
    }
}
