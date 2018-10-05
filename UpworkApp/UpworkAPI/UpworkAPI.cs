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
            {"Skills","profiles/v1/metadata/skills.json"}
        };

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="configuration">Upwork OAuth 1.0 client</param>
        /// <exception cref="System.ArgumentNullException">Thrown when OAuthClient parameter is null.</exception>
        public Upwork(IOAuthClient client)
        {
            _client = client ?? throw new ArgumentNullException("client");
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
