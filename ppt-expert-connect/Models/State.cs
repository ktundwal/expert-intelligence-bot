using Microsoft.Bot.Schema;

namespace Microsoft.ExpertConnect.Models
{
    public class State
    {
    }

    /// <summary>
    /// User state information.
    /// </summary>
    public class UserInfo
    {
        public TokenResponse Token { get; set; }
        public string Introduction { get; set; }
        public string Purpose { get; set; }
        public string Style { get; set; }
        public string Color { get; set; }
        public string Visuals { get; set; }
        public string Images { get; set; }
        public string Extra { get; set; }
        public int Rating { get; set; }
        public string Feedback { get; set; }

        // State management 
        public UserDialogState State { get; set; }
        public string VsoId { get; set; }
        public string VsoLink { get; set; }
        public string PptWebUrl { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public enum UserDialogState
    {
        ProjectStarted,
        ProjectSelectExampleOptions,
        ProjectCollectTemplateDetails,
        ProjectCollectingTemplateDetails,
        ProjectCollectDetails,
        ProjectCollectingDetails,
        ProjectCreated, // Unsure if useful
        ProjectWaitingAgentReply,
        ProjectWaitingUserReply,
        ProjectInOneOnOneConversation,
        ProjectWaitingReview,
        ProjectCompleted,
        ProjectUnderRevision
    }
}
