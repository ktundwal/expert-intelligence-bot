using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ConfigurationManager = System.Configuration.ConfigurationManager;

namespace Microsoft.Office.EIBot.Service.FancyHands
{
    public class FancyHandsApi
    {

        private const string ApiCustom = "https://www.fancyhands.com/api/v1/request/custom/";
        private const string ApiRealtimeRequest = "https://www.fancyhands.com/api/v1/realtime/request/";
        private const string ApiRealtimeMessage = "https://www.fancyhands.com/api/v1/realtime/message/";
        private const string ApiRealtimeClose = "https://www.fancyhands.com/api/v1/realtime/close/";
        private const string ApiCancel = "https://www.fancyhands.com/api/v1/request/custom/cancel/";
        private const string ApiCallback = "https://www.fancyhands.com/api/v1/callback/";
        private const string HardcodedRequestKeyForTesting = "ahBzfmZhbmN5aGFuZHMtaHJkcikLEgZGSFVzZXIYgICI0s_t_wgMCxIJRkhSZXF1ZXN0GICAgICEoJMKDA";

        public static FancyHandsApi BuildClient()
        {
            var api = new FancyHandsApi
            {
                ["oauth_consumer_key"] = ConfigurationManager.AppSettings["FancyHandsConsumerKey"],
                ["oauth_consumer_secret"] = ConfigurationManager.AppSettings["FancyHandsConsumerSecret"]
            };

            return api ;
        }

        /// <summary>
        ///   The default public constructor.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Initializes various fields to default values.
        ///   </para>
        /// </remarks>
        public FancyHandsApi()
        {
            _random = new Random();
            _params = new Dictionary<string, string>
            {
                ["oauth_consumer_key"] = "",
                ["oauth_consumer_secret"] = "",
                ["oauth_timestamp"] = GenerateTimeStamp(),
                ["oauth_nonce"] = GenerateNonce(),
                ["oauth_signature_method"] = "HMAC-SHA1",
                ["test"] = "1"
            };
            //  _params["oauth_callback"] = "oob"; // presume "desktop" consumer
            //  _params["oauth_signature"] = "";
            //  _params["token"] = "";
            //  _params["token_secret"] = "";
            //  _params["oauth_version"] = "1.0";
        }

        /// <summary>
        ///   The constructor to use when using OAuth when you already
        ///   have an OAuth access token.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     The parameters for this constructor all have the
        ///     meaning you would expect.  The token and tokenSecret
        ///     are set in oauth_token, and oauth_token_secret.
        ///     These are *Access* tokens, obtained after a call
        ///     to AcquireAccessToken.  The application can store
        ///     those tokens and re-use them on successive runs.
        ///     For twitter at least, the access tokens never expire.
        ///   </para>
        /// </remarks>
        public FancyHandsApi(string consumerKey,
                       string consumerSecret,
                       string token,
                       string tokenSecret) : this()

        {
            _params["oauth_consumer_key"] = consumerKey;
            _params["oauth_consumer_secret"] = consumerSecret;
            //_params["token"] = token;
            //_params["token_secret"] = tokenSecret;
        }

        /// <summary>
        ///   string indexer to get or set oauth parameter values.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Use the parameter name *without* the oauth_ prefix.
        ///     If you want to set the value for the oauth_token parameter
        ///     field in an HTTP message, then use oauth["token"].
        ///   </para>
        ///   <para>
        ///     The set of oauth param names known by this indexer includes:
        ///     callback, consumer_key, consumer_secret, timestamp, nonce,
        ///     signature_method, signature, token, token_secret, and version.
        ///   </para>
        ///   <para>
        ///     If you try setting a parameter with a name that is not known,
        ///     the setter will throw.  You cannot add new oauth parameters
        ///     using the setter on this indexer.
        ///   </para>
        /// </remarks>
        public string this[string ix]
        {
            get
            {
                if (_params.ContainsKey(ix))
                    return _params[ix];
                throw new ArgumentException(ix);
            }
            set
            {
                if (!_params.ContainsKey(ix))
                    throw new ArgumentException(ix);
                _params[ix] = value;
            }
        }

        /// <summary>
        /// Generate the timestamp for the signature.
        /// </summary>
        /// <returns>The timestamp, in string form.</returns>
        private string GenerateTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - _epoch;
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }


        /// <summary>
        ///   Renews the nonce and timestamp on the oauth parameters.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     Each new request should get a new, current timestamp, and a
        ///     nonce. This helper method does both of those things. This gets
        ///     called before generating an authorization header, as for example
        ///     when the user of this class calls <see cref='AcquireRequestToken'>.
        ///   </para>
        /// </remarks>
        private void NewRequest()
        {
            _params["oauth_nonce"] = GenerateNonce();
            _params["oauth_timestamp"] = GenerateTimeStamp();
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
        private string GenerateNonce()
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

        /// <summary>
        /// Internal function to extract from a URL all query string
        /// parameters that are not related to oauth - in other words all
        /// parameters not begining with "oauth_".
        /// </summary>
        ///
        /// <remarks>
        ///   <para>
        ///     For example, given a url like http://foo?a=7&guff, the
        ///     returned value will be a Dictionary of string-to-string
        ///     relations.  There will be 2 entries in the Dictionary: "a"=>7,
        ///     and "guff"=>"".
        ///   </para>
        /// </remarks>
        ///
        /// <param name="queryString">The query string part of the Url</param>
        ///
        /// <returns>A Dictionary containing the set of
        /// parameter names and associated values</returns>
        private Dictionary<string, string> ExtractQueryParameters(string queryString)
        {
            if (queryString.StartsWith("?"))
                queryString = queryString.Remove(0, 1);

            var result = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(queryString))
                return result;

            foreach (string s in queryString.Split('&'))
            {
                if (!string.IsNullOrEmpty(s) && !s.StartsWith("oauth_"))
                {
                    if (s.IndexOf('=') > -1)
                    {
                        string[] temp = s.Split('=');
                        result.Add(temp[0], temp[1]);
                    }
                    else
                        result.Add(s, string.Empty);
                }
            }

            return result;
        }

        /// <summary>
        ///   This is an oauth-compliant Url Encoder.  The default .NET
        ///   encoder outputs the percent encoding in lower case.  While this
        ///   is not a problem with the percent encoding defined in RFC 3986,
        ///   OAuth (RFC 5849) requires that the characters be upper case
        ///   throughout OAuth.
        /// </summary>
        ///
        /// <param name="value">The value to encode</param>
        ///
        /// <returns>the Url-encoded version of that string</returns>
        public static string UrlEncode(string value)
        {
            var result = new System.Text.StringBuilder();
            foreach (char symbol in value)
            {
                if (unreservedChars.IndexOf(symbol) != -1)
                    result.Append(symbol);
                else
                    result.Append('%' + $"{(int) symbol:X2}");
            }
            return result.ToString();
        }
        private static string unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        //private static string reservedChars = "+~";

        /// <summary>
        /// Formats the list of request parameters into string a according
        /// to the requirements of oauth. The resulting string could be used
        /// in the Authorization header of the request.
        /// </summary>
        ///
        /// <remarks>
        ///   <para>
        ///     See http://dev.twitter.com/pages/auth#intro  for some
        ///     background.  The output of this is not suitable for signing.
        ///   </para>
        ///   <para>
        ///     There are 2 formats for specifying the list of oauth
        ///     parameters in the oauth spec: one suitable for signing, and
        ///     the other suitable for use within Authorization HTTP Headers.
        ///     This method emits a string suitable for the latter.
        ///   </para>
        /// </remarks>
        ///
        /// <param name="parameters">The Dictionary of
        /// parameters. It need not be sorted.</param>
        ///
        /// <returns>a string representing the parameters</returns>
        private static string EncodeRequestParameters(ICollection<KeyValuePair<string, string>> p)
        {
            var sb = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, string> item in p.OrderBy(x => x.Key))
            {
                if (!string.IsNullOrEmpty(item.Value) && !item.Key.EndsWith("secret"))
                {
                    sb.AppendFormat("{0}={1}&",
                                    UrlEncode(item.Key),
                                    UrlEncode(item.Value));
                }

            }

            return sb.ToString();
        }

        public async Task<OAuthResponse> RequestApiget(string uri, string method, string key)
        {

            NewRequest();
            _params["key"] = key;

            var signature = GetSignature(uri, method);
            _params["oauth_signature"] = signature;

            var authzHeader = $"OAuth {GetAuthorizationHeader(uri, method)}";
            var fullUrl = $"{uri}?{EncodeRequestParameters(_params)}";
            System.Diagnostics.Debug.WriteLine("\nfullUrl: ");
            System.Diagnostics.Debug.WriteLine(fullUrl);

            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(fullUrl);
            request.Method = method;
            // request.Headers.Add("Authorization", authzHeader);
            System.Diagnostics.Debug.WriteLine("Authorization: ");
            System.Diagnostics.Debug.WriteLine(authzHeader);

            try
            {
                using (var response = await request.GetResponseAsync())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var r = new OAuthResponse(await reader.ReadToEndAsync());
                        return r;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(
                    $"Couldn't get ret response stream from fancy hands api. " +
                    $"Method={method} Key={key}", e);
            }
        }


        public OAuthResponse CancelRequest(string uri, string method, string key)
        {
            NewRequest();
            _params["key"] = key;

            var signature = GetSignature(uri, method);
            _params["oauth_signature"] = signature;

            var erp = EncodeRequestParameters(this._params);
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(uri);
            request.Method = method;

            byte[] byteArray = Encoding.UTF8.GetBytes(erp);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            using (var response = (System.Net.HttpWebResponse)request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream() ?? throw new Exception(
                                                                   $"Couldn't get ret response stream from fancy hands api. " +
                                                                   $"Method={method} Key={key}")))
                {
                    var r = new OAuthResponse(reader.ReadToEnd());
                    return r;
                }
            }
        }

        public OAuthResponse SendMessage(string uri, string method, string key, string message)
        {
            NewRequest();
            _params["key"] = key;
            _params["request_key"] = key;
            _params["content"] = message;

            var signature = GetSignature(uri, method);
            _params["oauth_signature"] = signature;

            var erp = EncodeRequestParameters(this._params);
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(uri);
            request.Method = method;

            byte[] byteArray = Encoding.UTF8.GetBytes(erp);
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            using (var response = (System.Net.HttpWebResponse)request.GetResponse())
            {
                using (var reader = new StreamReader(response.GetResponseStream() ?? throw new Exception(
                                                         $"Couldn't get ret response stream from fancy hands api. " +
                                                         $"Method={method} Key={key}")))
                {
                    var r = new OAuthResponse(reader.ReadToEnd());
                    return r;
                }
            }
        }

        public OAuthResponse1 RequestAPI(string uri, string method, string title, string description, float bid, DateTime date)
        {
            NewRequest();
            _params["title"] = title;
            _params["description"] = description;
            _params["bid"] = bid.ToString();
            _params["expiration_date"] = date.ToString("yyyy-MM-ddTHH:mm:ssZ");
            // _params["test"] = "true";

            // var authzHeader = GetAuthorizationHeader(uri, method);
            var signature = GetSignature(uri, method);
            _params["oauth_signature"] = signature;

            var erp = EncodeRequestParameters(this._params);

            // prepare the token request
            var request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(uri);
            // request.Headers.Add("Authorization", authzHeader);
            request.Method = method;

            byte[] byteArray = Encoding.UTF8.GetBytes(erp);
            System.Diagnostics.Debug.WriteLine("Signature:");
            System.Diagnostics.Debug.WriteLine(signature);
            System.Diagnostics.Debug.WriteLine("ERP:");
            System.Diagnostics.Debug.WriteLine(erp);

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = byteArray.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(byteArray, 0, byteArray.Length);
            dataStream.Close();

            using (var response = (System.Net.HttpWebResponse)request.GetResponse())
            {
                using (var reader = new StreamReader(response?.GetResponseStream() ??
                                                     throw new Exception(
                                                         $"Couldn't get ret response stream from fancy hands api. " +
                                                         $"Method={method} title={title}")))
                {
                    var r = new OAuthResponse1(reader.ReadToEnd());
                    return r;
                }
            }
        }


        /// <summary>
        ///   Generate a string to be used in an Authorization header in
        ///   an HTTP request.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This method assembles the available oauth_ parameters that
        ///     have been set in the Dictionary in this instance, produces
        ///     the signature base (As described by the OAuth spec, RFC 5849),
        ///     signs it, then re-formats the oauth_ parameters into the
        ///     appropriate form, including the oauth_signature value, and
        ///     returns the result.
        ///   </para>
        ///   <para>
        ///     If you pass in a non-null, non-empty realm, this method will
        ///     include the realm='foo' clause in the Authorization header.
        ///   </para>
        /// </remarks>
        ///
        /// <seealso cref='GenerateAuthzHeader'>
        public string GenerateCredsHeader(string uri, string method, string realm)
        {
            NewRequest();
            var authzHeader = GetAuthorizationHeader(uri, method, realm);
            return authzHeader;
        }

        public string GenerateAuthzHeader(string uri, string method)
        {
            NewRequest();
            var authzHeader = GetAuthorizationHeader(uri, method, null);
            return authzHeader;
        }

        private string GetAuthorizationHeader(string uri, string method)
        {
            return GetAuthorizationHeader(uri, method, null);
        }

        private string GetAuthorizationHeader(string uri, string method, string realm)
        {
            if (string.IsNullOrEmpty(this._params["oauth_consumer_key"]))
                throw new ArgumentNullException("oauth_consumer_key");

            if (string.IsNullOrEmpty(this._params["oauth_signature_method"]))
                throw new ArgumentNullException("oauth_signature_method");

            //Sign(uri, method);

            // var erp = EncodeRequestParameters(this._params);
            var p = this._params;
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, string> item in p.OrderBy(x => x.Key))
            {
                if (item.Key.StartsWith("oauth_"))
                {
                    sb.AppendFormat("{0}={1},",
                                    UrlEncode(item.Key),
                                    UrlEncode(item.Value));
                }
            }


            return sb.ToString().TrimEnd(',');
        }

        private string GetSignature(string uri, string method)
        {
            var signatureBase = GetSignatureBase(uri, method);

            System.Diagnostics.Debug.WriteLine("signatureBase:");
            System.Diagnostics.Debug.WriteLine(signatureBase);

            HashAlgorithm hash = GetHash();

            byte[] dataBuffer = Encoding.ASCII.GetBytes(signatureBase);
            byte[] hashBytes = hash.ComputeHash(dataBuffer);

            return Convert.ToBase64String(hashBytes);
        }

        private void Sign(string uri, string method)
        {
            var signatureBase = GetSignatureBase(uri, method);

            HashAlgorithm hash = GetHash();

            byte[] dataBuffer = Encoding.ASCII.GetBytes(signatureBase);
            byte[] hashBytes = hash.ComputeHash(dataBuffer);

            this["oauth_signature"] = Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Formats the list of request parameters into "signature base" string as
        /// defined by RFC 5849.  This will then be MAC'd with a suitable hash.
        /// </summary>
        private string GetSignatureBase(string url, string method)
        {
            // normalize the URI
            var uri = new Uri(url);
            var normUrl = $"{uri.Scheme}://{uri.Host}";
            if (!((uri.Scheme == "http" && uri.Port == 80) ||
                  (uri.Scheme == "https" && uri.Port == 443)))
                normUrl += ":" + uri.Port;

            normUrl += uri.AbsolutePath;

            // the sigbase starts with the method and the encoded URI
            var sb = new System.Text.StringBuilder();
            sb.Append(method)
                .Append('&')
                .Append(UrlEncode(normUrl))
                .Append('&');

            // the parameters follow - all oauth params plus any params on
            // the uri
            // each uri may have a distinct set of query params
            var p = ExtractQueryParameters(uri.Query);
            // add all non-empty params to the "current" params
            foreach (var p1 in this._params)
            {
                // Exclude all oauth params that are secret or
                // signatures; any secrets should be kept to ourselves,
                // and any existing signature will be invalid.


                if (!String.IsNullOrEmpty(this._params[p1.Key]) &&
                    !p1.Key.EndsWith("_secret") &&
                    !p1.Key.EndsWith("signature"))
                {
                    p.Add(p1.Key, p1.Value);
                }
            }

            // concat+format all those params
            var sb1 = new System.Text.StringBuilder();
            foreach (KeyValuePair<String, String> item in p.OrderBy(x => x.Key))
            {
                // even "empty" params need to be encoded this way.
                //sb1.AppendFormat("{0}={1}&", item.Key, item.Value);
                sb1.Append(UrlEncode(item.Key));
                sb1.Append(UrlEncode("="));
                sb1.Append(UrlEncode(UrlEncode(item.Value)));
                sb1.Append(UrlEncode("&"));
                // sb1.AppendFormat("{0}={1}&", item.Key, UrlEncode(item.Value));
            }
            // append the UrlEncoded version of that string to the sigbase
            // sb.Append(UrlEncode(sb1.ToString().TrimEnd('&')));
            var s = sb1.ToString();
            sb.Append(s.Remove(s.LastIndexOf("%")));
            var result = sb.ToString();
            return result;
        }

        private HashAlgorithm GetHash()
        {
            if (this["oauth_signature_method"] != "HMAC-SHA1")
                throw new NotImplementedException();

            string keystring = $"{UrlEncode(this["oauth_consumer_secret"])}&";

            var hmacsha1 = new HMACSHA1
            {
                Key = System.Text.Encoding.ASCII.GetBytes(keystring)
            };
            return hmacsha1;
        }

        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private Dictionary<String, String> _params;
        private Random _random;
    }


    /// <summary>
    ///   A class to hold an OAuth response message.
    /// </summary>
    public class OAuthResponse
    {
        /// <summary>
        ///   All of the text in the response. This is useful if the app wants
        ///   to do its own parsing.
        /// </summary>
        public string AllText { get; set; }
        public Dictionary<string, string> _params;

        /// <summary>
        ///   a Dictionary of response parameters.
        /// </summary>
        public string this[string ix] => _params[ix];


        public OAuthResponse(string alltext)
        {
            AllText = alltext;
            System.Diagnostics.Debug.WriteLine(alltext);
            _params = new Dictionary<string, string>();
            string pattern = "\",";
            var kvpairs = Regex.Split(alltext, pattern);//alltext.Split(',');
            foreach (var pair in kvpairs)
            {
                var kv = pair.Split(new[] { ':' }, 2);
                var val = "";
                if (_params.TryGetValue(kv[0], out val))
                {
                    _params[kv[0]] = kv[1];
                }
                else
                {
                    _params.Add(kv[0], kv[1]);
                }
            }
            // expected keys:
            //   user_key, key, messages, status
        }
    }

    public class OAuthResponse1
    {
        /// <summary>
        ///   All of the text in the response. This is useful if the app wants
        ///   to do its own parsing.
        /// </summary>
        public string AllText { get; set; }
        public Dictionary<string, string> _params;

        /// <summary>
        ///   a Dictionary of response parameters.
        /// </summary>
        public string this[string ix] => _params[ix];
        public OAuthResponse1(string alltext)
        {
            AllText = alltext;
            System.Diagnostics.Debug.WriteLine(alltext);
            _params = new Dictionary<String, String>();

            var kvpairs = alltext.Split(',');
            foreach (var pair in kvpairs)
            {
                var kv = pair.Split(':');
                var val = "";
                if (_params.TryGetValue(kv[0], out val))
                {
                    _params[kv[0]] = kv[1];
                }
                else
                {
                    _params.Add(kv[0], kv[1]);
                }

            }
            // expected keys:
            //   oauth_token, oauth_token_secret, user_id, screen_name, etc
        }
    }
}