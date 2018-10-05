using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UpworkAPI.Models;

namespace UpworkAPI
{
    /// <summary>
    /// Сlass for working with Upwork using Single-user OAuth 1.0
    /// https://www.upwork.com/api/
    /// </summary>
    public class Upwork
    {
        /// <summary>
        /// OAuthClient
        /// </summary>
        OAuthClient _client;

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
            {"UserTeams","hr/v2/teams.json"}
        };

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="configuration">Upwork OAuth 1.0 client</param>
        public Upwork(OAuthClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Get current User Info
        /// </summary>
        /// <returns></returns>
        public async Task<UpworkUser> GetUserInfo()
        {
            UpworkUser user = new UpworkUser();
            try
            {
                string clientUrl = UpworkApiBaseUrl + API_URL["UserInfo"];
                string clientResponse = await _client.SendRequest(clientUrl, "GET", new Dictionary<string, string>());
                user = JsonConvert.DeserializeObject<UpworkUser>(clientResponse);
            }
            catch { }
            
            return user;
        }

        /// <summary>
        /// Get user teams
        /// </summary>
        /// <returns>Return list of teams for current user </returns>
        public async Task<List<UserTeam>> GetUserTeams()
        {
            List<UserTeam> result = new List<UserTeam>();
            try
            {
                string teamsUrl = UpworkApiBaseUrl + API_URL["UserTeams"];
                string teamsResponse = await _client.SendRequest(teamsUrl, "GET", new Dictionary<string, string>());
                JObject jResponse = JObject.Parse(teamsResponse);
                result = JsonConvert.DeserializeObject<List<UserTeam>>(jResponse["teams"].ToString());
            }
            catch { }
            return result;
        }

        /// <summary>
        /// Posting Job
        /// </summary>
        /// <param name="job">UpworkJob instance</param>
        /// <returns>New job Id</returns>
        public async Task<JobInfo> PostJob(UpworkJob job)
        {
            JobInfo result = new JobInfo();
            try
            {
                string postJobUrl = UpworkApiBaseUrl + API_URL["PostJob"];
                string jobResponse = await _client.SendRequest(postJobUrl, "POST", job.ToUpworkDictionary());
                JObject jResponse = JObject.Parse(jobResponse);
                result = JsonConvert.DeserializeObject<JobInfo>(jResponse["job"].ToString());
            }
            catch { }
            return result;
        }

        public async Task<List<Category>> GetCategories()
        {
            List<Category> result = new List<Category>();
            try
            {
                string categoriesUrl = UpworkApiBaseUrl + API_URL["Categories"];
                string categoriesResponse = await _client.SendRequest(categoriesUrl, "GET", new Dictionary<string, string>());
                JObject jResponse = JObject.Parse(categoriesResponse);
                result = JsonConvert.DeserializeObject<List<Category>>(jResponse["categories"].ToString());
            }
            catch { }
            return result;
        }
    }
}
