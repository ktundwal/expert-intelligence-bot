using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UpworkAPI;
using UpworkAPI.Interfaces;
using UpworkAPI.Models;

namespace UpworkApp
{
    class Program
    {
        private static readonly string ConsumerKey = "ac894513895430f14047f6721241f067";
        private static readonly string ConsumerKeySecret = "cd03f726309f9514";

        public static async Task Main(string[] args)
        {
            Console.WriteLine("Upwork application started");

            string OAuthToken = "";
            string OAuthTokenSecret = "";

            //1. At first we have no OAuthToken and OAuthTokenSecret so for first usage we shold to get them
            OAuthConfig config = new OAuthConfig(ConsumerKey, ConsumerKeySecret, OAuthToken, OAuthTokenSecret);
            IOAuthClient client = new OAuthClient(config);

            //2.Get request token.
            //After calling this function We can refer to them through 'tokensResponse["oauth_token"] and 'tokensResponse["oauth_token_secret"]
            //Also after calling this function tokens already exist in config instance
            OAuthUpworkResponse tokensResponse = await client.GetRequestTokens();
            OAuthToken = tokensResponse["oauth_token"];
            OAuthTokenSecret = tokensResponse["oauth_token_secret"];

            //3.Authorize and get verifier
            //In MS Bot Framework application you should use UpworkHelper.cs to get URI with callback argument, witch will redirect you to your HttpCallbackController 
            string loginUrl = $"{OAuthConfig.AuthorizeUrl}?oauth_token={OAuthToken}";
            Console.WriteLine($"Please enter the verification code you get following this link: {loginUrl}");
            string oauth_verifier = Console.ReadLine();

            //3. Get access tokens
            //After calling this function We can refer to them through 'tokensResponse["oauth_token"] and 'tokensResponse["oauth_token_secret"]
            //Also after calling this function tokens already exist in config instance
            //Note: Once created, the Access token never expires.
            OAuthUpworkResponse accessToken = await client.GetAccessToken(oauth_verifier);
            OAuthToken = accessToken["oauth_token"];
            OAuthTokenSecret = accessToken["oauth_token_secret"];

            //4. Create Upwork API instance
            IUpwork upworkApi = new Upwork(client);

            // 5. Get Categories & Subcategories
            List<Category> categories = await upworkApi.GetCategories();

            // 6. Get User Info
            UpworkUser currentUser = await upworkApi.GetUserInfo();

            // 7. Get User teams
            List<UserTeam> teams = await upworkApi.GetUserTeams();

            // 8. Get available skills
            List<string> skills = await upworkApi.GetSkills();

            // 8. Post job
            string categoryTitle = categories.FirstOrDefault().Title;
            string subCategoryTitle = categories.FirstOrDefault().Topics.FirstOrDefault().Title;

            UpworkJob job = new UpworkJob("New Job2", teams.FirstOrDefault().Reference, "Test description2", categoryTitle, subCategoryTitle, "fixed-price", "pulic", skills.FirstOrDefault(), null, 50);
            JobInfo postedJob = await upworkApi.PostJob(job);

            Console.WriteLine($"New job URL: {postedJob.PublicUrl}");

            Console.ReadKey();
        }
    }
}
