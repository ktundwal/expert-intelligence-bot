using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UpworkAPI.Interfaces;
using UpworkAPI.Models;

namespace UpworkAPI
{
    /// <summary>
    /// Сlass for working with Upwork using Single-user OAuth 1.0
    /// https://www.upwork.com/api/
    /// </summary>
    public class Upwork : IUpwork
    {
        /// <summary>
        /// OAuthClient
        /// </summary>
        IOAuthClient _client;

        /// <summary>
        /// Instance to work with milestones
        /// </summary>
        private IMilestoneService milestoneService;

        /// <summary>
        /// Instance to work with engagements
        /// </summary>
        private IEngagementService engagementService;

        /// <summary>
        /// Base API url
        /// </summary>
        const string UpworkApiBaseUrl = "https://www.upwork.com/api/";

        /// <summary>
        /// Dictionary with urls, needed to work with Upwork API
        /// </summary>
        public readonly Dictionary<string, string> API_URL = new Dictionary<string, string>()
        {
            {"RequestToken","auth/v1/oauth/token/request"},
            {"AccessToken","auth/v1/oauth/token/access"},
            {"Authorize","auth/"},
            {"SearchProviders","profiles/v2/search/providers.json"},
            {"ListJobs","hr/v2/jobs.json"},
            {"PostJob","hr/v2/jobs.json"},
            {"Categories","profiles/v2/metadata/categories.json"},
            {"UserInfo","auth/v1/info.json"},
            {"UserTeams","hr/v2/teams.json"},
            {"Skills","profiles/v1/metadata/skills.json"},
            {"Engagements","hr/v2/engagements.json"},
            {"CreateMilestone","hr/v3/fp/milestones.json"}
        };

        /// <summary>
        /// Initializes a new instance of the UpworkAPI.Upwork class with a specified IOAuthClient
        /// </summary>
        /// <param name="configuration">Upwork OAuth 1.0 client</param>
        /// <exception cref="System.ArgumentNullException">Thrown when OAuthClient parameter is null.</exception>
        public Upwork(IOAuthClient client)
        {
            _client = client ?? throw new ArgumentNullException("client");
            milestoneService = new MilestoneService(client);
            engagementService = new EngagementService(client);
        }

        public async Task<List<JobInfo>> GetJobs(string buyer_team__reference, string created_by = null, string status = null, DateTime? created_time_from = null, DateTime? created_time_to = null, string page = null, string order_by = null)
        {
            List<JobInfo> result = new List<JobInfo>();;
            try
            {
                Dictionary<string, string> requestFilters = new Dictionary<string, string>();

                if (!String.IsNullOrEmpty(buyer_team__reference))
                    requestFilters.Add("buyer_team__reference", buyer_team__reference);

                if (!String.IsNullOrEmpty(created_by))
                    requestFilters.Add("created_by", created_by);

                if (!String.IsNullOrEmpty(status))
                    requestFilters.Add("status", status);

                if(created_time_from != null)
                    requestFilters.Add("created_time_from", created_time_from?.ToString("yyyy-MM-ddThh:mm:ss"));

                if (created_time_to != null)
                    requestFilters.Add("created_time_to", created_time_to?.ToString("yyyy-MM-ddThh:mm:ss"));

                if (!String.IsNullOrEmpty(page))
                    requestFilters.Add("page", page);

                if (!String.IsNullOrEmpty(order_by))
                    requestFilters.Add("order_by", order_by);

                string jobsUrl = UpworkApiBaseUrl + API_URL["PostJob"];
                string jobsResponse = await _client.SendRequest(jobsUrl, "GET", requestFilters);
                JObject jResponse = JObject.Parse(jobsResponse);
                int totalItems = int.Parse(jResponse["jobs"]["lister"]["total_items"].ToString());
                if(totalItems > 1)
                {
                    result = JsonConvert.DeserializeObject<List<JobInfo>>(jResponse["jobs"]["job"].ToString());
                }
                else if(totalItems == 1)
                {
                    JobInfo job = JsonConvert.DeserializeObject<JobInfo>(jResponse["jobs"]["job"].ToString());
                    result.Add(job);
                }
                
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot get user jobs: {ex.Message}");
            }
            return result;
        }

        public async Task<List<Engagement>> GetEngagements(string buyerTeamReference = null, int includeSubTeams = 0, string providerReference = null, string profileKey = null, string jobReference = null, string agencyTeamReference = null, string status = null, DateTime? createdTimeFrom = null, DateTime? createdTimeTo = null, string page = null, string orderBy = null)
        {
            List<Engagement> result = new List<Engagement>();
            try
            {
                Dictionary<string, string> requestFilters = new Dictionary<string, string> {
                    { "include_sub_teams",includeSubTeams.ToString() }
                };

                if (!String.IsNullOrEmpty(buyerTeamReference))
                    requestFilters.Add("buyer_team__reference", buyerTeamReference);

                if (!String.IsNullOrEmpty(providerReference))
                    requestFilters.Add("provider__reference", providerReference);

                if (!String.IsNullOrEmpty(profileKey))
                    requestFilters.Add("profile_key", profileKey);

                if (!String.IsNullOrEmpty(jobReference))
                    requestFilters.Add("job__reference", jobReference);

                if (!String.IsNullOrEmpty(agencyTeamReference))
                    requestFilters.Add("agency_team__reference", agencyTeamReference);

                if (!String.IsNullOrEmpty(status))
                    requestFilters.Add("status", status);

                if(createdTimeFrom != null)
                    requestFilters.Add("created_time_from", createdTimeFrom?.ToString("yyyy-MM-ddThh:mm:ss"));

                if (createdTimeTo != null)
                    requestFilters.Add("created_time_to", createdTimeTo?.ToString("yyyy-MM-ddThh:mm:ss"));

                if (!String.IsNullOrEmpty(page))
                    requestFilters.Add("page", page);

                if (!String.IsNullOrEmpty(orderBy))
                    requestFilters.Add("order_by", orderBy);


                string engagementsUrl = UpworkApiBaseUrl + API_URL["Engagements"];
                string engagementsResponse = await _client.SendRequest(engagementsUrl, "GET", requestFilters);
                JObject jResponse = JObject.Parse(engagementsResponse);
                int totalItems = int.Parse(jResponse["engagements"]["lister"]["total_items"].ToString());
                if(totalItems > 1)
                {
                    result = JsonConvert.DeserializeObject<List<Engagement>>(jResponse["engagements"]["engagement"].ToString());
                }
                else if(totalItems == 1)
                {
                    Engagement engagement = JsonConvert.DeserializeObject<Engagement>(jResponse["engagements"]["engagement"].ToString());
                    result.Add(engagement);
                }
                
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot get engagements: {ex.Message}");
            }
            return result;
        }

        public async Task<string> CreateMilestone(string contract_reference, string milestone_description, decimal deposit_amount, DateTime? due_date = null)
        {
            string result;

            Dictionary<string, string> milestoneData = new Dictionary<string, string>();

            if (!String.IsNullOrEmpty(contract_reference))
            {
                milestoneData.Add("contract_reference", contract_reference);
            }
            else
            {
                throw new ArgumentNullException("contract_reference");
            }

            if (!String.IsNullOrEmpty(milestone_description))
            {
                milestoneData.Add("milestone_description", milestone_description);
            }
            else
            {
                throw new ArgumentNullException("contract_reference");
            }

            if (deposit_amount > 0)
            {
                milestoneData.Add("deposit_amount", deposit_amount.ToString());
            }
            else
            {
                throw new ArgumentException($"Invalid deposit amount value: {deposit_amount}");
            }

            if (due_date != null)
            {
                milestoneData.Add("due_date", due_date?.ToString("MM-dd-yyyy"));
            }

            try
            {                
                string milestonesUrl = UpworkApiBaseUrl + API_URL["CreateMilestone"];
                string milestoneResponse = await _client.SendRequest(milestonesUrl, "POST", milestoneData);
                JObject jResponse = JObject.Parse(milestoneResponse);
                result = jResponse["id"].ToString();                

            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot create new milestone: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Get current User Info
        /// </summary>
        /// <returns></returns>
        public async Task<UpworkUser> GetUserInfo()
        {
            UpworkUser user;
            try
            {
                string clientUrl = UpworkApiBaseUrl + API_URL["UserInfo"];
                string clientResponse = await _client.SendRequest(clientUrl, "GET", new Dictionary<string, string>());
                user = JsonConvert.DeserializeObject<UpworkUser>(clientResponse);
            }
            catch(Exception ex) {
                throw new Exception($"Cannot get user information: {ex.Message}");
            }
            
            return user;
        }

        /// <summary>
        /// Get user teams
        /// </summary>
        /// <returns>Return list of teams for current user </returns>
        public async Task<List<UserTeam>> GetUserTeams()
        {
            List<UserTeam> result;
            try
            {
                string teamsUrl = UpworkApiBaseUrl + API_URL["UserTeams"];
                string teamsResponse = await _client.SendRequest(teamsUrl, "GET", new Dictionary<string, string>());
                JObject jResponse = JObject.Parse(teamsResponse);
                result = JsonConvert.DeserializeObject<List<UserTeam>>(jResponse["teams"].ToString());
            }
            catch(Exception ex) {
                throw new Exception($"Cannot get user teams: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Posting Job
        /// </summary>
        /// <param name="job">UpworkJob instance</param>
        /// <returns>New job Id</returns>
        public async Task<JobInfo> PostJob(UpworkJob job)
        {
            JobInfo result;
            try
            {
                string postJobUrl = UpworkApiBaseUrl + API_URL["PostJob"];
                Dictionary<string, string> jobData = job.ToUpworkDictionary();

                string jobResponse = await _client.SendRequest(postJobUrl, "POST", jobData);
                JObject jResponse = JObject.Parse(jobResponse);
                result = JsonConvert.DeserializeObject<JobInfo>(jResponse["job"].ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot post new job: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<Category>> GetCategories()
        {
            List<Category> result;
            try
            {
                string categoriesUrl = UpworkApiBaseUrl + API_URL["Categories"];
                string categoriesResponse = await _client.SendRequest(categoriesUrl, "GET", new Dictionary<string, string>());
                JObject jResponse = JObject.Parse(categoriesResponse);
                result = JsonConvert.DeserializeObject<List<Category>>(jResponse["categories"].ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot get upwork categories: {ex.Message}");
            }
            return result;
        }

        /// <summary>
        /// Get list of available upwork skills
        /// </summary>
        /// <returns>List<System.String></returns>
        public async Task<List<string>> GetSkills()
        {
            List<string> result;
            try
            {
                string skillsUrl = UpworkApiBaseUrl + API_URL["Skills"];
                string skillsResponse = await _client.SendRequest(skillsUrl, "GET", new Dictionary<string, string>());
                JObject jResponse = JObject.Parse(skillsResponse);
                result = JsonConvert.DeserializeObject<List<string>>(jResponse["skills"].ToString());
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot get upwork available skills: {ex.Message}");
            }
            return result;
        }
    }
}
