using System.Collections.Generic;

namespace com.microsoft.ExpertConnect.Helpers
{
    public class Constants
    {
        public const string WhoIsBot = "Hello! Welcome to Expert Connect.";
        public const string BotDescription = "We’re supported by experts from UpWork and FancyHands, who can work for you.";
        public const string StartingOptionDescription = "Please select an option below. We’ll collect some information to get started, then a project manager will review your request and follow up.";

        public const string WebResearch = "Web Research";
        public static string WebResearchUrl = "web_research_icon.png";

        public const string PresentationDesign = "Presentation Design";
        public static string PresentationDesignUrl = "presentation_design_icon.png";

        #region IntroductionCard
        public const string V2Introduction = 
            "Welcome to Expert Connect for PowerPoint. " +
            "You’ll get up to 10 slides designed by a professional designer with one round of revisions at no cost to you (Microsoft will cover the $50. " +
            "Please do not include any confidential company information).";

        public static string WhatWeDo = "**What we do:**";
        public static string WhatWeDontDo = "**What we DON'T do:**";

        public static List<string> V2WhatWeDo = new List<string>()
        {
            "Adjust fonts and text sizes",
            "Change colors",
            "Fix spacings and layout",
            "Add any images, illustrations or icons that you provide us",
            "Select stock images for you"
        };
        public static List<string> V2WhatWeDontDo = new List<string>()
        {
            "Create custom images",
            "Animation"
        };
        public const string V2Start = "Let’s start with a few simple questions, sound good?";
        public const string V2LetsBegin = "Let's do it!";
        public const string V2ShowExamples = "Show me examples";
        #endregion

        #region Purpose
        public const string V2PurposeDescription = "Okay. Which of these most closely matches the purpose of your presentation?";

        public const string V2NewProject = "New project pitch";
        public const string V2NewProjectDesc = "Pitch decks, business plans, financial reports";

        public const string V2ProgressReport = "Progress report";
        public const string V2ProgressReportDesc = "Company frameworks, guidelines or key resources";

        public const string V2Educate = "Educate & Instruct";
        public const string V2EducateDesc = "Slides for a workshop, webinar or training";

        public const string V2Cleanup = "Slide cleanup";
        public const string V2CleanupDec = "Upload your own PowerPoint file for some pro polish";
        #endregion

        #region Examples

        public static string V2ExampleInfo = 
            "To give you an idea of what this service can do, " +
            "here are 3 examples of an end-product, " +
            "all with the same content but different styles.";
        public static string V2SomethingDifferent = 
            "Want something different? " +
            "We can guide you to create your own brief like the above examples by asking you a few questions, " +
            "then we'll send the brief to the designer.";
        
        #endregion

        #region Styling

        public const string Variations = "Which of these variations do you like best?";
        public const string NoneOfThese = "None of these";
        public const string DescribeWhatIWant = "Let me describe what I want";

        public const string ColorDark = "Dark";
        public const string ColorLight = "Light";
        public const string Colorful = "Colorful";

        public const string VisualsPhotos = "Photos";
        public const string VisualsIllustrations = "Illustrations";
        public const string VisualsShapes = "Shapes";

        public const string ImagesDesc =
            "Thanks! " +
            "Do you want us to find up to 5 images for your presentation? " +
            "Or would you like to use your own?";

        public const string NewImages = "New images please!";
        public const string OwnImages = "I'll use my own images";

        public const string WhyWeAsking = "[Why we're asking](http://www.microsoft.com/)";

        public const string ImagesDisclaimer =
            "New image estimate includes up to 5 royalty-free images. " +
            "Amount will be charged to a purchase order set up by your Company. " +
            "Images you provide must be accompanied by proof of ownership or licensing.";

        #endregion

        #region MoreInformation

        public const string LastQuestion =
            "Great. Last question: " +
            "Is there anything that you specifically DON'T want the designer to do? Any pet peeves?";

        public const string LetUsKnow =
            "**Let us know in the reply box below, in one single message** (we'll progress to the next step afterwards)";

        #endregion


        #region Buttons

        public const string CreateBrief = "Create a brief";
        public const string AddedEverythingToFile = "Okay, I've added everythign to the file";
        public const string LooksGood = "Looks good, send the job";
        public const string ChangeSomething = "Let me start over";
        public const string Complete = "This is complete";
        public const string Revision = "I want a free revision";

        #endregion
    }
}
