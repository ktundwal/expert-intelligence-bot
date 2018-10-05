using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace UpworkAPI.Models
{
    public class UserTeam
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "company_name")]
        public string CompanyName { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "reference")]
        public string Reference { get; set; }

        [JsonProperty(PropertyName = "company__reference")]
        public string CompanyReference { get; set; }

        [JsonProperty(PropertyName = "parent_team__name")]
        public string ParentTeamName { get; set; }

        [JsonProperty(PropertyName = "parent_team__id")]
        public string ParentTeamId { get; set; }

        [JsonProperty(PropertyName = "parent_team__reference")]
        public string ParentTeamReference { get; set; }
    }
}
