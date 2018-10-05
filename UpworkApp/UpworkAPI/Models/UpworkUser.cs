using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace UpworkAPI.Models
{
    public class UpworkUser
    {
        [JsonProperty(PropertyName = "auth_user")]
        public AuthUser AuthUser { get; set; }

        [JsonProperty(PropertyName = "info")]
        public UserInfo Info { get; set; }
    }

    public class AuthUser
    {
        [JsonProperty(PropertyName = "first_name")]
        public string FirstName { get; set; }

        [JsonProperty(PropertyName = "last_name")]
        public string LastName { get; set; }
    }

    public class UserInfo
    {
        [JsonProperty(PropertyName = "ref")]
        public string Ref { get; set; }
        [JsonProperty(PropertyName = "company_url")]
        public string CompanyUrl { get; set; }
    }
}
