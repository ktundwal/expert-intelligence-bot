using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Microsoft.ExpertConnect.Helpers
{
    public class Helper
    {
        public static string GetValueFromConfiguration(IConfiguration config, string key)
        {
            return config.GetSection(key)?.Value;
        }

        public static string GetAssetLocationUrl(string url, string assetPath, string location)
        {
            return url + assetPath + location;
        }
    }
}
