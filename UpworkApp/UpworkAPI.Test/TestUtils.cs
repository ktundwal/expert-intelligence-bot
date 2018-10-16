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

        public static string GetJobsListJsonString() => "{'jobs':{'lister': {'paging': {'count':'20', 'offset':'0'},'query': '','sort': {'sort': {'sort': ['created_time', 'asc']}},'total_items': '5'}, 'job': [{'attachment_file_url': '','budget': '5','buyer_company__name': 'My Company','buyer_company__reference': '1040945','buyer_team__name': 'My Company','buyer_team__reference': '1040945','cancelled_date': '1380067200000','category2': 'Web, Mobile & Software Dev','created_time': '1377423220000','description': 'Testing some functionality','duration': '','end_date': '1380067200000','filled_date': '','job_ref_ciphertext': '~12345abcdf','job_type': 'fixed-price','keep_open_on_hire': '','num_active_candidates': '0', 'num_candidates': '0','num_new_candidates': '0','preference_candidate_type': 'individuals', 'public_url': 'https://...', 'reference': '~12345abcdf','skills': '','start_date': '1377388800000', 'status': 'cancelled','subcategory2': 'Web & Mobile Development','title': 'Test python-upwork','visibility': 'invite-only'}]}}";

        public static string GetEngagementsListJsonString() => "{'engagements':{'engagement': {'engagement_start_date':'1516147200000','job_ref_ciphertext':'~01e55e24e...','status':'closed','provider__reference':'12345','engagement_job_type':'fixed-price','offer_id':'1234','job__title':'Embedded login page with Python','cj_job_application_uid':'123456789012345678',    'provider_team__id':'','fixed_charge_amount_agreed':'150','job_application_ref':'','dev_recno_ciphertext':'~0192ebf.......',    'reference':'12345','active_milestone':'','engagement_end_ts':'1516215373000','provider__id':'providerid','engagement_title':'Embedded login page with Python','created_time':'1516172330000','engagement_end_date':'1516147200000','provider_team__reference':'', 'engagement_start_ts':'1516172329000','buyer_team__reference':'234','buyer_team__id':'xsdf-6sdfsyuia...', 'fixed_price_upfront_payment':'',    'portrait_url':'https://url.com/Users:auser:PortraitUrl_original?AWSAccessKyId=AK7&Signature=8k%3D','feedback':{'feedback_for_provider':{'score':'4.5 - 5.0 Stars'},'feedback_for_buyer':{'score':'4.5 - 5.0 Stars'}}},'lister': {'paging': {'count': '20', 'offset': '0'},'query': '','sort': {'sort': {'sort': ['created_time', 'asc']}},'total_count': '1','total_items': '1'}}}";
    }
}
