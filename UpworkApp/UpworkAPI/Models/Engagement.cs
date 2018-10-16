using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace UpworkAPI.Models
{
    public class Engagement
    {
        [JsonProperty(PropertyName = "engagement_start_date")]
        public string StartDate { get; set; }

        [JsonProperty(PropertyName = "engagement_end_date")]
        public string EndDate { get; set; }

        [JsonProperty(PropertyName = "engagement_start_ts")]
        public string StartDateTime { get; set; }

        [JsonProperty(PropertyName = "engagement_end_ts")]
        public string EndDateTime { get; set; }

        [JsonProperty(PropertyName = "created_time")]
        public string CreatedTime { get; set; }

        [JsonProperty(PropertyName = "job_ref_ciphertext")]
        public string JobsProfileId { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "provider__reference")]
        public string ProviderReference { get; set; }

        [JsonProperty(PropertyName = "engagement_job_type")]
        public string EngagementJobType { get; set; }

        [JsonProperty(PropertyName = "offer_id")]
        public string OfferId { get; set; }

        [JsonProperty(PropertyName = "job__title")]
        public string JobTitle { get; set; }

        [JsonProperty(PropertyName = "cj_job_application_uid")]
        public string RelatedJobApplicationId { get; set; }

        [JsonProperty(PropertyName = "provider_team__id")]
        public string ProviderTeamId { get; set; }

        [JsonProperty(PropertyName = "fixed_charge_amount_agreed")]
        public decimal? FixedChargeAmountAgreed { get; set; }

        [JsonProperty(PropertyName = "job_application_ref")]
        public string JobApplicationReference { get; set; }

        [JsonProperty(PropertyName = "dev_recno_ciphertext")]
        public string ContractorsProfileCiphertext { get; set; }

        [JsonProperty(PropertyName = "reference")]
        public string Reference { get; set; }

        [JsonProperty(PropertyName = "provider__id")]
        public string ProviderId { get; set; }

        [JsonProperty(PropertyName = "engagement_title")]
        public string EngagementTitle { get; set; }

        [JsonProperty(PropertyName = "provider_team__reference")]
        public string ProviderTeamReference { get; set; }

        [JsonProperty(PropertyName = "buyer_team__reference")]
        public string BuyerTeamReference { get; set; }

        [JsonProperty(PropertyName = "buyer_team__id")]
        public string BuyerTeamId { get; set; }

        [JsonProperty(PropertyName = "fixed_price_upfront_payment")]
        public decimal? FixedPriceUpfrontPayment { get; set; }

        [JsonProperty(PropertyName = "portrait_url")]
        public string PortraitUrl { get; set; }
    }
}
