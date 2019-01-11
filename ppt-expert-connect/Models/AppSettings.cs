using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PPTExpertConnect.Models
{
    public class AppSettings
    {
        public string Url { get; set; }
        public string AssetsPath { get; set; }

        public string GetImageUrlFromLocation(string location)
        {
            return Url + AssetsPath + location;
        }
    }
}
