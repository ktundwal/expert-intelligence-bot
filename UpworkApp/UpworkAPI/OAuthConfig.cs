using System;
using System.Collections.Generic;
using System.Text;

namespace UpworkAPI
{
    /// <summary>
    /// OAuth 1.0 Configuration class
    /// Contains consumer keys and tokens
    /// </summary>
    public class OAuthConfig
    {
        /// <summary>
        /// API consumer key
        /// </summary>
        public readonly string ConsumerKey = "";
        /// <summary>
        /// API consumer secret key
        /// </summary>
        public readonly string ConsumerSecret = "";
        /// <summary>
        /// OAuth token
        /// </summary>
        public string OAuthToken = "";
        /// <summary>
        /// OAuth secret token
        /// </summary>
        public string OAuthTokenSecret = "";

        public const string RequestTokenUrl = "https://www.upwork.com/api/auth/v1/oauth/token/request";
        public const string AccessTokenUrl = "https://www.upwork.com/api/auth/v1/oauth/token/access";
        public const string AuthorizeUrl = "https://www.upwork.com/services/api/auth";

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="consumerKey">A consumer (application) key</param>
        /// <param name="consumerSecret">A consumer (application) secret key</param>
        /// <param name="oAuthToken">Application OAuth token. Leave it empty('') if not have token yet</param>
        /// <param name="oAuthTokenSecret">Application OAuth secret token. Leave it empty('') if not have token yet</param>
        /// <exception cref="System.ArgumentNullException">Thrown when one of required parameters is missing</exception>
        public OAuthConfig(string consumerKey, string consumerSecret, string oAuthToken, string oAuthTokenSecret)
        {
            if (String.IsNullOrEmpty(consumerKey))
                throw new ArgumentNullException("consumerKey");
            if (String.IsNullOrEmpty(consumerSecret))
                throw new ArgumentNullException("consumerSecret");

            ConsumerKey = consumerKey;
            ConsumerSecret = consumerSecret;
            OAuthToken = oAuthToken;
            OAuthTokenSecret = oAuthTokenSecret;
        }
    }
}
