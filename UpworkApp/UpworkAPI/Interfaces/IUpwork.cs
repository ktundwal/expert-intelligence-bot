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
    }
}
