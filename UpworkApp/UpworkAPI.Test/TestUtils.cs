using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpworkAPI.Test
{
    public static class TestUtils
    {
        public static OAuthConfig GetOauthConfig() => new OAuthConfig("test_consumer_key", "test_secret_Key", "oauth-token", "oauth-token-secret");

        public static OAuthClient GetOAuthClient() => new OAuthClient(GetOauthConfig());

        public static string GetUserInfoJsonString() => "{\"server_time\":\"1538726059\",\"auth_user\":{\"first_name\":\"Test_Name\",\"last_name\":\"Test_Last_Name\",\"timezone\":\"America\\/Tijuana\",\"timezone_offset\":\"-25200\"},\"info\":{\"portrait_100_img\":\"\",\"has_agency\":\"0\",\"company_url\":\"\",\"portrait_32_img\":\"\",\"ref\":\"9999\",\"portrait_50_img\":\"\",\"capacity\":{\"provider\":\"no\",\"buyer\":\"yes\",\"affiliate_manager\":\"no\"},\"location\":{\"city\":\"\",\"state\":\"\",\"country\":\"United States\"},\"profile_url\":\"https:\\/\\/www.testurl.com\"}}";

        public static string GetUserTeamsJsonString() => "{\"server_time\":\"1538727697\",\"auth_user\":{\"first_name\":\"First_Name\",\"last_name\":\"Last_Name\",\"timezone\":\"America\\/Tijuana\",\"timezone_offset\":\"-25200\"},\"teams\":[{\"parent_team__reference\":\"111111\",\"name\":\"Team Test Account\",\"company__reference\":\"0909887\",\"id\":\"q9q9q9q9q9q9q9q9\",\"company_name\":\"Company Test Name\",\"parent_team__id\":\"z8z8z8z8z8z8z8\",\"reference\":\"7771777\",\"parent_team__name\":\"Parent Team Test Account\"}]}";

        public static string GetCategoriesJsonString() => "{\"server_time\":1538727636,\"auth_user\":{\"first_name\":\"Eibot\",\"last_name\":\"Agents\",\"timezone\":\"America\\/Tijuana\",\"timezone_offset\":\"-25200\"},\"categories\":[{\"title\":\"Web, Mobile & Software Dev\",\"id\":\"531770282580668418\",\"topics\":[{\"title\":\"Desktop Software Development\",\"id\":\"531770282589057025\"},{\"title\":\"Ecommerce Development\",\"id\":\"531770282589057026\"},{\"title\":\"Game Development\",\"id\":\"531770282589057027\"},{\"title\":\"Mobile Development\",\"id\":\"531770282589057024\"},{\"title\":\"Product Management\",\"id\":\"531770282589057030\"},{\"title\":\"QA & Testing\",\"id\":\"531770282589057031\"},{\"title\":\"Scripts & Utilities\",\"id\":\"531770282589057028\"},{\"title\":\"Web Development\",\"id\":\"531770282584862733\"},{\"title\":\"Web & Mobile Design\",\"id\":\"531770282589057029\"},{\"title\":\"Other - Software Development\",\"id\":\"531770282589057032\"}]}]}";

        public static string GetPostedJobJsonString() => "{'auth_user': {'first_name': 'John','last_name': 'Johnson', 'timezone': 'Asia/Omsk', 'timezone_offset': '25200'}, 'job': {'public_url': 'https://joburl.com','reference': '~aaa999f4c68af61ed6'},'server_time': 1404364847}";
    }
}
