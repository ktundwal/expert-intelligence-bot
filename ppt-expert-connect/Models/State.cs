using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.Models
{
    public class State
    {
    }

    /// <summary>
    /// User state information.
    /// </summary>
    public class UserInfo
    {
        public string Introduction { get; set; }
        public string Purpose { get; set; }
        public string Style { get; set; }
        public string Color { get; set; }
        public string Visuals { get; set; }
        public string Images { get; set; }
        public string Extra { get; set; }
        public int Rating { get; set; }
        public string Feedback { get; set; }
    }
}
