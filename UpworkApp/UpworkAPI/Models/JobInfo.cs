using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace UpworkAPI.Models
{
    public class JobInfo
    {
        [JsonProperty(PropertyName = "reference")]
        public string Reference { get; set; }

        [JsonProperty(PropertyName = "public_url")]
        public string PublicUrl { get; set; }
    }
}
