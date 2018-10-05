using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UpworkAPI.Interfaces;

namespace UpworkAPI
{
    public class OAuthClient : IOAuthClient
    {
        /// <summary>
        /// OAuth configuration
        /// </summary>
        OAuthConfig _config;

        readonly DateTime epochUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Random instance to generate nonce
        /// </summary>
        private Random _random;

        /// <summary>
        /// Hasher
        /// </summary>
        private HMACSHA1 sigHasher;

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="config">OAuth 1.0 config</param>
        /// <exception cref="System.ArgumentNullException">Thrown when config parameter is missing.</exception>
        public OAuthClient(OAuthConfig config)
        {
            _config = config ?? throw new ArgumentNullException("config");

            _random = new Random();
            sigHasher = new HMACSHA1(new ASCIIEncoding().GetBytes(string.Format("{0}&{1}", _config.ConsumerSecret, _config.OAuthTokenSecret)));
        }

        /// <summary>
        /// Get request tokens for upwork applications
        /// </summary>
        /// <returns>OAuthUpworkResponse instance with tokens.</returns>
        public async Task<OAuthUpworkResponse> GetRequestTokens()
        {
            string tokenResponse = await SendRequest(OAuthConfig.RequestTokenUrl, "POST", new Dictionary<string, string>());
            OAuthUpworkResponse oauthResponse = new OAuthUpworkResponse(tokenResponse);
            try
            {
                _config.OAuthToken = oauthResponse["oauth_token"];
                _config.OAuthTokenSecret = oauthResponse["oauth_token_secret"];
                sigHasher = new HMACSHA1(new ASCIIEncoding().GetBytes(string.Format("{0}&{1}", _config.ConsumerSecret, _config.OAuthTokenSecret)));
            }
            catch(Exception ex) {
                throw new Exception($"Cannot get request tokens: {ex.Message}");
            }
            return oauthResponse;
        }

        /// <summary>
        /// Get OAuth 1.0 access token
        /// </summary>
        /// <param name="verifier">Verify code</param>
        /// <returns></returns>
        public async Task<OAuthUpworkResponse> GetAccessToken(string verifier)
        {
            string tokenResponse = await SendRequest(OAuthConfig.AccessTokenUrl, "POST", new Dictionary<string, string> { { "oauth_verifier", verifier } });
            OAuthUpworkResponse oauthResponse = new OAuthUpworkResponse(tokenResponse);
            try
            {
                _config.OAuthToken = oauthResponse["oauth_token"];
                _config.OAuthTokenSecret = oauthResponse["oauth_token_secret"];
                sigHasher = new HMACSHA1(new ASCIIEncoding().GetBytes(string.Format("{0}&{1}", _config.ConsumerSecret, _config.OAuthTokenSecret)));
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot get access tokens: {ex.Message}");
            }
            return oauthResponse;
        }

        /// <summary>
        /// Generate OAuth headers, send HTTP Request and return the response
        /// </summary>
        /// <param name="url">Full request url</param>
        /// <param name="data">Request data</param>
        /// <returns>OAuth 1.0 headers string</returns>
        public Task<string> SendRequest(string url, string method, Dictionary<string, string> data)
        {

            // Timestamps are in seconds since 1/1/1970.
            var timestamp = (int)((DateTime.UtcNow - epochUtc).TotalSeconds);

            // Add all the OAuth headers we'll need to use when constructing the hash.
            data.Add("oauth_consumer_key", _config.ConsumerKey);
            data.Add("oauth_signature_method", "HMAC-SHA1");
            data.Add("oauth_timestamp", timestamp.ToString());
            data.Add("oauth_nonce", GenerateNonce());
            data.Add("oauth_token", _config.OAuthToken);
            //data.Add("oauth_version", "1.0");

            // Generate the OAuth signature and add it to our payload.
            data.Add("oauth_signature", GenerateSignature(url, method, data));

            // Build the OAuth HTTP Header from the data.
            string oAuthHeader = GenerateOAuthHeader(data);

            // Build the form data (exclude OAuth stuff that's already in the header).
            var formData = new FormUrlEncodedContent(data.Where(kvp => !kvp.Key.StartsWith("oauth_")));

            return SendRequest(url, method, oAuthHeader, formData);
        }

        /// <summary>
        /// Send HTTP Request and return the response.
        /// </summary>
        /// <param name="fullUrl">Full request URL</param>
        /// <param name="fullUrl">HTTP request method - 'GET' or 'POST'</param>
        /// <param name="oAuthHeader">String wirh OAuth 1.0 header params</param>
        /// <param name="formData">Request data</param>
        /// <returns></returns>
        async Task<string> SendRequest(string fullUrl, string method, string oAuthHeader, FormUrlEncodedContent formData)
        {
            using (var http = new HttpClient())
            {
                http.DefaultRequestHeaders.Add("Authorization", oAuthHeader);
                string respBody = "";
                if (method == "GET")
                {
                    var httpResp = await http.GetAsync(fullUrl);
                    respBody = await httpResp.Content.ReadAsStringAsync();
                }
                else
                {
                    var httpResp = await http.PostAsync(fullUrl, formData);
                    respBody = await httpResp.Content.ReadAsStringAsync();
                }
                return respBody;
            }
        }

        /// <summary>
        /// Generate request signature.
        /// </summary>
        /// <param name="url">Full request URL</param>
        /// <param name="method">Request method</param>
        /// <param name="data">Request data</param>
        /// <returns>Returns System.String with request signature</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when request data is null</exception>
        public string GenerateSignature(string url, string method, Dictionary<string, string> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var sigString = string.Join(
                "&",
                data
                    .Union(data)
                    .Select(kvp => string.Format("{0}={1}", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value.Replace(" ","+"))))
                    .OrderBy(s => s)
            );

            var fullSigData = string.Format(
                "{0}&{1}&{2}",
                method,
                Uri.EscapeDataString(url),
                Uri.EscapeDataString(sigString.ToString())
            );

            return Convert.ToBase64String(sigHasher.ComputeHash(new ASCIIEncoding().GetBytes(fullSigData.ToString())));
        }

        /// <summary>
        /// Generate the raw OAuth HTML header from the values (including signature).
        /// </summary>
        /// <param name="data">Request Auth data</param>
        /// <returns>System.String with OAuth header</returns>
        /// <exception cref="System.ArgumentNullException">Thrown when Request Auth data is null</exception>
        public string GenerateOAuthHeader(Dictionary<string, string> data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            return "OAuth " + string.Join(
                ", ",
                data
                    .Where(kvp => kvp.Key.StartsWith("oauth_"))
                    .Select(kvp => string.Format("{0}=\"{1}\"", Uri.EscapeDataString(kvp.Key), Uri.EscapeDataString(kvp.Value)))
                    .OrderBy(s => s)
            );
        }

        /// <summary>
        /// Generate an oauth nonce.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     According to RFC 5849, A nonce is a random string,
        ///     uniquely generated by the client to allow the server to
        ///     verify that a request has never been made before and
        ///     helps prevent replay attacks when requests are made over
        ///     a non-secure channel.  The nonce value MUST be unique
        ///     across all requests with the same timestamp, client
        ///     credentials, and token combinations.
        ///   </para>
        ///   <para>
        ///     One way to implement the nonce is just to use a
        ///     monotonically-increasing integer value.  It starts at zero and
        ///     increases by 1 for each new request or signature generated.
        ///     Keep in mind the nonce needs to be unique only for a given
        ///     timestamp!  So if your app makes less than one request per
        ///     second, then using a static nonce of "0" will work.
        ///   </para>
        ///   <para>
        ///     Most oauth nonce generation routines are waaaaay over-engineered,
        ///     and this one is no exception.
        ///   </para>
        /// </remarks>
        /// <returns>the nonce</returns>
        public string GenerateNonce()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 8; i++)
            {
                int g = _random.Next(3);
                switch (g)
                {
                    case 0:
                        // lowercase alpha
                        sb.Append((char)(_random.Next(26) + 97), 1);
                        break;
                    default:
                        // numeric digits
                        sb.Append((char)(_random.Next(10) + 48), 1);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
