using System;
using System.Collections.Generic;
using System.Text;

namespace UpworkAPI.Models
{
    public class UpworkJob
    {
        /// <summary>
        /// Title of the job
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The client's team ID
        /// </summary>
        public string BuyerTeamReference { get; set; }

        /// <summary>
        /// Job type
        /// </summary>
        public string JobType { get; set; }

        /// <summary>
        /// Job description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The visibility of the job
        /// </summary>
        public string Visibility { get; set; }

        /// <summary>
        /// The category of the job
        /// </summary>
        public string Category2 { get; set; }

        /// <summary>
        /// The subcategory of the job
        /// </summary>
        public string Subcategory2 { get; set; }

        /// <summary>
        /// The start date of the job
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// The budget for a fixed-price job
        /// </summary>
        public decimal? Budget { get; set; }

        /// <summary>
        /// The duration of the job in hours
        /// </summary>
        public int? Duration { get; set; }

        /// <summary>
        /// The skills required for the job
        /// </summary>
        public string Skills { get; set; }

        /// <summary>
        /// The preferred type of freelancer
        /// </summary>
        public string ContractorType { get; set; }

        /// <summary>
        /// Public Constructor for UpworkJob instance
        /// </summary>
        /// <param name="title">
        /// Title of the job. Example: `Development of API ecosystem`
        /// </param>
        /// <param name="buyerTeamReference">
        /// The reference ID of the client's team that is posting the job. Example: `34567`. You can get it from List teams API call.
        ///  </param>
        /// <param name="description">
        /// The job description. Example: `A new interesting start-up requires an API ecosystem`
        /// </param>
        /// <param name="jobType">
        /// The type of the job posted. Valid values: hourly, fixed-price
        /// </param>
        /// <param name="visibility">
        /// The visibility of the job. Values description: `public` - the job is available to all users who search for jobs; `private` - the job is visible to the employer only; `odesk` - the job     appears in search results only for Upwork users who are logged in; `invite-only` - jobs do not appear in search and are used for jobs where the client wants to control the potential       applicants
        /// </param>
        /// <param name="category">
        /// The category of the job according to the list of Categories 2.0. Example: `Web Development`. You can get it via Metadata Category (V2) resource
        /// </param>
        /// <param name="subCategory">
        /// The subcategory of the job according to the list of Categories 2.0. Example: `Web & Mobile Development`. You can get it via Metadata Category (v2) resource.
        /// </param>
        /// <param name="skills">
        /// The skills required for the job. Use semi-colon ';' to separate the skills. Optional parameter
        /// </param>
        /// <param name="startDate">
        /// The start date of the job. If the `start_date` is not included, the job defaults to starting immediately. Example: `06-15-2011`. Optional parameter
        /// </param>
        /// <param name="budget">
        /// The budget for a fixed-price job. Example: `100. Optional parameter
        /// </param>
        /// <param name="duration">
        /// The duration of the job in hours. Used for hourly-jobs. Example: `90`. Optional parameter
        /// </param>
        /// /// <param name="contractorType">
        /// The preferred type of freelancer. Valid values: individuals, agencies, all. Optional parameter
        /// </param>
        public UpworkJob(string title, string buyerTeamReference, string description, string category, string subCategory, string jobType = "fixed-price", string visibility = "private",
            string skills = null, DateTime? startDate = null, decimal? budget = null, int? duration = null, string contractorType = null)
        {
            Title = title;
            BuyerTeamReference = buyerTeamReference;
            Description = description;
            JobType = jobType;
            Visibility = visibility;
            Category2 = category;
            Subcategory2 = subCategory;
            Skills = skills;
            StartDate = startDate;
            Budget = budget;
            Duration = duration;
            ContractorType = contractorType;

        }

        /// <summary>
        /// Generate json string with job values
        /// </summary>
        /// <returns>String in Json format</returns>
        public string ToJsonString()
        {
            string result = "";

            Dictionary<string, string> jobDictionary = ToUpworkDictionary();

            result = Newtonsoft.Json.JsonConvert.SerializeObject(jobDictionary, Newtonsoft.Json.Formatting.Indented);

            return result;
        }

        /// <summary>
        /// Create dictionary with upwork job params
        /// </summary>
        /// <returns>Dictionary with Upwork API posting job keys</returns>
        public Dictionary<string, string> ToUpworkDictionary()
        {
            var jobDictionary = new Dictionary<string, string>
            {
                { "buyer_team__reference", BuyerTeamReference },
                { "title", Title },
                { "job_type", JobType },
                { "description", Description },
                { "visibility", Visibility },
                { "category2", Category2 },
                { "subcategory2", Subcategory2 }
            };

            if (StartDate != null)
                jobDictionary.Add("start_date", StartDate?.ToString("MM-dd-yyy"));
            if (Duration != null)
                jobDictionary.Add("duration", Duration.ToString());
            if (Budget != null)
                jobDictionary.Add("budget", Budget.ToString());
            if (!String.IsNullOrEmpty(Skills))
                jobDictionary.Add("skills", Skills);

            return jobDictionary;
        }
    }
}
