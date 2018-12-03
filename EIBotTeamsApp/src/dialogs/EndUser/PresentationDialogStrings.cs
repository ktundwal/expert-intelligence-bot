namespace Microsoft.Office.EIBot.Service.dialogs.EndUser
{
    public class PresentationDialogStrings
    {
        private static string projectUrl = System.Configuration.ConfigurationManager.AppSettings["BaseUri"];
        private const string presentationAssetPath = @"public/assets/ppt/";
        

        public const string WhoIsBot = "Hello! Welcome to Expert Connect.";
        public const string BotDescription = "We’re supported by experts from UpWork and FancyHands, who can work for you.";
        public const string StartingOptionDescription = "Please select an option below. We’ll collect some information to get started, then a project manager will review your request and follow up.";

        public const string WebResearch = "Web Research";
        public static string WebResearchUrl = projectUrl + presentationAssetPath + "web_research_icon.png";

        public const string PresentationDesign = "Presentation Design";
        public static string PresentationDesignUrl = projectUrl + presentationAssetPath + "presentation_design_icon.png";

        public const string PersonalTasks = "Personal Tasks";
        public static string PersonalTasksUrl = projectUrl + presentationAssetPath + "personal_tasks_icon.png";

        public const string LetsBegin = "Let's do it!";
        public const string LetsBeginDescription = "Okay great. You’ll get up to 10 slides designed by a professional designer with one round of revisions at no cost to you (Microsoft will cover the $50. Please do not include any confidential company information)";
        public const string LetsBeginWhatWeDo = "**What we do**: Adjust fonts and text sizes Change colors Fix spacings and layout Add any images, illustrations or icons that you provide us Select stock images for you";
        public const string LetsBeginConfirmation = "**What we don’t do**: Create custom images Animation Let’s start with a few simple questions, sound good?";
        //public const string LetsBeginConfirmation = "Let's start with a few simple questions, sounds good?";

        public const string PurposeDescription = "Okay. Which of these most closely matches the purpose of your presentation?";

        public const string NewProject = "New project pitch";
        public const string NewProjectDesc = "Pitch decks, business plans, financial reports";

        public const string ProgressReport = "Progress report";
        public const string ProgressReportDesc = "Slides for a workshop, webinar or training";

        public const string Educate = "Educate & Instruct";
        public const string EducateDesc = "Company frameworks, guidelines or key resources";

        public static string GetImageUrl(string location)
        {
            return projectUrl + presentationAssetPath + location;
        }
    }
}