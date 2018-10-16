using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UpworkAPI.Models;

namespace UpworkAPI.Interfaces
{
    public interface IUpwork
    {
        /// <summary>
        /// Get current User Info
        /// </summary>
        /// <returns></returns>
        Task<UpworkUser> GetUserInfo();

        /// <summary>
        /// Get user teams
        /// </summary>
        /// <returns>Return list of teams for current user </returns>
        Task<List<UserTeam>> GetUserTeams();

        /// <summary>
        /// Posting Job
        /// </summary>
        /// <param name="job">UpworkJob instance</param>
        /// <returns>New job Id</returns>
        Task<JobInfo> PostJob(UpworkJob job);

        /// <summary>
        /// Get list of Upwork first-level categories with subcategories
        /// </summary>
        /// <returns>List of Category</returns>
        Task<List<Category>> GetCategories();

        /// <summary>
        /// Get list of available upwork skills
        /// </summary>
        /// <returns>List<System.String></returns>
        Task<List<string>> GetSkills();

        /// <summary>
        /// Get engagement(s) based on the parameters supplied in the API call
        /// </summary>
        /// <param name="buyerTeamReference">The reference ID of the client's team. Example: `34567`. Use 'List teams' API call to get it.</param>
        /// <param name="includeSubTeams">If set to `1`: the response includes info about sub teams. Valid values: 0, 1</param>
        /// <param name="provider__reference">The freelancer's reference ID. Example: `1234`.</param>
        /// <param name="profile_key">The unique profile key. It is used if the `provider_reference` param is absent.</param>
        /// <param name="job__reference">The job reference ID. Use `List jobs` call to get it.</param>
        /// <param name="agency_team__reference">The reference ID of the agency</param>
        /// <param name="status">The current status of the engagement. Multiple statuses can be listed using semicolon. Example: `status=active;closed`. Valid values: active, closed</param>
        /// <param name="created_time_from">Filters by 'from' time.</param>
        /// <param name="created_time_to">Filters by 'to' time.</param>
        /// <param name="page">Pagination, formed as `$offset;$count`. Example: `page=20;10`</param>
        /// <param name="order_by">Sorts results in format `$field_name1;$field_name2;..$field_nameN;AD...A`. Here `A` stands for ascending order, `D` - descending order. Valid field names for ordering are: `reference`, `created_time`, `offer__reference`, `job__reference`, `client_team__reference`, `provider__reference`, `status`, `engagement_start_date`, `engagement_end_date`</param>
        /// <returns></returns>
        Task<List<Engagement>> GetEngagements(string buyerTeamReference = null, int includeSubTeams = 0, string providerReference = null, string profileKey = null, string jobReference = null, string agencyTeamReference = null, string status = null, DateTime? createdTimeFrom = null, DateTime? createdTimeTo = null, string page = null, string orderBy = null);

        /// <summary>
        /// Get all jobs that a user has `manage_recruiting` access to. It can be used to find the reference/key ID of a specific job
        /// </summary>
        /// <param name="buyer_team__reference">The reference ID of the client's team. Example: `34567`. You can get it from List teams API call.</param>
        /// <param name="created_by">The user ID</param>
        /// <param name="status">The status of the job. Valid values: open, filled, cancelled</param>
        /// <param name="created_time_from">Filters by 'from' time</param>
        /// <param name="created_time_to">Filters by 'to' time</param>
        /// <param name="page">Pagination, formed as `$offset;$count`. Example: `page=20;10`</param>
        /// <param name="order_by">Sorts results by the value defined. Example: `order_by=created_time`</param>
        /// <returns>List of UpworkAPI.JobInfo</returns>
        Task<List<JobInfo>> GetJobs(string buyer_team__reference, string created_by = null, string status = null, DateTime? created_time_from = null, DateTime? created_time_to = null, string page = null, string order_by = null);

        /// <summary>
        /// Create a milestone. Note that the user must be authorized in Upwork and must be Hiring Manager in the team to be able to create a milestone
        /// </summary>
        /// <param name="contract_reference">Contract reference. Contracts info are available in the Engagements API.</param>
        /// <param name="milestone_description">Name of the milestone</param>
        /// <param name="deposit_amount">Amount to deposit for this milestone</param>
        /// <param name="due_date">Expected date of finalization. Optional.</param>
        /// <returns>Id of new milestone</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when one of required arguments is null or empty</exception>
        /// <exception cref="System.ArgumentException">Thrown when deposit amount value is invalid</exception>
        Task<string> CreateMilestone(string contract_reference, string milestone_description, decimal deposit_amount, DateTime? due_date = null);
    }
}
