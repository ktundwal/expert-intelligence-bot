using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using UpworkAPI.Interfaces;

namespace UpworkAPI.Test.OAuthClientTests
{
    [TestClass]
    public class OAuthClientTokensTest
    {
        //[TestMethod]
        //[DataRow("oauth_token=test-token&oauth_token_secret=test-token-secret", "test-token", "token-secret")]
        //public async Task GetRequestTokensTest(string response, string token, string tokenSecret)
        //{
        //    // arrange
        //    OAuthConfig config = TestUtils.GetOauthConfig();
        //    var clientMock = new Mock<OAuthClient>();
        //    clientMock.Setup(a => a.SendRequest("https://www.upwork.com/api/auth/v1/oauth/token/request", "POST", new Dictionary<string, string>())).Returns(Task.FromResult(response));
        //    clientMock.Object.SetOAuthConfig(config);

        //    // act
        //    OAuthUpworkResponse oauthResponse = await clientMock.Object.GetRequestTokens();

        //    //assert
        //    oauthResponse["oauth_token"].Should().Be(token);
        //    oauthResponse["oauth_token_secret"].Should().Be(tokenSecret);
        //}
    }
}
