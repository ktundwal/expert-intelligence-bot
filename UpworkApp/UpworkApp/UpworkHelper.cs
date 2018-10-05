using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace UpworkApp
{
    //public class UpworkHelper
    //{

    //    /// <summary>
    //    /// Create Upwork Login URL
    //    /// </summary>
    //    /// <param name="conversationReference"></param>
    //    /// <param name="upworkOauthCallback"></param>
    //    /// <returns></returns>
    //    public static string GetUpworkLoginURL(ConversationReference conversationReference, string upworkOauthCallback, string token, string secretToken)
    //    {
    //        var redirectUri = GetOAuthCallBack(conversationReference, upworkOauthCallback, token, secretToken);
    //        var uri = GetUri(ConfigurationManager.AppSettings["UpworkAuthUrl"].ToString(),
    //            Tuple.Create("oauth_token", token),
    //            Tuple.Create("oauth_callback", redirectUri)
    //            );

    //        return uri.ToString();
    //    }

    //    /// <summary>
    //    /// Create the Auth URL    
    //    /// </summary>
    //    /// <param name="conversationReference"></param>
    //    /// <param name="upworkOauthCallback"></param>
    //    /// <returns></returns>
    //    public static string GetOAuthCallBack(ConversationReference conversationReference, string upworkOauthCallback, string token, string secretToken)
    //    {
    //        var uri = GetUri(upworkOauthCallback,
    //            Tuple.Create("userId", TokenEncoder(conversationReference.User.Id)),
    //            Tuple.Create("botId", TokenEncoder(conversationReference.Bot.Id)),
    //            Tuple.Create("conversationId", TokenEncoder(conversationReference.Conversation.Id)),
    //            Tuple.Create("serviceUrl", TokenEncoder(conversationReference.ServiceUrl)),
    //            Tuple.Create("channelId", conversationReference.ChannelId),
    //            Tuple.Create("secretToken", secretToken)
    //            );
    //        return uri.ToString();
    //    }

    //    public static string TokenEncoder(string token)
    //    {
    //        return HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(token));
    //    }

    //    /// <summary>
    //    /// Helper method to create URL
    //    /// </summary>
    //    /// <param name="endPoint"></param>
    //    /// <param name="queryParams"></param>
    //    /// <returns></returns>
    //    private static Uri GetUri(string endPoint, params Tuple<string, string>[] queryParams)
    //    {
    //        var queryString = HttpUtility.ParseQueryString(string.Empty);
    //        foreach (var queryparam in queryParams)
    //        {
    //            queryString[queryparam.Item1] = queryparam.Item2;
    //        }

    //        var builder = new UriBuilder(endPoint);
    //        builder.Query = queryString.ToString();
    //        return builder.Uri;
    //    }
    //}
}
